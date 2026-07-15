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

    [Header("Vignette / 색보정 (씬의 Global Volume, 없으면 스킵)")]
    [SerializeField] private Volume volume;
    [SerializeField] private float maxVignetteIntensity = 0.5f;
    [SerializeField] private float minSaturation = -60f;

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
        if (fogParticles != null)
        {
            var emission = fogParticles.emission;
            emission.rateOverTimeMultiplier = fogMaxRateOverTime * value;
        }

        if (distortionMaterial != null)
            distortionMaterial.SetFloat("_Distortion", value * maxDistortion);

        if (vignette != null)
            vignette.intensity.value = value * maxVignetteIntensity;

        if (colorAdjustments != null)
            colorAdjustments.saturation.value = Mathf.Lerp(0f, minSaturation, value);
    }
}
