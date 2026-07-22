using UnityEngine;

/// <summary>
/// 패닉 글리치 셰이더(_Intensity) 구동기. 씬마다 1개 배치 (CameraShake와 같은 씬 싱글톤 패턴).
/// 다른 연출에서 아래 두 줄 중 하나로 호출:
///   PanicGlitchDirector.Instance.Pulse(0.8f);          // 확 튀었다 잦아드는 단발 — 점프스케어, 조우, 기억의 균열
///   PanicGlitchDirector.Instance.SetBaseLevel(0.25f);  // 구간 지속형 — 환청·손전등 글리치 구간 (0으로 해제)
/// 단발과 지속형이 겹치면 큰 값이 화면에 반영됨(합산 아님 — 과다 노출 방지).
/// glitchMaterial은 Renderer 2D의 Full Screen Pass에 꽂힌 머티리얼 에셋과 같은 것이어야 함.
/// </summary>
public class PanicGlitchDirector : MonoBehaviour
{
    public static PanicGlitchDirector Instance { get; private set; }

    [Header("글리치 머티리얼 (Full Screen Pass의 Pass Material과 동일 에셋)")]
    [SerializeField] private Material glitchMaterial;

    [Header("단발(Pulse) 기본 감쇠 시간")]
    [SerializeField] private float defaultDecayDuration = 0.6f;

    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private float baseLevel;       // 구간 지속형 레벨 (SetBaseLevel로 제어)
    private float pulseValue;      // 단발 현재값 (매 프레임 감쇠)
    private float pulseDecaySpeed; // 단발 감쇠 속도 (강도/시간)
    private float appliedValue = -1f;   // 마지막으로 머티리얼에 쓴 값 (불필요한 SetFloat 방지)

    private void Awake()
    {
        // 씬 전환 후 살아남은 옛 인스턴스는 컴포넌트만 제거하고 현재 씬 것이 승계 (DDOL 좀비 방지)
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;

        Apply(0f);   // 에디터에서 머티리얼 에셋에 남은 이전 실행 값 청소
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable() => Apply(0f);   // SetFloat 값은 에디터에서 에셋에 영구 저장되므로 원복

    private void Update()
    {
        if (pulseValue > 0f)
            pulseValue = Mathf.MoveTowards(pulseValue, 0f, pulseDecaySpeed * Time.deltaTime);

        Apply(Mathf.Max(baseLevel, pulseValue));
    }

    /// <summary>
    /// 확 튀었다 잦아드는 단발 글리치. 이미 진행 중이면 더 강한 쪽으로 교체.
    /// </summary>
    /// <param name="strength">피크 강도 (0~1)</param>
    public void Pulse(float strength) => Pulse(strength, defaultDecayDuration);

    /// <param name="strength">피크 강도 (0~1)</param>
    /// <param name="decayDuration">피크에서 0까지 잦아드는 시간</param>
    public void Pulse(float strength, float decayDuration)
    {
        strength = Mathf.Clamp01(strength);
        if (strength <= pulseValue) return;   // 진행 중인 더 강한 펄스를 약한 호출이 끊지 않게

        pulseValue = strength;
        pulseDecaySpeed = strength / Mathf.Max(0.01f, decayDuration);
    }

    /// <summary>
    /// 구간 지속형 글리치 레벨 설정. 구간 진입 시 원하는 레벨, 이탈 시 0.
    /// </summary>
    public void SetBaseLevel(float level) => baseLevel = Mathf.Clamp01(level);

    private void Apply(float value)
    {
        if (glitchMaterial == null) return;
        if (Mathf.Approximately(value, appliedValue)) return;

        appliedValue = value;
        glitchMaterial.SetFloat(IntensityId, value);
    }
}
