using UnityEngine;

public class VisionAim : MonoBehaviour
{
    [Header("Camera for aim")]
    [SerializeField] private Camera cam;
    [SerializeField] private float visionTurnSpeed = 720f;

    // 메인 카메라 리셋
    private void Reset() => cam = Camera.main;


    private void Update()
    {
        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)(m - transform.position);

        // 작은 변화는 무시
        if (dir.sqrMagnitude < 0.001f) return;

        // spot light 2d 기본 방향 +y, 보정 필요함
        float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        float z = Mathf.MoveTowardsAngle(
            transform.eulerAngles.z,
            target,
            visionTurnSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0, 0, z);
    }

}
