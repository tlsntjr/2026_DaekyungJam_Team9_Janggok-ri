using UnityEngine;
using System.Collections;

/// <summary>
/// 인면어 이동/전투 상태 머신.
///   - Chase(1~2페이즈)  : 발각 순간 소형 괴성 1회 → 단순 실시간 추격. 파훼는 은신처.
///   - Berserk(3페이즈)   : "괴성(위치 고정) → 대기 → 관통 돌진 → 경직(두리번) → 범위 안이면 재돌진" 사이클 무한 반복.
/// 괴성 사용 규칙: 발각(추격 시작) = 소형 / 먼 소음(발전기 등)에 반응해 달려올 때 = 대형 / 광폭 = 전부 대형.
/// 돌진 확정 판정(괴성~돌진 종료) 동안은 외부 상태 전환(SetState)과 소음 반응이 잠긴다 —
/// 어그로 풀림/소음 유인 때문에 돌진이 도중에 어정쩡하게 끊기는 것 방지.
/// </summary>
public class FishMovement : MonoBehaviour
{
    public enum BehaviorState { Patrol, Suspect, Chase, Retreat, Berserk }

    [Header("상태 머신")]
    [SerializeField] private BehaviorState currentState = BehaviorState.Patrol;
    public BehaviorState CurrentState => currentState;

    [Header("참조 필드")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private HauntController controller;

    [Header("괴담 연동 (ChaseMusicController 등 huntId 기반 시스템용 — HauntDefinition.huntId와 일치시킬 것)")]
    [SerializeField] private string huntId;

    [Header("순찰(Waypoint) 설정")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointArrivalDistance = 0.2f;
    private int currentWaypointIndex = 0;

    [Header("이동 속도 설정")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float suspectSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float noiseScreamWindup = 1.0f;      // 소음 반응 시 괴성을 지르며 이만큼 정지한 뒤 달려감
    [SerializeField] private float noiseArrivalDistance = 1.2f;   // 소음 지점 도착 판정 거리 — 발전기 등 충돌체가 있는 목표는 정확히 겹칠 수 없으므로 여유를 둠(안 두면 벽/오브젝트에 제자리서 계속 밀려붙는 현상 발생)

    [Header("추격(Chase) — 1~2페이즈: 발각 시 소형 괴성 1회 후 단순 추격 (파훼는 은신)")]
    [SerializeField] private float chaseScreamCooldown = 6f;      // 발각 괴성 재발화 최소 간격 (시야 경계에서 재발각 스팸 방지)

    [Header("광폭(Berserk) 돌진 — 3페이즈: 같은 패턴의 흉폭한 버전")]
    [SerializeField] private float chargeWaitTime = 0.8f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float cooldownTime = 2.0f;
    [SerializeField] private float berserkEntryDelay = 1.2f;      // 광폭 진입 대형 괴성이 들릴 시간 확보 후 첫 돌진

    [Header("돌진 공통")]
    [SerializeField] private float dashDuration = 1.5f;           // 돌진 최대 시간 (목표 도달 시 조기 종료)
    [SerializeField] private float dashOvershoot = 3.5f;          // 고정 위치를 지나쳐 더 나아가는 거리 — 상어가 먹이를 물고 관통하듯
    [SerializeField] private float reDashRange = 5f;              // 경직이 끝난 시점에 이 범위 안에 남아 있으면(은신 제외) 즉시 재돌진
    [SerializeField] private float staggerScanAngle = 50f;        // 경직 중 좌우로 두리번거리는 각도(±)

    [SerializeField] private bool isMovingActive = false;

    private Vector3 dashTarget;
    private bool isCharging = false;   // 괴성~돌진 시작 사이 (위치 고정됨)
    private bool isDashing = false;    // 돌진 이동 중
    private Coroutine dashCycleCoroutine;
    private float berserkEntryUntil;

    private Vector3 targetNoisePosition;
    private Coroutine berserkTimerCoroutine;
    private Coroutine suspectTimerCoroutine;
    private bool hasTriggeredBerserkMode = false;
    private bool isNoiseWindup = false;    // 소음 반응 괴성 중 (제자리 정지)
    private int lastThreatLevel = -1;      // ThreatStateChanged 중복 발화 방지
    private float lastChaseScreamTime = -999f;

    /// <summary>
    /// 상태 변경의 단일 통로 — 상태를 바꾸면서 위협 레벨(0=평시, 1=수상, 2=추격)을 브로드캐스트.
    /// ChaseMusicController가 이 이벤트로 추격 BGM을 켜고 끈다.
    /// Chase 진입(발각) 순간엔 소형 괴성 1회 — 재발각 스팸은 chaseScreamCooldown으로 방지.
    /// </summary>
    private void ApplyState(BehaviorState s)
    {
        bool enteredChase = s == BehaviorState.Chase && currentState != BehaviorState.Chase;
        currentState = s;

        // Suspect를 벗어나는 전환이면 잔여 복귀 타이머를 반드시 정리 —
        // 안 하면 10초 뒤 타이머가 Chase/Berserk 상태를 Patrol로 강등시키면서 긴장 오디오(BGM·심장)를 꺼버림
        if (s != BehaviorState.Suspect && suspectTimerCoroutine != null)
        {
            StopCoroutine(suspectTimerCoroutine);
            suspectTimerCoroutine = null;
            isNoiseWindup = false;
        }

        // 위협 레벨: 0=평시, 1=수상, 2=추격, 3=광폭 — FMOD에서 광폭 전용 레이어를 구분할 수 있게 분리
        // (ChaseMusicController 등 기존 구독자는 >=2 판정이라 영향 없음)
        RaiseThreatLevel(s == BehaviorState.Berserk ? 3
                       : s == BehaviorState.Chase ? 2
                       : s == BehaviorState.Suspect ? 1 : 0);

        if (enteredChase && Time.time - lastChaseScreamTime >= chaseScreamCooldown)
        {
            lastChaseScreamTime = Time.time;
            EventBus.RaiseMonsterScreamed(transform.position);   // 발각! — 소형 괴성 (파동·글리치는 디렉터가 처리)
        }
    }

    private void RaiseThreatLevel(int level)
    {
        if (level == lastThreatLevel) return;
        lastThreatLevel = level;
        if (!string.IsNullOrEmpty(huntId))
            EventBus.RaiseThreatStateChanged(huntId, level);
    }

    /// <summary>괴성~돌진 종료까지 = 확정 판정 구간. 외부 상태 전환·소음 반응을 받지 않음.</summary>
    private bool IsDashLocked => isCharging || isDashing;

    private void OnEnable()
    {
        isMovingActive = true;

        if (controller != null && controller.CurrentPhaseIndex == 2)
        {
            TriggerBerserkModeImmediate();
        }
    }

    private void Start()
    {
        isMovingActive = true;
    }

    private void Update()
    {
        if (controller != null && controller.CurrentPhaseIndex == 2 && !hasTriggeredBerserkMode)
        {
            TriggerBerserkModeImmediate();
        }

        if (!isMovingActive) return;

        ExecuteMovement();
    }

    private void TriggerBerserkModeImmediate()
    {
        hasTriggeredBerserkMode = true;
        isMovingActive = true;

        EventBus.RaiseMonsterScreamed(transform.position, true);   // 광폭 진입 선언 — 대형 괴성
        berserkEntryUntil = Time.time + berserkEntryDelay;         // 대형 괴성과 첫 돌진의 소형 괴성이 겹치지 않게

        ForceState(BehaviorState.Berserk, 60f);
        Debug.Log("<color=red>[FishMovement 광폭 기동]</color> 3페이즈 진입 — 돌진 사이클을 무한 반복합니다!");
    }

    private void ExecuteMovement()
    {
        switch (currentState)
        {
            case BehaviorState.Patrol:
                ExecuteWaypointPatrol();
                break;

            case BehaviorState.Suspect:
                if (isNoiseWindup) return;   // 괴성을 지르는 동안 제자리 — "반응했다"는 텔레그래프

                // 목표 지점 근처(noiseArrivalDistance 이내)에 도착했으면 더 밀고 들어가지 않고 그 자리에서 주시만 함 —
                // 발전기 등 충돌체가 있는 목표는 정확히 겹칠 수 없어서, 여유 없이 계속 MoveTowards하면
                // 벽/오브젝트에 영구히 밀려붙어 "벽 보고 제자리 이동"하는 것처럼 보이는 문제 방지
                if (Vector3.Distance(transform.position, targetNoisePosition) > noiseArrivalDistance)
                    transform.position = Vector3.MoveTowards(transform.position, targetNoisePosition, suspectSpeed * Time.deltaTime);

                LookAtTarget(targetNoisePosition);
                break;

            case BehaviorState.Chase:
                // 1~2페이즈: 단순 추격 — 파훼는 은신처. 돌진 사이클은 광폭(3페이즈) 전용
                if (playerTransform == null) return;
                transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);
                LookAtTarget(playerTransform.position);
                break;

            case BehaviorState.Berserk:
                if (playerTransform == null)
                {
                    playerTransform = GameObject.FindWithTag("Player")?.transform;
                    return;
                }
                if (dashCycleCoroutine != null) return;
                if (Time.time < berserkEntryUntil) return;   // 진입 대형 괴성 연출 시간

                // 광폭 — 매 돌진 대형 괴성 (미친 듯이 반복되는 게 의도)
                dashCycleCoroutine = StartCoroutine(
                    DashCycleRoutine(chargeWaitTime, dashSpeed, cooldownTime, true));
                break;
        }
    }

    /// <summary>
    /// 돌진 사이클 공통 루틴: 괴성(이 순간 플레이어 위치 고정) → 대기 → 고정 위치를 "관통해 지나가는" 돌진
    /// → 경직(제자리에서 좌우로 두리번) → 재돌진 판정.
    /// 상어가 먹이를 물고 그대로 헤엄쳐 지나가듯 고정 위치에서 멈추지 않고 dashOvershoot만큼 더 나아가고,
    /// 경직이 끝났는데 플레이어가 reDashRange 안에 남아 있으면(은신 제외) 접근 없이 즉시 다음 돌진으로 이어진다 —
    /// 경직은 휴식이 아니라 "지금 도망가지 않으면 또 온다"는 압박 구간.
    /// Chase(소형 괴성)/Berserk(대형 괴성)가 파라미터만 달리해 공유. 이동도 이 코루틴이 직접 수행한다.
    /// </summary>
    private IEnumerator DashCycleRoutine(float chargeWait, float dashSpd, float cooldown, bool bigScream)
    {
        while (true)
        {
            if (playerTransform == null) break;

            // 1) 괴성 + 위치 고정 — "지금 서 있으면 맞는다"는 텔레그래프
            isCharging = true;
            EventBus.RaiseMonsterScreamed(transform.position, bigScream);

            Vector3 lockedPos = playerTransform.position;
            Vector3 dashDir = lockedPos - transform.position;
            dashDir = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : transform.up;
            dashTarget = lockedPos + dashDir * dashOvershoot;   // 고정 위치를 지나쳐 관통하는 지점까지
            LookAtTarget(dashTarget);
            Debug.Log($"<color=red>[FishMovement]</color> 괴성({(bigScream ? "대형" : "소형")})! 위치 고정 → {chargeWait}초 후 돌진");

            yield return new WaitForSeconds(chargeWait);

            // 2) 고정 위치로 관통 돌진 (실시간 추적 아님 — 회피가 성립하는 이유)
            isCharging = false;
            isDashing = true;

            float elapsed = 0f;
            while (elapsed < dashDuration && Vector3.Distance(transform.position, dashTarget) > 0.1f)
            {
                if (!isMovingActive) break;   // StopMovement 등 외부 정지 존중

                elapsed += Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpd * Time.deltaTime);
                yield return null;
            }

            isDashing = false;
            if (!isMovingActive) break;

            // 3) 경직 — 제자리에서 좌우로 두리번거리며 주변을 살핌 (이동·상태 전환은 잠긴 상태)
            Debug.Log($"<color=yellow>[FishMovement]</color> 돌진 종료 → 경직 {cooldown}초 (주변 탐색)");
            float baseAngle = transform.eulerAngles.z;
            float t = 0f;
            while (t < cooldown)
            {
                if (!isMovingActive) break;

                t += Time.deltaTime;
                transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + Mathf.Sin(t * 3f) * staggerScanAngle);
                yield return null;
            }
            if (!isMovingActive) break;

            // 4) 재돌진 판정 — 경직이 끝났는데 아직 범위 안이면(은신 중이면 제외) 접근 없이 바로 다음 돌진
            bool stillClose = playerTransform != null
                && !Concealment.IsPlayerConcealed
                && Vector3.Distance(transform.position, playerTransform.position) <= reDashRange;
            if (!stillClose) break;

            Debug.Log("<color=red>[FishMovement]</color> 경직 종료 — 아직 범위 안! 재돌진!");
        }

        // 사이클 종료 — "물었나?" 확인하듯 플레이어 쪽으로 몸을 돌리고, 재발견/상태 판정은 Gaze에 넘김
        // (관통 돌진 후 등지고 있으면 Gaze 시야에 안 걸려 바로 순찰로 새는 것 방지)
        if (playerTransform != null) LookAtTarget(playerTransform.position);

        dashCycleCoroutine = null;
    }

    // GameObject 비활성화 시 코루틴은 조용히 죽지만 참조·플래그는 남음 —
    // 정리하지 않으면 재활성화 후 dashCycleCoroutine != null 로 영구 정지(이동 불능)됨
    private void OnDisable()
    {
        dashCycleCoroutine = null;
        isCharging = false;
        isDashing = false;
        isNoiseWindup = false;
    }

    private void ExecuteWaypointPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 targetWaypointPos = waypoints[currentWaypointIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, targetWaypointPos, patrolSpeed * Time.deltaTime);
        LookAtTarget(targetWaypointPos);

        if (Vector3.Distance(transform.position, targetWaypointPos) <= waypointArrivalDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    public void SetState(BehaviorState newState)
    {
        if (currentState == BehaviorState.Berserk) return;
        // 사이클 전체(경직 포함) 동안 외부 전환 잠금 — 경직 중 Gaze가 "놓쳤다"고 Patrol로 돌려서
        // 걸어 나가는 것 방지. Gaze는 매 프레임 재시도하므로 사이클이 끝나면 올바른 상태로 수렴함.
        if (dashCycleCoroutine != null) return;

        if (currentState == BehaviorState.Chase && newState == BehaviorState.Patrol)
        {
            currentWaypointIndex = GetClosestWaypointIndex();
        }

        ApplyState(newState);
    }

    private int GetClosestWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return 0;
        int closestIndex = 0;
        float closestDistance = Mathf.Infinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float dist = Vector3.Distance(transform.position, waypoints[i].position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    public void HandleNoise(Vector2 position, float radius)
    {
        if (currentState == BehaviorState.Berserk)
        {
            Debug.Log("<color=grey>[FishMovement]</color> 소음 무시 — 광폭 중 (의도된 동작)");
            return;
        }
        if (IsDashLocked)
        {
            Debug.Log("<color=grey>[FishMovement]</color> 소음 무시 — 돌진 판정 중");
            return;
        }

        float distanceToNoise = Vector2.Distance((Vector2)transform.position, position);
        if (distanceToNoise > radius)
        {
            Debug.Log($"<color=grey>[FishMovement]</color> 소음 무시 — 반경 밖 (거리 {distanceToNoise:F1} > 반경 {radius})");
            return;
        }

        targetNoisePosition = new Vector3(position.x, position.y, transform.position.z);

        if (Mathf.Approximately(position.x, 0f) && Mathf.Approximately(position.y, 0f))
        {
            Generator[] generators = FindObjectsByType<Generator>(FindObjectsSortMode.None);
            foreach (Generator gen in generators)
            {
                if (gen != null && !gen.IsSatisfied)
                {
                    targetNoisePosition = new Vector3(gen.transform.position.x, gen.transform.position.y, transform.position.z);
                    break;
                }
            }
        }

        // 먼 소음(발전기 가동 등)에 처음 반응하는 순간 — 대형 괴성 + 윈드업 후 달려감.
        // 이미 Suspect로 이동 중일 땐 괴성·윈드업 없이 목표만 갱신 (연속 소음에 괴성 연발 방지)
        bool newEntry = currentState != BehaviorState.Suspect;
        if (newEntry)
            EventBus.RaiseMonsterScreamed(transform.position, true);

        // 이전 윈드업 루틴이 도중에 교체될 때 isNoiseWindup=true가 남아
        // Suspect 이동이 영구 정지되는 누수 방지 — 새 루틴이 필요하면 다시 켠다
        isNoiseWindup = false;

        if (suspectTimerCoroutine != null) StopCoroutine(suspectTimerCoroutine);
        suspectTimerCoroutine = StartCoroutine(SuspectTimerRoutine(newEntry));

        Debug.Log($"<color=orange>[FishMovement]</color> 소음 반응! → {targetNoisePosition} (신규 진입: {newEntry})");
    }

    private IEnumerator SuspectTimerRoutine(bool withWindup)
    {
        ApplyState(BehaviorState.Suspect);

        // 괴성 윈드업 — 소음 쪽으로 몸을 돌리고 울부짖는 동안 정지, 그 후 내달림
        if (withWindup)
        {
            isNoiseWindup = true;
            LookAtTarget(targetNoisePosition);
            yield return new WaitForSeconds(noiseScreamWindup);
            isNoiseWindup = false;
        }

        yield return new WaitForSeconds(10f);
        ApplyState(BehaviorState.Patrol);
    }

    public void ForceState(BehaviorState newState, float duration)
    {
        ApplyState(newState);
        if (newState == BehaviorState.Berserk)
        {
            if (berserkTimerCoroutine != null) StopCoroutine(berserkTimerCoroutine);
            berserkTimerCoroutine = StartCoroutine(BerserkTimerRoutine(duration));
        }
    }

    private IEnumerator BerserkTimerRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (currentState == BehaviorState.Berserk && controller != null)
        {
            controller.Fail(FailReason.Caught);
        }
    }

    public void ActivateMovement()
    {
        isMovingActive = true;
        if (currentState != BehaviorState.Berserk)
        {
            ApplyState(BehaviorState.Patrol);
            currentWaypointIndex = 0;
        }
    }

    public void StopMovement()
    {
        isMovingActive = false;
        if (berserkTimerCoroutine != null) StopCoroutine(berserkTimerCoroutine);
        if (suspectTimerCoroutine != null) StopCoroutine(suspectTimerCoroutine);

        // 돌진 사이클도 완전히 정리 — 플래그가 남으면 재가동 시 상태 잠금이 풀리지 않음
        if (dashCycleCoroutine != null)
        {
            StopCoroutine(dashCycleCoroutine);
            dashCycleCoroutine = null;
        }
        isCharging = false;
        isDashing = false;
        isNoiseWindup = false;

        RaiseThreatLevel(0);   // 위협 비활성화 — 추격 BGM 페이드아웃
    }

    private void LookAtTarget(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
