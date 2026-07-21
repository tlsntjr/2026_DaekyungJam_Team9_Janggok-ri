using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 갯벌 4페이즈 "귀환 러시"의 밀물 추격벽 — 바다 쪽에서 뭍 방향으로 일정 속도로 전진한다.
/// 밀물에 잠겨 죽은 아이들이 밀물과 함께 돌아오는 연출: 이 오브젝트 하위에
/// 실루엣 무리 스프라이트 + 물 이펙트를 담고, 최전선에 Collider2D(Is Trigger) + ThreatContact(killInstantly)를
/// 붙이면 닿는 순간 사망 처리는 기존 시스템이 알아서 한다.
///
/// IThreatBehavior — HauntController의 4페이즈 threats 배열에 등록:
///   Activate → 오브젝트 켜지고 전진 시작 / Neutralize(페이즈 클리어) → 정지·소멸.
/// 속도 가이드: 플레이어 이동속도(5)의 0.8~0.9배 = 4.0~4.5. "멈추면 잡히고, 계속 달리면 아슬아슬".
/// </summary>
public class AdvancingTideWall : MonoBehaviour, IThreatBehavior
{
    [Header("전진 방향 (바다 → 뭍. 예: 아래쪽 바다면 (0, 1))")]
    [SerializeField] private Vector2 advanceDirection = Vector2.up;

    [Header("전진 속도 (플레이어 5 기준 0.8~0.9배 권장)")]
    [SerializeField] private float advanceSpeed = 4.2f;

    [Header("시작 위치로 리셋 후 발동 (재시작 대비)")]
    [SerializeField] private bool resetPositionOnActivate = true;

    [Header("사운드 (비우면 스킵 — 밀려오는 물+웅성거림 루프, 3D로 만들면 다가오는 게 들림)")]
    [SerializeField] private EventReference rushLoopSfx;

    private Vector3 initialPosition;
    private bool advancing;
    private bool initialized;
    private bool pendingActivation;   // Activate 경유 활성화인지 구분 (Awake의 자동 숨김과 충돌 방지)
    private EventInstance loopInstance;
    private bool loopPlaying;

    public bool IsNeutralized { get; private set; }

    private void Awake()
    {
        initialPosition = transform.position;
        initialized = true;

        // 씬에 켜진 채 배치됐다면 페이즈 발동 전까지 숨김 (PhaseObjectToggler와 같은 규약).
        // Activate()가 켜는 경우(pendingActivation)는 숨기면 안 됨 — 켜자마자 다시 꺼지는 버그 방지
        if (!pendingActivation)
            gameObject.SetActive(false);
    }

    public void Activate()
    {
        IsNeutralized = false;
        pendingActivation = true;

        gameObject.SetActive(true);   // 비활성 배치였다면 이 순간 Awake가 실행되며 시작 위치를 기록

        if (resetPositionOnActivate && initialized)
            transform.position = initialPosition;

        advancing = true;

        if (!rushLoopSfx.IsNull)
        {
            loopInstance = SoundManager.Instance.PlayLoop(rushLoopSfx, transform);
            loopPlaying = true;
        }

        Debug.Log("<color=red>[AdvancingTideWall]</color> 밀물이 몰려온다 — 귀환 러시 시작!");
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        advancing = false;

        if (loopPlaying)
        {
            SoundManager.Instance.StopLoop(loopInstance, immediate: false);
            loopPlaying = false;
        }

        gameObject.SetActive(false);
    }

    public void Tick() { }
    public void SetProgress(float t) { }

    private void Update()
    {
        if (!advancing) return;
        transform.position += (Vector3)(advanceDirection.normalized * (advanceSpeed * Time.deltaTime));
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
