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
        // 이미 멈춰있거나 소음 지점으로 이동 중일 수 있으니 상태 갱신
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);

        StartCoroutine(DistractionRoutine(noisePos));
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