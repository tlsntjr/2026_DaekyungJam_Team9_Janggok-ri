using UnityEngine;
using System.Collections;

public class FishMovement : MonoBehaviour
{
    public enum BehaviorState { Patrol, Suspect, Chase, Retreat, Berserk }

    [Header("상태 머신")]
    [SerializeField] private BehaviorState currentState = BehaviorState.Patrol;
    public BehaviorState CurrentState => currentState;

    [Header("연결 필드")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private HauntController controller;

    [Header("순찰(Waypoint) 설정")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointArrivalDistance = 0.2f;
    private int currentWaypointIndex = 0;

    [Header("이동 속도 세팅")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float suspectSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float berserkSpeed = 2f;

    [SerializeField] private bool isMovingActive = false;
    private Vector3 targetNoisePosition;
    private Coroutine berserkTimerCoroutine;
    private Coroutine suspectTimerCoroutine;
    private bool hasTriggeredBerserkMode = false;

    private float berserkDirectionTimer = 0f;
    private Vector3 berserkRandomOffset = Vector3.zero;

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

        ForceState(BehaviorState.Berserk, 60f);
        Debug.Log("<color=red>[ FishMovement 안전 기동]</color> 3단계 진입이 확인되었습니다. 정지 버그를 우회하여 즉시 광폭화 돌진을 개시합니다!");
    }

    private void ExecuteMovement()
    {
        switch (currentState)
        {
            case BehaviorState.Patrol:
                ExecuteWaypointPatrol();
                break;

            case BehaviorState.Suspect:
                transform.position = Vector3.MoveTowards(transform.position, targetNoisePosition, suspectSpeed * Time.deltaTime);
                LookAtTarget(targetNoisePosition);
                break;

            case BehaviorState.Chase:
                if (playerTransform == null) return;
                transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);
                LookAtTarget(playerTransform.position);
                break;

            case BehaviorState.Berserk:
                if (playerTransform != null)
                {
                    ExecuteBerserkMovement();
                }
                else
                {
                    playerTransform = GameObject.FindWithTag("Player")?.transform;
                }
                break;
        }
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

    private void ExecuteBerserkMovement()
    {
        berserkDirectionTimer += Time.deltaTime;
        if (berserkDirectionTimer >= 0.3f)
        {
            berserkDirectionTimer = 0f;
            berserkRandomOffset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0f);
        }

        Vector3 berserkTargetPos = playerTransform.position + berserkRandomOffset;

        transform.position = Vector3.MoveTowards(transform.position, berserkTargetPos, berserkSpeed * Time.deltaTime);
        LookAtTarget(berserkTargetPos);

        Debug.DrawLine(transform.position, playerTransform.position, Color.red);
    }

    public void SetState(BehaviorState newState)
    {
        if (currentState == BehaviorState.Berserk) return;

        if (currentState == BehaviorState.Chase && newState == BehaviorState.Patrol)
        {
            currentWaypointIndex = GetClosestWaypointIndex();
        }

        currentState = newState;
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
        if (currentState == BehaviorState.Berserk) return;

        float distanceToNoise = Vector2.Distance((Vector2)transform.position, position);
        if (distanceToNoise <= radius)
        {
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

            if (suspectTimerCoroutine != null) StopCoroutine(suspectTimerCoroutine);
            suspectTimerCoroutine = StartCoroutine(SuspectTimerRoutine());
        }
    }

    private IEnumerator SuspectTimerRoutine()
    {
        currentState = BehaviorState.Suspect;
        yield return new WaitForSeconds(10f);
        currentState = BehaviorState.Patrol;
    }

    public void ForceState(BehaviorState newState, float duration)
    {
        currentState = newState;
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
            currentState = BehaviorState.Patrol;
            currentWaypointIndex = 0;
        }
    }

    public void StopMovement()
    {
        isMovingActive = false;
        if (berserkTimerCoroutine != null) StopCoroutine(berserkTimerCoroutine);
        if (suspectTimerCoroutine != null) StopCoroutine(suspectTimerCoroutine);
    }

    private void LookAtTarget(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}