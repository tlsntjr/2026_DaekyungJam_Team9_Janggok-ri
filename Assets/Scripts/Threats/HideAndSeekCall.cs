using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 갯벌 2페이즈 "부름" 기믹 — 숨바꼭질의 "찾으러 간다" 규칙.
/// 주기적으로 원혼의 부름이 들려온다 → 유혹 대사 + 선택지 팝업 →
///   · 대답(위험) 선택: 즉시 오염 스파이크 (대답 금지 규칙 위반)
///   · 무시(안전) 선택: "찾으러 온다" — 유예 시간 안에 은신처(Concealment 존)에 숨어야 함.
///     유예가 끝나는 순간 숨어있지 않으면 오염 스파이크.
/// 팝업이 "닫는 것"이 아니라 "행동의 시작 신호"가 되는 구조.
///
/// IThreatBehavior — HauntController의 2페이즈 threats 배열에 등록하면 페이즈 시작/종료가 자동 관리됨.
/// </summary>
public class HideAndSeekCall : MonoBehaviour, IThreatBehavior
{
    [Header("부름 주기 (페이즈 시작 후 첫 부름까지도 이 간격)")]
    [SerializeField] private float callIntervalMin = 14f;
    [SerializeField] private float callIntervalMax = 22f;

    [Header("유혹 대사 풀 (부름마다 무작위 1줄)")]
    [SerializeField, TextArea(2, 4)] private string[] luringPool = {
        "『찾는다아— 어디 숨었니?』",
        "『이번엔 네가 술래야.』",
        "『같이 숨자. 여기가 좋아.』",
        "『발소리 다 들려.』",
    };

    [Header("선택지 (위치는 매번 랜덤)")]
    [SerializeField] private string safeOption = "...";
    [SerializeField] private string unsafeOption = "대답한다";

    [Header("숨기 유예")]
    [SerializeField] private float hideGraceTime = 5f;                     // 선택 후 이 시간 안에 은신해야 함
    [SerializeField, TextArea(2, 3)] private string seekWarningLine = "...찾으러 온다. 숨어야 해!";
    [SerializeField] private bool warnOnlyFirstTime = true;                // 경고 대사는 첫 부름에만 (이후엔 소리로만)

    [Header("페널티 (오염 스파이크)")]
    [SerializeField, Range(0f, 1f)] private float answeredPenalty = 0.2f;  // 대답했을 때
    [SerializeField, Range(0f, 1f)] private float caughtPenalty = 0.2f;    // 못 숨었을 때
    [SerializeField, TextArea(2, 3)] private string caughtLine = "『찾았다.』";

    [Header("사운드 (비우면 스킵)")]
    [SerializeField] private EventReference callSfx;     // 부름 (아이들 목소리 — 3D, 원혼 위치감)
    [SerializeField] private EventReference caughtSfx;   // 들켰을 때 스팅

    [Header("처벌 연출 — 규칙 위반(대답함) / 들킴 공통")]
    [SerializeField] private float penaltyShakeDuration = 0.3f;
    [SerializeField] private float penaltyShakeMagnitude = 0.25f;
    [SerializeField, Range(0f, 1f)] private float penaltyGlitchStrength = 0.6f;

    [Header("수색 동안 위협 레벨 — 심장박동(ThreatState 파라미터)·추격 BGM 연동 (huntId 비우면 스킵)")]
    [SerializeField] private string huntId = "mudflat_call";   // 실제 몬스터 huntId와 겹치지만 않으면 됨
    [SerializeField] private int seekThreatLevel = 2;          // 2 이상이면 심장박동·추격 BGM 발동

    private bool isActive;
    private bool warned;
    private bool threatRaised;
    private Coroutine loopCoroutine;

    public bool IsNeutralized { get; private set; }

    public void Activate()
    {
        IsNeutralized = false;
        isActive = true;
        loopCoroutine = StartCoroutine(CallLoop());
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        isActive = false;
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
        SetSeekThreat(false);   // 수색 도중 페이즈가 끝나도 심장박동이 켜진 채 남지 않게
    }

    public void Tick() { }
    public void SetProgress(float t) { }

    private void OnDisable()
    {
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
    }

    private IEnumerator CallLoop()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(Random.Range(callIntervalMin, callIntervalMax));
            if (!isActive) yield break;

            // 스토리 시퀀스(수첩 등) 진행 중이면 부름을 미룸 — 선택지가 스토리 대사를 덮지 않게
            yield return new WaitUntil(() => !DialogueSystem.Instance.IsSequenceActive);
            if (!isActive) yield break;

            yield return RunOneCall();
        }
    }

    private IEnumerator RunOneCall()
    {
        if (!callSfx.IsNull)
            SoundManager.Instance.PlayOneShot(callSfx, transform.position);

        // 유혹 대사를 질문 텍스트로 담아 선택지 표시 (선택이 끝날 때까지 대기)
        bool choiceResolved = false;
        bool choseSafe = false;

        string lure = luringPool != null && luringPool.Length > 0
            ? luringPool[Random.Range(0, luringPool.Length)]
            : null;

        bool swapped = Random.value < 0.5f;
        string[] options = swapped
            ? new[] { unsafeOption, safeOption }
            : new[] { safeOption, unsafeOption };

        DialogueSystem.Instance.ShowChoice(lure, options, index =>
        {
            choseSafe = (index == 0) != swapped;
            choiceResolved = true;
        });

        yield return new WaitUntil(() => choiceResolved || !isActive);
        if (!isActive) yield break;

        // 대답해버림 — 괴담 규칙 위반, 즉시 대가
        if (!choseSafe)
        {
            ContaminationSystem.Instance.Add(answeredPenalty);
            PlayPenaltyEffect();
            if (!caughtSfx.IsNull)
                SoundManager.Instance.PlayOneShot(caughtSfx, transform.position);
            Debug.Log("<color=red>[HideAndSeekCall]</color> 대답함 — 오염 스파이크");
            yield break;
        }

        // 무시 성공 → "찾으러 온다" — 유예 안에 은신해야 함
        if (!warned || !warnOnlyFirstTime)
        {
            warned = true;
            if (!string.IsNullOrEmpty(seekWarningLine))
                DialogueSystem.Instance.Show(seekWarningLine);
        }

        // 수색 시작 — 위협 레벨 상승 → 심장박동(ThreatState 파라미터)·추격 BGM이 차오름
        SetSeekThreat(true);

        Debug.Log($"<color=orange>[HideAndSeekCall]</color> 수색 시작 — {hideGraceTime}초 안에 은신할 것");
        yield return new WaitForSeconds(hideGraceTime);
        if (!isActive) { SetSeekThreat(false); yield break; }

        // 판정의 순간 — 유예가 끝났을 때 숨어있는가
        if (Concealment.IsPlayerConcealed)
        {
            Debug.Log("<color=green>[HideAndSeekCall]</color> 은신 성공 — 지나쳐 갔다");
        }
        else
        {
            ContaminationSystem.Instance.Add(caughtPenalty);
            PlayPenaltyEffect();
            if (!caughtSfx.IsNull)
                SoundManager.Instance.PlayOneShot(caughtSfx, transform.position);
            if (!string.IsNullOrEmpty(caughtLine))
                DialogueSystem.Instance.Show(caughtLine);
            Debug.Log("<color=red>[HideAndSeekCall]</color> 들킴 — 오염 스파이크");
        }

        // 수색 종료 — 위협 해제 (심장이 잦아듦, FMOD 파라미터 Seek Speed가 잔향을 만듦)
        SetSeekThreat(false);
    }

    /// <summary>처벌 순간 화면 흔들림 + 글리치 펄스 — 규칙 위반/들킴 둘 다 공통으로 사용</summary>
    private void PlayPenaltyEffect()
    {
        CameraShake.Instance.Shake(penaltyShakeDuration, penaltyShakeMagnitude);
        PanicGlitchDirector.Instance.Pulse(penaltyGlitchStrength);
    }

    /// <summary>수색 구간 위협 레벨 on/off — 중복 발화 방지 가드 포함</summary>
    private void SetSeekThreat(bool on)
    {
        if (string.IsNullOrEmpty(huntId)) return;
        if (on == threatRaised) return;

        threatRaised = on;
        EventBus.RaiseThreatStateChanged(huntId, on ? seekThreatLevel : 0);
    }
}
