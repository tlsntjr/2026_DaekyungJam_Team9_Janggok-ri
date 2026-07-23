using UnityEngine;
using System.Collections;

public class GhostPatrol : MonoBehaviour
{
    private enum GhostState { Patrol, Chase, Suspect, Wait }

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

    private void OnEnable() => EventBus.OnNoiseEmitted += HandleNoise;
    private void OnDisable() => EventBus.OnNoiseEmitted -= HandleNoise;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
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
        if (distance <= detectRadius)
        {
            // 아이 귀신이 발견하여 쫓는 소리
            // EventBus.RaiseMonsterScreamed(transform.position);
            Debug.Log("<color=red>[Ghost]</color> 플레이어 발견! 추적(Chase) 개시!");

            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
                currentRoutine = null;
            }

            currentState = GhostState.Chase;
        }
    }

    private void DoChase()
    {
        if (playerTransform == null)
        {
            currentState = GhostState.Patrol;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, playerTransform.position) > detectRadius * 2f)
        {
            Debug.Log("<color=gray>[Ghost]</color> 플레이어를 놓쳤습니다. 순찰로 복귀합니다.");
            currentState = GhostState.Patrol;
        }
    }

    private void HandleNoise(Vector2 noisePos, float radius)
    {
        float distance = Vector2.Distance(transform.position, noisePos);
        float effectiveRadius = Mathf.Min(radius, hearRadius);

        if (distance <= effectiveRadius)
        {
            Debug.Log($"<color=cyan>[Ghost]</color> 소음 감지! 기존 행동을 중단하고 소리 난 곳으로 이동. 거리: {distance:F2}");

            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }

            currentRoutine = StartCoroutine(DistractionRoutine(noisePos));
        }
        else
        {
            Debug.Log($"<color=gray>[Ghost]</color> 너무 먼 소리라 무시함. 거리: {distance:F2}");
        }
    }

    private IEnumerator DistractionRoutine(Vector2 targetPos)
    {
        currentState = GhostState.Suspect;

        while (Vector2.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseSpeed * Time.deltaTime);
            yield return null;
        }

        currentState = GhostState.Wait;
        yield return new WaitForSeconds(7.0f);

        if (stayAtLure)
        {
            Debug.Log("<color=yellow>[Ghost]</color> 유인된 자리에 계속 머물러 있습니다.");
        }
        else
        {
            currentState = GhostState.Patrol;
            Debug.Log("<color=gray>[Ghost]</color> 7초 대기 완료. 순찰로 복귀합니다.");
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