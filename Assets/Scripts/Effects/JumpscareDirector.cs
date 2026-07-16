using System.Collections;
using UnityEngine;
using FMODUnity;

/// <summary>
/// 오염 2단계 진입 시 랜덤 간격으로 풀스크린 플래시(+실루엣, +효과음)를 재생한다.
/// 실루엣 스프라이트/효과음은 아직 없으면 필드를 비워두면 스킵된다.
/// </summary>
public class JumpscareDirector : MonoBehaviour
{
    [Header("연출 대상(없으면 스킵)")]
    [SerializeField] private CanvasGroup flashOverlay;
    [SerializeField] private Sprite silhouette;
    [SerializeField] private SpriteRenderer silhouetteRenderer;

    [Header("사운드")]
    [SerializeField] private EventReference jumpscareSfx;

    [Header("타이밍")]
    [SerializeField] private float minInterval = 4f;
    [SerializeField] private float maxInterval = 10f;
    [SerializeField] private float stage3FrequencyMultiplier = 1.5f;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float silhouetteDuration = 0.3f;

    [Header("3단계 화면 흔들림")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    private Coroutine loop;
    private int currentStage;
    private float shakeTimeRemaining;

    private void OnEnable()  => EventBus.OnContaminationStageChanged += HandleStageChanged;
    private void OnDisable() => EventBus.OnContaminationStageChanged -= HandleStageChanged;

    private void HandleStageChanged(int stage)
    {
        currentStage = stage;

        if (stage >= 2 && loop == null)
            loop = StartCoroutine(ScareLoop());
        else if (stage < 2 && loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator ScareLoop()
    {
        while (true)
        {
            float interval = Random.Range(minInterval, maxInterval);
            if (currentStage >= 3)
                interval /= stage3FrequencyMultiplier;

            yield return new WaitForSeconds(interval);
            TriggerScare();
        }
    }

    private void TriggerScare()
    {
        if (flashOverlay != null)
            StartCoroutine(FlashRoutine());

        if (silhouette != null && silhouetteRenderer != null)
            StartCoroutine(SilhouetteRoutine());

        if (!jumpscareSfx.IsNull)
            SoundManager.Instance.PlayOneShot(jumpscareSfx, Camera.main.transform.position);

        if (currentStage >= 3)
            shakeTimeRemaining = shakeDuration;
    }

    // CameraMovement가 FixedUpdate에서 카메라 위치를 잡은 뒤, 그 위에 흔들림을 덧씌운다.
    private void LateUpdate()
    {
        if (shakeTimeRemaining <= 0f) return;

        shakeTimeRemaining -= Time.deltaTime;
        Vector3 shakeOffset = (Vector3)Random.insideUnitCircle * shakeMagnitude;
        Camera.main.transform.position += shakeOffset;
    }

    private IEnumerator FlashRoutine()
    {
        flashOverlay.alpha = 1f;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            flashOverlay.alpha = Mathf.Lerp(1f, 0f, t / flashDuration);
            yield return null;
        }
        flashOverlay.alpha = 0f;
    }

    private IEnumerator SilhouetteRoutine()
    {
        silhouetteRenderer.sprite = silhouette;
        silhouetteRenderer.enabled = true;
        yield return new WaitForSeconds(silhouetteDuration);
        silhouetteRenderer.enabled = false;
    }
}
