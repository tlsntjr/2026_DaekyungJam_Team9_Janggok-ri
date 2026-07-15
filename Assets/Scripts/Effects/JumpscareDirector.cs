using System.Collections;
using UnityEngine;
using FMODUnity;

/// <summary>
/// 오염 2단계 진입 시 랜덤 간격으로 풀스크린 플래시(+실루엣, +효과음)를 재생한다.
/// 실루엣 스프라이트/효과음은 아직 없으면 필드를 비워두면 스킵된다.
/// </summary>
public class JumpscareDirector : MonoBehaviour
{
    [Header("연출 대상 (없으면 스킵)")]
    [SerializeField] private CanvasGroup flashOverlay;
    [SerializeField] private Sprite silhouette;
    [SerializeField] private SpriteRenderer silhouetteRenderer;

    [Header("사운드 (팀장에게 요청 예정, 비워두면 스킵)")]
    [SerializeField] private EventReference jumpscareSfx;

    [Header("타이밍")]
    [SerializeField] private float minInterval = 4f;
    [SerializeField] private float maxInterval = 10f;
    [SerializeField] private float stage3FrequencyMultiplier = 1.5f;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float silhouetteDuration = 0.3f;

    private Coroutine loop;
    private int currentStage;

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
