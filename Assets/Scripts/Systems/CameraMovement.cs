using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target Player")]
    [SerializeField] private Transform playerTransform;

    [Header("Lerp speed")]
    [SerializeField] private float lerpSpeed = 0.1f;

    /// <summary>
    /// 카메라 흔들림 오프셋 (병합 시 유실 복구).
    /// CameraShake가 값을 넣고, 추적 위치에 합산됨 — 직접 position을 건드리면 추적 Lerp와 충돌.
    /// </summary>
    public Vector3 ShakeOffset { get; set; }

    private Vector3 basePosition;   // 쉐이크가 섞이지 않은 순수 추적 위치

    private void Start()
    {
        basePosition = new Vector3(transform.position.x, transform.position.y, -10);
    }

    private void FixedUpdate()
    {
        // 추적은 basePosition 기준 — 직전 프레임 쉐이크가 추적에 누적되는 드리프트 방지
        basePosition = Vector3.Lerp(
            basePosition,
            new Vector3(playerTransform.position.x, playerTransform.position.y, -10),
            lerpSpeed);

        transform.position = basePosition + ShakeOffset;
    }
}
