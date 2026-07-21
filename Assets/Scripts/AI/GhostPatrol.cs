using UnityEngine;
using System.Collections;

public class GhostPatrol : MonoBehaviour
{
    private enum GhostState { Patrol, Suspect, Wait }

    [Header("���� ����")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 2f;
    private int currentTargetIndex = 0;
    private GhostState currentState = GhostState.Patrol;
    private Coroutine waitCoroutine;

    [Header("AI ����")]
    [SerializeField] private float hearRadius = 10f; // �ͽ��� û�� ����

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
        // 1. �ͽŰ� ���� �߻� ���� ������ �Ÿ� ���
        float distance = Vector2.Distance(transform.position, noisePos);

        // 2. 소리의 도달 반경(이벤트 radius)과 유령 청각 반경 중 좁은 쪽으로 판정 —
        //    이벤트 반경을 무시하면 "조개 소리는 반경 5까지"라는 설계가 깨져서
        //    로그(무시)와 실제 행동(반응)이 어긋나는 문제가 생김
        float effectiveRadius = Mathf.Min(radius, hearRadius);
        if (distance <= effectiveRadius)
        {
            Debug.Log($"<color=cyan>[Ghost]</color> ���� ����! �Ÿ�: {distance:F2}");

            if (waitCoroutine != null) StopCoroutine(waitCoroutine);
            waitCoroutine = StartCoroutine(DistractionRoutine(noisePos));
        }
        else
        {
            // ���� ���̸� ����
            Debug.Log($"<color=gray>[Ghost]</color> �ʹ� �� �Ҹ��� ������. �Ÿ�: {distance:F2}");
        }
    }

    private IEnumerator DistractionRoutine(Vector2 targetPos)
    {
        currentState = GhostState.Suspect;

        // 1. ���� �������� �̵�
        while (Vector2.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        // 2. 7�ʰ� ���
        currentState = GhostState.Wait;
        yield return new WaitForSeconds(7.0f);

        // 3. ���� ����
        currentState = GhostState.Patrol;
    }
}