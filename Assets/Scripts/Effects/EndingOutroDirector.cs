using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 엔딩 아웃트로 컷씬 —
///   어둠 속 독백(속삭임 루프가 깔림) → 눈을 깜빡이며 뜨는 연출(점점 크게 떠지며 "이상한 곳"이 드러남)
///   → 잠시 바라보는 정적 → 속삭임이 뚝 끊기며 라이저 SFX + 다시 어둠 → To Be Continued → 메인 메뉴.
///
/// 눈 깜빡임은 검은 오버레이 알파로 표현: 감김(1) → 반쯤 떠짐 → 다시 감김 → 더 떠짐 → ... → 완전히 떠짐(0).
///
/// 씬 세팅:
///   · 이 컴포넌트 하나로 엔딩 컷씬 전체를 담당 — 엔딩 씬에 SceneIntroDirector는 필요 없음.
///   · Canvas 밑에 ① 전체화면 검은 Image + CanvasGroup = blackOverlay,
///     ② "To Be Continued" 텍스트 그룹 + CanvasGroup(alpha 0) = tbcGroup (오버레이보다 하이어라키 아래 = 위에 그려짐).
///   · 독백은 DialogueSystem을 쓰므로 이 Canvas의 Sort Order를 대사 UI 캔버스보다 "낮게" —
///     안 그러면 검은 화면이 대사창을 가려 독백이 안 보임.
///   · 속삭임/라이저 FMOD 이벤트는 2D 권장.
/// </summary>
/// <summary>아웃트로 진행 단계 — 오브젝트 켜기/끄기 타이밍 지정용</summary>
public enum OutroStage
{
    Monologue,       // 어둠 속 독백 시작
    Reveal,          // 눈뜨기 시작 (이상한 곳이 드러나기 직전 — 배치물 켜기 좋은 지점)
    Blackout,        // 라이저와 함께 다시 어두워지는 순간
    ToBeContinued,   // TBC 표시 순간
}

/// <summary>단계 진입 순간 켜고/끌 오브젝트 묶음</summary>
[System.Serializable]
public class OutroStageToggle
{
    public OutroStage stage;
    public GameObject[] enable;    // 이 단계에 SetActive(true)
    public GameObject[] disable;   // 이 단계에 SetActive(false)
}

public class EndingOutroDirector : MonoBehaviour
{
    [Header("시작 — 씬 시작 후 이 시간 뒤 자동 시작 (컷씬 구성 기본값)")]
    [SerializeField] private float autoStartDelay = 0.6f;

    [Header("어둠 속 독백 (자동 진행 — DialogueUI autoAdvance)")]
    [SerializeField, TextArea(2, 4)] private string[] finalLines;

    [Header("독백 캐릭터 일러스트 (비우면 스킵 — 독백 동안만 표시, 줄 번호로 표정 전환)")]
    [SerializeField] private MonologuePortrait portrait;
    [SerializeField] private PortraitCue[] portraitCues;   // 예: 마지막 줄 → 어지러움 (눈뜨기 직전의 상태 전달)

    [Header("사운드 (비우면 스킵)")]
    [SerializeField] private EventReference whisperLoop;   // 속삭임 루프 — 독백 시작부터 라이저 직전까지 (2D)
    [SerializeField] private EventReference riseUpSfx;     // 기이한 라이저 — 다시 어두워지는 순간 (2D)

    [Header("눈뜨기 연출")]
    [SerializeField] private int blinkCount = 2;              // 완전히 뜨기 전 깜빡이는 횟수 (점점 크게 떠짐)
    [SerializeField] private float blinkOpenTime = 0.45f;     // 눈이 떠지는 속도 (한 번 깜빡일 때)
    [SerializeField] private float blinkCloseTime = 0.2f;     // 도로 감기는 속도
    [SerializeField] private float blinkClosedHold = 0.3f;    // 감긴 채 머무는 시간
    [SerializeField] private float finalOpenDuration = 1.1f;  // 마지막으로 완전히 떠지는 시간
    [SerializeField] private float revealHoldTime = 3.5f;     // 이상한 곳을 바라보는 정적 (속삭임은 계속)

    [Header("어둠 복귀 → To Be Continued")]
    [SerializeField] private float blackoutFadeDuration = 0.9f;  // 라이저와 함께 어둠에 잠기는 속도
    [SerializeField] private CanvasGroup blackOverlay;           // 전체 화면 검은 이미지
    [SerializeField] private CanvasGroup tbcGroup;               // "To Be Continued" 그룹 (시작 alpha 0)
    [SerializeField] private float tbcDelay = 1.4f;              // 어둠 속 정적 — 여운
    [SerializeField] private float tbcFadeDuration = 1.5f;
    [SerializeField] private float tbcHoldTime = 4f;

    [Header("메인 메뉴 씬 이름 (Build Settings에 등록돼 있어야 함)")]
    [SerializeField] private string mainMenuSceneName = "SCENE_MAIN";

    [Header("단계별 오브젝트 켜기/끄기 — 각 단계 진입 순간 적용 (예: Reveal에 '이상한 곳' 배치물 켜기)")]
    [SerializeField] private OutroStageToggle[] stageToggles;

    private bool began;
    private EventInstance whisperInstance;
    private bool whisperPlaying;

    private void Start()
    {
        // 첫 프레임부터 눈 감은 상태 — 이상한 곳이 미리 보이는 스포 방지
        if (blackOverlay != null) { blackOverlay.alpha = 1f; blackOverlay.blocksRaycasts = true; }
        if (tbcGroup != null) tbcGroup.alpha = 0f;

        // 무조건 자동 시작 — 예전 버전의 "0 = 트리거 전용" 값이 인스펙터에 저장돼 있어도 동작하게
        Invoke(nameof(Begin), Mathf.Max(0.05f, autoStartDelay));
    }

    /// <summary>컷씬 시작 — 자동 지연 외에 트리거/다른 연출에서 직접 불러도 됨</summary>
    public void Begin()
    {
        if (began) return;
        began = true;
        CancelInvoke(nameof(Begin));
        StartCoroutine(OutroRoutine());
    }

    private IEnumerator OutroRoutine()
    {
        // 한 프레임 대기 — DialogueUI가 구독을 마치기 전에 ShowSequence를 쏘면 독백이 통째로 유실됨
        yield return null;

        // 조작 잠금 — 엔딩 컷씬 동안 플레이어는 개입하지 않음
        GameObject playerObj = GameObject.FindWithTag("Player");
        PlayerMovement movement = playerObj != null ? playerObj.GetComponent<PlayerMovement>() : null;
        if (movement != null) movement.MovementLocked = true;

        // ── 1) 어둠 속 독백 — 속삭임이 깔린 채 ──
        ApplyStage(OutroStage.Monologue);
        StartWhisper();

        if (finalLines != null && finalLines.Length > 0 && DialogueSystem.Instance != null)
        {
            if (portrait != null) portrait.Show(PortraitEmotion.Default);   // 독백 시작과 함께 등장

            bool done = false;
            DialogueSystem.Instance.ShowSequence(finalLines,
                lineIndex => { if (portrait != null) portrait.ApplyCue(portraitCues, lineIndex); },
                () => done = true);
            yield return new WaitUntil(() => done);

            if (portrait != null) portrait.Hide();   // 눈뜨기 전에 퇴장 — 드러나는 "이상한 곳"을 가리지 않게
        }

        // ── 2) 눈을 깜빡이며 뜸 — 점점 크게 떠지다 완전히 뜨며 "이상한 곳"이 드러남 ──
        ApplyStage(OutroStage.Reveal);   // 아직 화면은 검은 상태 — 여기서 켜지는 배치물은 첫 깜빡임에 처음 보임
        for (int i = 0; i < blinkCount; i++)
        {
            // i번째 깜빡임의 최대 개방 정도 (예: 2회면 40% → 70% → 마지막에 100%)
            float openness = (i + 1f) / (blinkCount + 1f) + 0.1f;
            float targetAlpha = Mathf.Clamp01(1f - openness);

            yield return FadeGroup(blackOverlay, 1f, targetAlpha, blinkOpenTime);
            yield return FadeGroup(blackOverlay, targetAlpha, 1f, blinkCloseTime);
            yield return new WaitForSeconds(blinkClosedHold);
        }
        yield return FadeGroup(blackOverlay, 1f, 0f, finalOpenDuration);
        if (blackOverlay != null) blackOverlay.blocksRaycasts = false;

        // ── 3) 이상한 곳을 바라보는 정적 — 속삭임은 계속되고 있다 ──
        yield return new WaitForSeconds(revealHoldTime);

        // ── 4) 속삭임이 뚝 끊기고, 라이저와 함께 다시 어둠 ──
        ApplyStage(OutroStage.Blackout);
        StopWhisper(immediate: true);   // 페이드 없이 '뚝' — 끊김 자체가 신호
        if (!riseUpSfx.IsNull)
            SoundManager.Instance.PlayOneShot(riseUpSfx, transform.position);

        if (blackOverlay != null) blackOverlay.blocksRaycasts = true;
        yield return FadeGroup(blackOverlay, 0f, 1f, blackoutFadeDuration);

        // ── 5) 어둠 속 정적 → To Be Continued → 메인 메뉴 ──
        yield return new WaitForSeconds(tbcDelay);

        ApplyStage(OutroStage.ToBeContinued);
        if (tbcGroup != null)
            yield return FadeGroup(tbcGroup, 0f, 1f, tbcFadeDuration);

        yield return new WaitForSeconds(tbcHoldTime);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>해당 단계에 등록된 오브젝트 켜기/끄기 적용</summary>
    private void ApplyStage(OutroStage stage)
    {
        if (stageToggles == null) return;

        foreach (var toggle in stageToggles)
        {
            if (toggle == null || toggle.stage != stage) continue;

            if (toggle.enable != null)
                foreach (var go in toggle.enable)
                    if (go != null) go.SetActive(true);

            if (toggle.disable != null)
                foreach (var go in toggle.disable)
                    if (go != null) go.SetActive(false);
        }
    }

    private void StartWhisper()
    {
        if (whisperLoop.IsNull || whisperPlaying || SoundManager.Instance == null) return;
        whisperInstance = SoundManager.Instance.PlayLoop(whisperLoop, transform);
        whisperPlaying = true;
    }

    private void StopWhisper(bool immediate)
    {
        if (!whisperPlaying) return;
        SoundManager.Instance.StopLoop(whisperInstance, immediate);
        whisperPlaying = false;
    }

    private void OnDisable() => StopWhisper(immediate: true);

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
