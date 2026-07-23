using UnityEngine;
using System.Collections;

public class Gaze : MonoBehaviour, IThreatBehavior
{
    [Header("���� �ʵ�")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private FishMovement movement;

    [Header("�þ�(Gaze) ����")]
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("진단 로그 — 감지가 안 될 때 켜기 (어느 관문에서 막히는지 1초 간격으로 출력)")]
    [SerializeField] private bool debugLog = true;

    private bool isActive = false;
    private float nextDebugTime;
    private float lastTickTime;
    private float nextTickWarnTime;

    public bool IsNeutralized { get; private set; }

    private void OnEnable()
    {
        EventBus.OnNoiseEmitted += HandleNoiseBridge;
    }

    private void OnDisable()
    {
        EventBus.OnNoiseEmitted -= HandleNoiseBridge;
    }

    public void Activate()
    {
        IsNeutralized = false;
        isActive = true;
        if (movement != null) movement.ActivateMovement();
    }

    public void Tick()
    {
        lastTickTime = Time.time;   // Tick이 실제로 불리는지 진단용

        if (!isActive || IsNeutralized) return;

        // 소음 조사 중에도 시야는 항상 살아 있음 — 예전엔 소음 후 10초간 시야를 껐는데(주의분산),
        // 소음이 몬스터를 플레이어 쪽으로 부르는 구조에선 다가와 놓고 눈앞의 플레이어를 못 보는 모순이 됨.
        // 소음을 계속 내면(발전기 등) 분산이 계속 갱신되어 사실상 영구 실명이 되던 문제 수정.
        PerformGazeCheck();
    }

    /// <summary>
    /// 등록 문제 감시 — Tick은 HauntController의 "현재 페이즈" Threat Behaviours에 등록돼 있어야만 불린다.
    /// 등록이 빠졌거나 페이즈가 비활성이면 아래 경고가 5초마다 출력됨 (감지 코드는 아예 실행 기회가 없는 상태).
    /// </summary>
    private void Update()
    {
        if (!debugLog || IsNeutralized) return;
        if (Time.time < 5f || Time.time - lastTickTime < 2f || Time.time < nextTickWarnTime) return;

        nextTickWarnTime = Time.time + 5f;
        Debug.LogWarning("[Gaze] Tick이 호출되지 않고 있음 — HauntController '현재 페이즈'의 Threat Behaviours에 이 Gaze가 등록됐는지, 페이즈가 활성인지 확인할 것");
    }

    private void PerformGazeCheck()
    {
        // 스토리 시퀀스 동안은 시야 판정 유예 — 낭독 중 발각→도약으로 스토리가 끊기지 않게
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsSequenceActive)
        {
            Report("스토리 시퀀스 진행 중 — 시야 판정 유예");
            return;
        }

        if (playerTransform == null || movement == null)
        {
            Report($"참조 누락 — playerTransform:{(playerTransform == null ? "없음" : "OK")}, movement:{(movement == null ? "없음" : "OK")}");
            return;
        }

        if (Concealment.IsPlayerConcealed)
        {
            Report("플레이어 은신 중 (IsPlayerConcealed = true) — 은신처에서 나왔는데도 계속 뜨면 Concealment 플래그가 안 풀린 것");
            LostPlayer();
            return;
        }

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= viewDistance)
        {
            float angle = Vector2.Angle(transform.up, directionToPlayer);
            if (angle <= viewAngle / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask);
                if (hit.collider == null)
                {
                    movement.SetState(FishMovement.BehaviorState.Chase);

                    // 전환 요청이 실제로 먹혔는지 확인 — 도약/돌진 사이클 잠금 중이면 거부될 수 있음(정상),
                    // 사이클이 안 도는데도 계속 거부되면 잠금 플래그가 새고 있는 것(버그)
                    if (movement.CurrentState != FishMovement.BehaviorState.Chase)
                        Report($"발각했지만 Chase 전환 거부됨 — 현재 상태: {movement.CurrentState} (사이클 잠금 중이면 정상)");

                    // 오염은 접촉(ThreatContact)에서만 — 발각 자체는 추격/도약으로 이어지는 신호일 뿐 피해가 아님
                    Debug.DrawLine(transform.position, playerTransform.position, Color.red);
                    return;
                }
                Report($"시선 차단 — '{hit.collider.name}' (레이어 {LayerMask.LayerToName(hit.collider.gameObject.layer)}) 이 가로막음. Obstacle Mask에서 이 레이어를 빼야 할 수도");
            }
            else
            {
                Report($"시야각 밖 — 각도 {angle:F0}° > 허용 {viewAngle / 2f:F0}° (View Angle을 360으로 두면 전방향 감지)");
            }
        }
        else
        {
            Report($"거리 밖 — {distanceToPlayer:F1} > View Distance {viewDistance}");
        }

        LostPlayer();
    }

    /// <summary>진단 출력 (1초 간격 스로틀) — debugLog 끄면 침묵</summary>
    private void Report(string msg)
    {
        if (!debugLog || Time.time < nextDebugTime) return;
        nextDebugTime = Time.time + 1f;
        Debug.Log($"<color=grey>[Gaze 진단]</color> {msg}");
    }

    private void LostPlayer()
    {
        if (movement.CurrentState == FishMovement.BehaviorState.Chase)
        {
            movement.SetState(FishMovement.BehaviorState.Patrol);
        }
    }

    private void HandleNoiseBridge(Vector2 position, float radius)
    {
        if (!isActive || IsNeutralized) return;

        // 스토리 시퀀스 동안 발생한 소음은 무시 — 낭독 중 괴성 윈드업·도약이 끼어들지 않게
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsSequenceActive) return;

        float distanceToNoise = Vector2.Distance((Vector2)transform.position, position);
        if (distanceToNoise <= radius)
        {
            if (movement != null) movement.HandleNoise(position, radius);
        }
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        isActive = false;
        if (movement != null) movement.StopMovement();
    }

    public void SetProgress(float t) { }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.orange;
        Vector3 forward = transform.up;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, viewAngle / 2f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -viewAngle / 2f) * forward;

        Gizmos.DrawRay(transform.position, forward * viewDistance);
        Gizmos.DrawRay(transform.position, leftBoundary * viewDistance);
        Gizmos.DrawRay(transform.position, rightBoundary * viewDistance);
    }
}