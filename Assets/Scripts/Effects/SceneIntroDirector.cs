using System.Collections;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;

/// <summary>
/// 씬 시작 인트로 연출: 검은 화면 위에 독백이 흐르고, 끝나면 효과음(문 소리 등)과 함께
/// 페이드인되며 조작이 풀린다. 씬마다 배치 (집 오프닝, 갯벌/양식장 진입 독백 재사용 가능).
///
/// 흐름: 검은 오버레이 + 조작 잠금 → (발소리 등 루프 재생) → 독백 시퀀스 (클릭으로 진행)
///       → 루프 정지 + 전환음 1회 → 오버레이 페이드인 → 조작 해제
///
/// 배치 주의: blackOverlay(전체 화면 CanvasGroup)는 하이어라키에서 DialogueUI보다 "위(이전 순서)"에
/// 있어야 독백 텍스트가 검은 화면 위로 보인다.
/// </summary>
public class SceneIntroDirector : MonoBehaviour
{
    [Header("검은 오버레이 (화면 전체를 덮는 CanvasGroup)")]
    [SerializeField] private CanvasGroup blackOverlay;
    [SerializeField] private float fadeInDuration = 1.2f;

    [Header("괴담 표제 (검은 화면 중앙 TMP — 비우면 스킵. 예: 第一話 — 갯벌의 숨바꼭질)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string titleString;          // 비우면 titleText에 미리 적힌 내용 그대로 사용
    [SerializeField] private float titleFadeDuration = 0.8f;
    [SerializeField] private float titleHoldDuration = 1.8f;
    [SerializeField] private bool titleAfterMonologue = false;   // 켜면 독백·페이드인이 끝난 뒤, 밝아진 게임 화면 위에 표제 표시 (조작 가능한 상태)

    [Header("인트로 독백 (한 칸 = 한 줄, 클릭으로 다음 줄. 비우면 즉시 페이드인만)")]
    [SerializeField, TextArea(2, 4)] private string[] introLines;

    [Header("독백 캐릭터 일러스트 (비우면 스킵 — 독백 동안만 표시, 줄 번호로 표정 전환)")]
    [SerializeField] private MonologuePortrait portrait;
    [SerializeField] private PortraitCue[] portraitCues;   // 예: lineIndex 2 → 놀람. 비우면 기본 표정 고정

    [Header("독백 동안 재생할 루프 사운드 (발소리 등 — 비우면 스킵, 2D 이벤트 권장)")]
    [SerializeField] private EventReference ambientLoopSfx;

    [Header("독백 종료 시 1회 재생 (문 여닫는 소리 등 — 비우면 스킵)")]
    [SerializeField] private EventReference transitionSfx;

    [Header("인트로 동안 잠글 플레이어 컴포넌트 (이동·조준·상호작용 등)")]
    [SerializeField] private MonoBehaviour[] disableDuringIntro;

    private EventInstance loopInstance;
    private bool loopPlaying;

    private void Start()
    {
        // 화면 가림·조작 잠금은 즉시 (한 프레임이라도 맵이 보이거나 움직여지면 안 됨)
        foreach (var comp in disableDuringIntro)
            if (comp != null) comp.enabled = false;

        if (blackOverlay != null)
        {
            blackOverlay.alpha = 1f;
            blackOverlay.blocksRaycasts = false;   // 독백 클릭 진행을 막지 않도록 — 반드시 통과
            blackOverlay.interactable = false;
        }

        StartCoroutine(BeginIntroRoutine());
    }

    private IEnumerator BeginIntroRoutine()
    {
        // 한 프레임 대기 — Start끼리의 실행 순서는 보장되지 않아서,
        // DialogueUI가 구독을 마치기 전에 ShowSequence를 쏘면 독백이 통째로 유실됨
        yield return null;

        // 표제를 독백 앞에 표시하는 순서 (기본)
        if (titleText != null && !titleAfterMonologue)
            yield return TitleSequence();

        if (!ambientLoopSfx.IsNull)
        {
            loopInstance = SoundManager.Instance.PlayLoop(ambientLoopSfx, transform);
            loopPlaying = true;
        }

        if (introLines != null && introLines.Length > 0)
        {
            if (portrait != null) portrait.Show(PortraitEmotion.Default);   // 독백 시작과 함께 등장

            DialogueSystem.Instance.ShowSequence(introLines,
                lineIndex => { if (portrait != null) portrait.ApplyCue(portraitCues, lineIndex); },
                () => StartCoroutine(AfterMonologueRoutine()));
        }
        else
        {
            StartCoroutine(AfterMonologueRoutine());
        }
    }

    private IEnumerator AfterMonologueRoutine()
    {
        if (portrait != null) portrait.Hide();   // 일러스트는 독백과 함께 퇴장 — 페이드인 화면을 가리지 않게

        // 발소리 등 루프는 독백과 함께 끝남
        if (loopPlaying)
        {
            SoundManager.Instance.StopLoop(loopInstance);
            loopPlaying = false;
        }

        FinishIntro();
        yield break;
    }

    private IEnumerator TitleSequence()
    {
        if (!string.IsNullOrEmpty(titleString))
            titleText.text = titleString;

        titleText.gameObject.SetActive(true);

        yield return FadeTitle(0f, 1f);
        yield return new WaitForSeconds(titleHoldDuration);
        yield return FadeTitle(1f, 0f);

        titleText.gameObject.SetActive(false);
    }

    private IEnumerator FadeTitle(float from, float to)
    {
        float t = 0f;
        while (t < titleFadeDuration)
        {
            t += Time.deltaTime;
            titleText.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / titleFadeDuration));
            yield return null;
        }
        titleText.alpha = to;
    }

    private void FinishIntro()
    {
        if (loopPlaying)
        {
            SoundManager.Instance.StopLoop(loopInstance);   // 표제 없이 바로 온 경로 대비 (AfterMonologue에서 이미 멈췄으면 스킵)
            loopPlaying = false;
        }

        if (!transitionSfx.IsNull)
            SoundManager.Instance.PlayOneShot(transitionSfx, transform.position);

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeInDuration && blackOverlay != null)
        {
            t += Time.deltaTime;
            blackOverlay.alpha = 1f - Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        if (blackOverlay != null) blackOverlay.alpha = 0f;

        foreach (var comp in disableDuringIntro)
            if (comp != null) comp.enabled = true;

        // 표제 후행 모드 — 밝아진 게임 화면 위에 표제가 떠올랐다 사라짐 (조작은 이미 풀린 상태)
        if (titleText != null && titleAfterMonologue)
            yield return TitleSequence();
    }

    private void OnDisable()
    {
        if (loopPlaying)
        {
            SoundManager.Instance.StopLoop(loopInstance, immediate: true);
            loopPlaying = false;
        }
    }
}
