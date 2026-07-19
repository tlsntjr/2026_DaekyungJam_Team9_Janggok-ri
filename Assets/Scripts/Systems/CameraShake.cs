using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라 흔들림 유틸리티
/// 카메라 흔듦 효과 발생 시 해당 인스턴스 호출
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("추적 카메라")]
    [SerializeField] private CameraMovement cameraMovement;

    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 카메라 흔들기 
    /// 이미 흔들리는 중이면 새 흔들림으로 교체
    /// </summary>
    /// <param name="duration">지속 시간</param>
    /// <param name="magnitude">최대 흔들림 거리</param>
    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float damper = 1f - (elapsed / duration);
            cameraMovement.ShakeOffset = (Vector3)(Random.insideUnitCircle * magnitude * damper);

            yield return null;
        }

        cameraMovement.ShakeOffset = Vector3.zero;
        shakeCoroutine = null;
    }
}
