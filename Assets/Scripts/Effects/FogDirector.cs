using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 안개 셰이더(_Density)의 단일 소유자. 씬마다 1개 배치 (PanicGlitchDirector와 같은 씬 싱글톤 패턴).
/// 입력 채널은 두 개이고, 화면에는 항상 둘 중 큰 값이 반영됨 (합산 아님):
///   1) 전역(오염도) : FogDirector.Instance.SetAmbientLevel(값)
///      — ContaminationEffectsDirector가 오염도 intensity를 이 한 줄로 넘기면 됨
///   2) 구역        : FogZone 트리거가 진입/이탈 시 자동 호출 (직접 호출할 일 없음)
/// ※ 다른 코드에서 안개 머티리얼에 SetFloat를 직접 치지 말 것 — 서로 덮어써서 충돌남.
/// fogMaterial은 Renderer 2D의 Full Screen Pass에 꽂힌 머티리얼 에셋과 같은 것이어야 함.
/// </summary>
public class FogDirector : MonoBehaviour
{
    public static FogDirector Instance { get; private set; }

    [Header("안개 머티리얼 (Full Screen Pass의 Pass Material과 동일 에셋)")]
    [SerializeField] private Material fogMaterial;

    [Header("전환 시간 (구역 진입/이탈 시 부드럽게)")]
    [SerializeField] private float fadeInDuration = 1.2f;    // 짙어질 때
    [SerializeField] private float fadeOutDuration = 1.8f;   // 걷힐 때 — 살짝 더 느리게 남는 게 자연스러움

    [Header("플레이어 주변 시야 구멍 (player 비우면 구멍 없이 화면 전체 안개)")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera cam;                      // 비우면 Camera.main
    [SerializeField] private float clearWorldRadius = 2.2f;   // 월드 유닛 기준 반경 (손전등 빛 반경과 맞추면 자연스러움)

    private static readonly int DensityId     = Shader.PropertyToID("_Density");
    private static readonly int ClearCenterId = Shader.PropertyToID("_ClearCenter");
    private static readonly int ClearRadiusId = Shader.PropertyToID("_ClearRadius");

    private readonly HashSet<FogZone> activeZones = new HashSet<FogZone>();
    private float ambientLevel;          // 오염도 등 전역 입력
    private float currentDensity;        // 실제 화면 반영값 (목표를 향해 러프)
    private float appliedDensity = -1f;  // 마지막으로 머티리얼에 쓴 값 (불필요한 SetFloat 방지)

    private void Awake()
    {
        // 씬 전환 후 살아남은 옛 인스턴스는 컴포넌트만 제거하고 현재 씬 것이 승계 (DDOL 좀비 방지)
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;

        if (cam == null) cam = Camera.main;
        Apply(0f);   // 에디터에서 머티리얼 에셋에 남은 이전 실행 값 청소
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable() => Apply(0f);   // SetFloat 값은 에디터에서 에셋에 영구 저장되므로 원복

    private void Update()
    {
        float target = ambientLevel;
        foreach (var zone in activeZones)
            if (zone != null && zone.Density > target) target = zone.Density;

        float duration = target > currentDensity ? fadeInDuration : fadeOutDuration;
        currentDensity = Mathf.MoveTowards(currentDensity, target, Time.deltaTime / Mathf.Max(0.01f, duration));

        Apply(currentDensity);
        UpdateClearHole();
    }

    /// <summary>
    /// 플레이어 주변 시야 구멍 갱신 — 카메라가 움직여도 구멍이 캐릭터를 정확히 따라가도록 매 프레임 호출.
    /// </summary>
    private void UpdateClearHole()
    {
        if (fogMaterial == null || currentDensity <= 0f) return;

        if (player == null || cam == null)
        {
            fogMaterial.SetFloat(ClearRadiusId, 0f);   // 구멍 없음
            return;
        }

        Vector3 viewport = cam.WorldToViewportPoint(player.position);
        fogMaterial.SetVector(ClearCenterId, new Vector4(viewport.x, viewport.y, 0f, 0f));

        // 월드 반경 → 화면 세로 비율 (직교 카메라 기준: 세로 절반이 orthographicSize)
        fogMaterial.SetFloat(ClearRadiusId, clearWorldRadius / (cam.orthographicSize * 2f));
    }

    /// <summary>
    /// 전역 안개 레벨 (0~1). 오염도 연출 쪽에서 호출 — 구역 안개와 겹치면 큰 쪽이 반영됨.
    /// </summary>
    public void SetAmbientLevel(float level) => ambientLevel = Mathf.Clamp01(level);

    // ===== FogZone 전용 (트리거가 자동 호출) =====
    public void EnterZone(FogZone zone) => activeZones.Add(zone);
    public void ExitZone(FogZone zone)  => activeZones.Remove(zone);

    private void Apply(float value)
    {
        if (fogMaterial == null) return;
        if (Mathf.Approximately(value, appliedDensity)) return;

        appliedDensity = value;
        fogMaterial.SetFloat(DensityId, value);
    }
}
