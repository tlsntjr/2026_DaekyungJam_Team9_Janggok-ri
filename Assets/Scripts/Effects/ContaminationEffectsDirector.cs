using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 오염도 값(0~1)에 비례해 안개 파티클, 풀스크린 왜곡, Vignette/색보정을 동시에 구동한다.
/// 안개 파티클/왜곡 머티리얼은 아직 준비되지 않았으면 필드를 비워두면 스킵된다.
/// </summary>
public class ContaminationEffectsDirector : MonoBehaviour
{
    [Header("안개 파티클 (없으면 스킵)")]
    [SerializeField] private ParticleSystem fogParticles;
    [SerializeField] private float fogMaxRateOverTime = 20f;

    [Header("풀스크린 왜곡 (없으면 스킵, Full Screen Pass Renderer Feature의 Pass Material)")]
    [SerializeField] private Material distortionMaterial;
    [SerializeField] private float maxDistortion = 1f;

    [Header("뿌연 시야 (없으면 스킵, 화면을 덮는 반투명 흰색 오버레이 CanvasGroup)")]
    [SerializeField] private CanvasGroup hazeOverlay;
    [SerializeField] private float maxHazeAlpha = 0.6f;

    [Header("Vignette / 색보정 (씬의 Global Volume, 없으면 스킵)")]
    [SerializeField] private Volume volume;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private float minSaturation = -60f;

    [Header("강도 곡선 (1보다 크면 3단계 근처에서 더 가파르게 강해짐)")]
    [SerializeField] private float intensityCurvePower = 1.5f;

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (volume == null || volume.profile == null) return;

        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;

        if (!volume.profile.TryGet(out colorAdjustments))
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.overrideState = true;
    }

    private void OnEnable()  => EventBus.OnContaminationChanged += HandleContaminationChanged;
    private void OnDisable() => EventBus.OnContaminationChanged -= HandleContaminationChanged;

    private void HandleContaminationChanged(float value)
    {
        // 낮은 오염도에선 약하게, 3단계(0.9~1.0) 근처에선 훨씬 가파르게 강해지는 곡선
        float intensity = Mathf.Pow(value, intensityCurvePower);

        if (fogParticles != null)
        {
            var emission = fogParticles.emission;
            emission.rateOverTimeMultiplier = fogMaxRateOverTime * intensity;
        }

        if (distortionMaterial != null)
            distortionMaterial.SetFloat("_Distortion", intensity * maxDistortion);

        if (hazeOverlay != null)
            hazeOverlay.alpha = intensity * maxHazeAlpha;

        if (vignette != null)
            vignette.intensity.value = intensity * maxVignetteIntensity;

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = Mathf.Lerp(0f, minSaturation, intensity);
    }
}
