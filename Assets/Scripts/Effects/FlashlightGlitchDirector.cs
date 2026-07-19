using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 오염도 2단계/3단계인 동안 랜덤 간격으로 손전등(Light2D)에 글리치 연출을 반복한다.
/// 2단계: 랜덤 타이밍마다 잠깐(1.5초) 불규칙하게 깜빡임.
/// 3단계: 랜덤 타이밍마다 완전히 꺼졌다가 약 2초 뒤 다시 켜짐.
/// </summary>
public class FlashlightGlitchDirector : MonoBehaviour
{
    [Header("손전등 (없으면 스킵)")]
    [SerializeField] private Light2D flashlight;

    [Header("2단계 랜덤 깜빡임")]
    [SerializeField] private float stage2MinInterval = 4f;
    [SerializeField] private float stage2MaxInterval = 10f;
    [SerializeField] private float flickerDuration = 1.5f;
    [SerializeField] private float flickerMinIntensity = 0.2f;
    [SerializeField] private float flickerStep = 0.05f;

    [Header("3단계 랜덤 완전 꺼짐")]
    [SerializeField] private float stage3MinInterval = 4f;
    [SerializeField] private float stage3MaxInterval = 10f;
    [SerializeField] private float blackoutDuration = 2f;

    private float baseIntensity;
    private Coroutine loop;
    private int currentStage;

    private void Awake()
    {
        if (flashlight != null)
            baseIntensity = flashlight.intensity;
    }

    private void OnEnable()  => EventBus.OnContaminationStageChanged += HandleStageChanged;
    private void OnDisable() => EventBus.OnContaminationStageChanged -= HandleStageChanged;

    private void HandleStageChanged(int stage)
    {
        currentStage = stage;

        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }

        if (stage >= 2)
            loop = StartCoroutine(GlitchLoop());
        else if (flashlight != null)
            flashlight.intensity = baseIntensity;
    }

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            bool isStage3 = currentStage >= 3;
            float interval = isStage3
                ? Random.Range(stage3MinInterval, stage3MaxInterval)
                : Random.Range(stage2MinInterval, stage2MaxInterval);

            yield return new WaitForSeconds(interval);

            if (flashlight == null) continue;

            if (currentStage >= 3)
                yield return BlackoutRoutine();
            else
                yield return FlickerRoutine();
        }
    }

    private IEnumerator FlickerRoutine()
    {
        float t = 0f;
        while (t < flickerDuration)
        {
            t += flickerStep;
            flashlight.intensity = Random.Range(flickerMinIntensity, baseIntensity);
            yield return new WaitForSeconds(flickerStep);
        }
        flashlight.intensity = baseIntensity;
    }

    private IEnumerator BlackoutRoutine()
    {
        flashlight.intensity = 0f;

        yield return new WaitForSeconds(blackoutDuration);

        flashlight.intensity = baseIntensity;
    }
}
