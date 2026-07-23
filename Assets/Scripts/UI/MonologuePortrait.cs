using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>표정 종류 — 일러스트 3종에 대응</summary>
public enum PortraitEmotion
{
    Default,     // 기본
    Surprised,   // 놀람
    Dizzy,       // 어지러움
}

/// <summary>줄 번호(0부터)에 도달하면 해당 표정으로 전환 — DialogueLineEffect와 같은 사용 방식</summary>
[System.Serializable]
public class PortraitCue
{
    public int lineIndex;
    public PortraitEmotion emotion;
}

/// <summary>
/// 독백용 캐릭터 일러스트 — 인트로(SceneIntroDirector)·아웃트로(EndingOutroDirector) 전용.
/// 인게임 대사(DialogueUI)에는 관여하지 않는다 — 디렉터가 독백 시작/줄 전환/종료 시점에 직접 호출.
///
/// 셋업: 인트로/엔딩 Canvas 밑에 일러스트 Image 오브젝트 + CanvasGroup + 이 컴포넌트.
///       검은 오버레이보다 하이어라키상 "아래"(= 화면상 위에 그려지게) 배치.
///       스프라이트 3장(기본/놀람/어지러움) 등록. 시작 시 자동으로 투명(알파 0).
/// </summary>
public class MonologuePortrait : MonoBehaviour
{
    [Header("참조 (비우면 자기 자신에서 자동 탐색)")]
    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup group;

    [Header("일러스트 3종")]
    [SerializeField] private Sprite defaultSprite;     // 기본
    [SerializeField] private Sprite surprisedSprite;   // 놀람
    [SerializeField] private Sprite dizzySprite;       // 어지러움

    [Header("연출")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float punchScale = 1.05f;     // 표정이 바뀌는 순간 살짝 커졌다 돌아오는 팝 (1 = 끔)
    [SerializeField] private float punchDuration = 0.18f;

    private Coroutine fadeCoroutine;
    private Coroutine punchCoroutine;
    private Vector3 baseScale;
    private bool visible;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
        if (group == null) group = GetComponent<CanvasGroup>();
        baseScale = transform.localScale;

        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; }
    }

    /// <summary>표정 표시 — 숨겨져 있으면 페이드인, 이미 보이는 중에 표정이 바뀌면 팝 연출</summary>
    public void Show(PortraitEmotion emotion)
    {
        Sprite sprite = SpriteFor(emotion);
        if (sprite == null || image == null)
        {
            Debug.LogWarning($"[MonologuePortrait] '{emotion}' 스프라이트 미등록 — 표정 전환 스킵");
            return;
        }

        bool changed = image.sprite != sprite;
        image.sprite = sprite;

        if (!visible)
        {
            visible = true;
            StartFade(1f);
        }
        else if (changed)
        {
            Punch();
        }
    }

    /// <summary>줄 번호에 걸린 큐가 있으면 해당 표정으로 전환 — 디렉터의 onLineShown에서 호출</summary>
    public void ApplyCue(PortraitCue[] cues, int lineIndex)
    {
        if (cues == null) return;
        foreach (var cue in cues)
            if (cue != null && cue.lineIndex == lineIndex)
            {
                Show(cue.emotion);
                return;
            }
    }

    public void Hide()
    {
        if (!visible) return;
        visible = false;
        StartFade(0f);
    }

    private Sprite SpriteFor(PortraitEmotion emotion) => emotion switch
    {
        PortraitEmotion.Surprised => surprisedSprite,
        PortraitEmotion.Dizzy => dizzySprite,
        _ => defaultSprite,
    };

    private void StartFade(float target)
    {
        if (group == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(target));
    }

    private IEnumerator FadeRoutine(float target)
    {
        float from = group.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        group.alpha = target;
    }

    private void Punch()
    {
        if (punchScale <= 1f) return;
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / punchDuration);
            float s = Mathf.Lerp(punchScale, 1f, p);   // 커진 상태에서 원래 크기로 스냅백
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    private void OnDisable()
    {
        visible = false;
        if (group != null) group.alpha = 0f;
        transform.localScale = baseScale;
    }
}
