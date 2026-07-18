using UnityEngine;
using System.Collections;

public class GhostPatrol : MonoBehaviour
{
    private enum GhostState { Patrol, Suspect, Wait }

    [Header("순찰 설정")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 2f;
    private int currentTargetIndex = 0;
    private GhostState currentState = GhostState.Patrol;
    private Coroutine waitCoroutine;

    [Header("AI 설정")]
    [SerializeField] private float hearRadius = 10f; // 귀신의 청각 범위

    private void OnEnable() => EventBus.OnNoiseEmitted += HandleNoise;
    private void OnDisable() => EventBus.OnNoiseEmitted -= HandleNoise;

    void Update()
    {
        if (currentState == GhostState.Patrol)
        {
            DoPatrol();
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

    private void HandleNoise(Vector2 noisePos, float radius)
    {
        // 1. 귀신과 소음 발생 지점 사이의 거리 계산
        float distance = Vector2.Distance(transform.position, noisePos);

        // 2. 청각 범위 내에 있을 때만 반응
        if (distance <= hearRadius)
        {
            Debug.Log($"<color=cyan>[Ghost]</color> 소음 감지! 거리: {distance:F2}");

            if (waitCoroutine != null) StopCoroutine(waitCoroutine);
            waitCoroutine = StartCoroutine(DistractionRoutine(noisePos));
        }
        else
        {
            // 범위 밖이면 무시
            Debug.Log($"<color=gray>[Ghost]</color> 너무 먼 소리라 무시함. 거리: {distance:F2}");
        }
    }

    private IEnumerator DistractionRoutine(Vector2 targetPos)
    {
        currentState = GhostState.Suspect;

        // 1. 소음 지점으로 이동
        while (Vector2.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        // 2. 7초간 대기
        currentState = GhostState.Wait;
        yield return new WaitForSeconds(7.0f);

        // 3. 순찰 복귀
        currentState = GhostState.Patrol;
    }
}