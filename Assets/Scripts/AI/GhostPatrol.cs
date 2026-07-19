using UnityEngine;

public class GhostPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints; // 이동할 지점들
    [SerializeField] private float speed = 2f;      // 기획 의도에 맞춰 조정
    private int currentTargetIndex = 0;

    void Update()
    {
        if (waypoints.Length == 0) return;

        // 현재 타겟으로 이동
        Transform target = waypoints[currentTargetIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // 도착 시 다음 타겟으로 변경
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentTargetIndex = (currentTargetIndex + 1) % waypoints.Length;
        }
    }
}