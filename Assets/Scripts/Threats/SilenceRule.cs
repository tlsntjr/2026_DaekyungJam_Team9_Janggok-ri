using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 갯벌 3페이즈 "정적" 기믹 — 아이들 웃음소리가 뚝 끊기면, 쟤들이 귀 기울이고 있다는 뜻.
/// 정적 창(silenceDuration) 동안 움직이면(발소리) 감지되어 오염이 차오르고 스팅이 울린다.
/// 밀물(Deadline 지속 오염)이 "빨리 가라"고 미는 것과 정반대로 "멈춰라"가 당기는 3페이즈의 긴장 축.
///
/// IThreatBehavior — HauntController의 3페이즈 threats 배열에 등록하면 페이즈 시작/종료가 자동 관리됨.
/// 사운드가 아직 없어도 대사·로그로 동작 확인 가능.
/// </summary>
public class SilenceRule : MonoBehaviour, IThreatBehavior
{
    [Header("참조 (비우면 Player 태그에서 자동 탐색)")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("정적 주기")]
    [SerializeField] private float silenceIntervalMin = 10f;   // 웃음이 이어지는 시간 (다음 정적까지)
    [SerializeField] private float silenceIntervalMax = 16f;
    [SerializeField] private float silenceDuration = 3.5f;     // 정적 창 — 이 동안 움직이면 감지
    [SerializeField] private float firstSilenceDelay = 15f;    // 첫 정적까지의 여유 — 웃음이 "기본 상태"로 인식될 시간

    [Header("페널티")]
    [SerializeField, Range(0f, 1f)] private float contaminationPerSecondWhileMoving = 0.08f;

    [Header("안내 (각각 첫 1회만 — 규칙 학습용)")]
    [SerializeField, TextArea(2, 3)] private string laughterHint = "...아이들 웃음소리. 웃고 있는 동안은, 나를 보고 있지 않다.";
    [SerializeField] private float laughterHintDelay = 3f;     // 페이즈 도입 대사와 겹치지 않게 살짝 늦게
    [SerializeField, TextArea(2, 3)] private string firstSilenceHint = "...웃음소리가 멎었다. 움직이면 안 될 것 같다.";

    [Header("사운드 (비우면 스킵)")]
    [SerializeField] private EventReference laughterLoop;   // 아이들 웃음 루프 (2D 또는 3D)
    [SerializeField] private EventReference detectedSting;  // 정적 중 움직였을 때 스팅

    private bool isActive;
    private bool hinted;
    private bool laughterHinted;
    private Coroutine loopCoroutine;
    private Coroutine hintCoroutine;
    private EventInstance laughterInstance;
    private bool laughterPlaying;

    public bool IsNeutralized { get; private set; }

    public void Activate()
    {
        IsNeutralized = false;
        isActive = true;
        StartLaughter();
        loopCoroutine = StartCoroutine(SilenceLoop());

        // 규칙 성립의 1단계 — "웃음 = 안전"을 먼저 알려줘야, 나중에 끊겼을 때 "정적 = 위험"이 이해됨
        if (!laughterHinted && !string.IsNullOrEmpty(laughterHint))
        {
            laughterHinted = true;
            hintCoroutine = StartCoroutine(LaughterHintRoutine());
        }
    }

    private IEnumerator LaughterHintRoutine()
    {
        yield return new WaitForSeconds(laughterHintDelay);

        // 스토리 시퀀스(수첩 등) 진행 중이면 끝날 때까지 대기 후 표시
        yield return new WaitUntil(() => !DialogueSystem.Instance.IsSequenceActive);

        if (isActive)
            DialogueSystem.Instance.Show(laughterHint);
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        isActive = false;
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        StopLaughter(immediate: false);
    }

    public void Tick() { }
    public void SetProgress(float t) { }

    private void Awake()
    {
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    private void OnDisable()
    {
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
        StopLaughter(immediate: true);
    }

    private IEnumerator SilenceLoop()
    {
        bool firstCycle = true;

        while (isActive)
        {
            // ── 웃음이 이어지는 구간 (자유 이동) ──
            // 첫 사이클은 여유를 길게 — 웃음소리가 "이 구역의 기본 상태"로 귀에 자리잡은 뒤에 끊겨야
            // "멎었다"는 변화가 공포로 인식됨
            float wait = firstCycle ? firstSilenceDelay : Random.Range(silenceIntervalMin, silenceIntervalMax);
            firstCycle = false;

            yield return new WaitForSeconds(wait);
            if (!isActive) yield break;

            // ── 정적 시작 — 웃음이 뚝 끊김 ──
            StopLaughter(immediate: true);   // 페이드 없이 '뚝' 끊기는 게 신호의 핵심
            Debug.Log($"<color=yellow>[SilenceRule]</color> 정적 — {silenceDuration}초간 움직이지 말 것");

            // 스토리 시퀀스(수첩 등) 진행 중엔 안내를 끼워넣지 않음 — 힌트는 소비하지 않고 다음 정적 때 재시도
            if (!hinted && !string.IsNullOrEmpty(firstSilenceHint) && !DialogueSystem.Instance.IsSequenceActive)
            {
                hinted = true;
                DialogueSystem.Instance.Show(firstSilenceHint);
            }

            bool stungThisWindow = false;
            float t = 0f;
            while (t < silenceDuration)
            {
                if (!isActive) yield break;
                t += Time.deltaTime;

                if (playerMovement != null && playerMovement.IsMoving)
                {
                    ContaminationSystem.Instance.Add(contaminationPerSecondWhileMoving * Time.deltaTime);

                    if (!stungThisWindow)
                    {
                        stungThisWindow = true;
                        if (!detectedSting.IsNull)
                            SoundManager.Instance.PlayOneShot(detectedSting, transform.position);
                        Debug.Log("<color=red>[SilenceRule]</color> 정적 중 움직임 감지!");
                    }
                }

                yield return null;
            }

            // ── 정적 종료 — 웃음 재개 ──
            StartLaughter();
        }
    }

    private void StartLaughter()
    {
        if (laughterLoop.IsNull || laughterPlaying) return;
        laughterInstance = SoundManager.Instance.PlayLoop(laughterLoop, transform);
        laughterPlaying = true;
    }

    private void StopLaughter(bool immediate)
    {
        if (!laughterPlaying) return;
        SoundManager.Instance.StopLoop(laughterInstance, immediate);
        laughterPlaying = false;
    }
}
