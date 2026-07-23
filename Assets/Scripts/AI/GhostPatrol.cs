using UnityEngine;
using System.Collections;
using FMODUnity;

public class GhostPatrol : MonoBehaviour
{
    private enum GhostState { Patrol, Chase, Suspect, Wait }

    [Header("괴담 연동 (ChaseMusicController 등 huntId 기반 시스템용 — HauntDefinition.huntId와 일치시킬 것)")]
    [SerializeField] private string huntId;

    [Header("순찰 설정")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseSpeed = 2f;
    private int currentTargetIndex = 0;
    private GhostState currentState = GhostState.Patrol;
    private Coroutine currentRoutine;

    [Header("추적(Sight) 설정")]
    [SerializeField] private float detectRadius = 3f;
    private Transform playerTransform;

    [Header("청각(Sound) 설정")]
    [SerializeField] private float hearRadius = 10f;

    [Header("옵션")]
    [SerializeField] private bool stayAtLure = false;

    [Header("FMOD 사운드")]
    [SerializeField] private EventReference chaseSfx;

    // 위협 레벨 캐싱 (중복 이벤트 방송 방지용)
    private int currentThreatLevel = -1;

    private void OnEnable()
    {
        EventBus.OnNoiseEmitted += HandleNoise;
    }

    private void OnDisable()
    {
        EventBus.OnNoiseEmitted -= HandleNoise;
        // 비활성화 시 안전하게 위협 레벨 0으로 초기화
        SetThreatLevel(0);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // 시작 시 기본 상태(Patrol) 적용 (자동으로 레벨 0 세팅됨)
        ChangeState(GhostState.Patrol);
    }

    void Update()
    {
        if (currentState == GhostState.Patrol)
        {
            CheckPlayerDetection();
            DoPatrol();
        }
        else if (currentState == GhostState.Chase)
        {
            DoChase();
        }
    }

    // ==========================================
    // 상태 및 3단계 위협 레벨 관리부 (핵심 수정됨)
    // ==========================================
    private void ChangeState(GhostState newState)
    {
        currentState = newState;

        int newThreatLevel = 0;

        // 상태에 따른 위협 레벨 할당 (0: 평시, 1: 의심, 2: 추적)
        switch (newState)
        {
            case GhostState.Patrol:
                newThreatLevel = 0; // 평시 순찰
                break;
            case GhostState.Suspect:
            case GhostState.Wait:
                newThreatLevel = 1; // 소음 발생 지점으로 유인 및 대기 (의심)
                break;
            case GhostState.Chase:
                newThreatLevel = 2; // 플레이어 발견 및 맹추격
                break;
        }

        SetThreatLevel(newThreatLevel);
    }

    private void SetThreatLevel(int level)
    {
        // 이미 같은 레벨이면 방송하지 않음
        if (currentThreatLevel == level) return;

        currentThreatLevel = level;
        EventBus.RaiseThreatStateChanged(huntId, level);
        Debug.Log($"<color=magenta>[GhostPatrol]</color> {huntId} 위협 레벨 변경: Level {level}");
    }

    // ==========================================
    // 이동 및 추적 로직
    // ==========================================
    private void DoPatrol()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentTargetIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentTargetIndex = (currentTargetIndex + 1) % waypoints.Length;
        }
    }

    private void CheckPlayerDetection()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= detectRadius && currentState != GhostState.Chase)
        {
            Debug.Log("<color=red>[Ghost]</color> 플레이어 발견! 추적(Chase) 개시!");

            EventBus.RaiseMonsterScreamed(transform.position);

            if (!chaseSfx.IsNull)
            {
                SoundManager.Instance.PlayOneShot(chaseSfx, transform.position);
            }

            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
                currentRoutine = null;
            }

            // 상태 변경 -> 자동으로 레벨 2 방송됨
            ChangeState(GhostState.Chase);
        }
    }

    private void DoChase()
    {
        if (playerTransform == null)
        {
            ChangeState(GhostState.Patrol);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, playerTransform.position) > detectRadius * 2f)
        {
            Debug.Log("<color=gray>[Ghost]</color> 플레이어를 놓쳤습니다. 순찰로 복귀합니다.");

            // 상태 변경 -> 자동으로 레벨 0 방송됨
            ChangeState(GhostState.Patrol);
        }
    }

    // ==========================================
    // 소음 반응 로직
    // ==========================================
    private void HandleNoise(Vector2 noisePos, float radius)
    {
        float distance = Vector2.Distance(transform.position, noisePos);
        float effectiveRadius = Mathf.Min(radius, hearRadius);

        if (distance <= effectiveRadius)
        {
            Debug.Log($"<color=cyan>[Ghost]</color> 소음 감지! 기존 행동 중단 후 이동. 거리: {distance:F2}");

            if (currentRoutine != null) StopCoroutine(currentRoutine);
            currentRoutine = StartCoroutine(DistractionRoutine(noisePos));
        }
    }

    private IEnumerator DistractionRoutine(Vector2 targetPos)
    {
        // 상태 변경 -> 자동으로 레벨 1 방송됨 (의심 시작)
        ChangeState(GhostState.Suspect);

        while (Vector2.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * Time.deltaTime);
            yield return null;
        }

        // 상태 변경 -> 여전히 레벨 1 유지 (수색 중)
        ChangeState(GhostState.Wait);
        yield return new WaitForSeconds(7.0f);

        if (!stayAtLure)
        {
            // 상태 변경 -> 자동으로 레벨 0 방송됨 (의심 해제, 순찰 복귀)
            ChangeState(GhostState.Patrol);
        }

        currentRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearRadius);
    }
}