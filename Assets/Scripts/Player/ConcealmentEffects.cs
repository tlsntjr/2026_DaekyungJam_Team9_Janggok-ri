using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 은신 시각 효과 — 플레이어가 은신처에 숨어 있는 동안(Concealment.IsPlayerConcealed):
///   · 캐릭터 스프라이트가 완전히 사라짐 (기본 알파 0 — 오브젝트 뒤에 몸을 숨긴 표현)
///   · 손전등·주변광의 "반경"이 좁아지고 밝기도 낮아짐 (틈새로 밖을 보는 답답한 시야)
///
/// ★ 반드시 플레이어 오브젝트에 부착해야 동작함. 존 쪽 세팅은 불필요 (static 플래그 참조).
///
/// 기준값은 "숨지 않은 동안 매 프레임 재캡처" — PhaseVisionModifier(2페이즈 시야 축소) 등
/// 다른 시스템이 라이트 값을 바꿔도 그 값을 새 기준으로 흡수하므로 서로 충돌하지 않는다.
/// </summary>
public class ConcealmentEffects : MonoBehaviour
{
    [Header("은신 시 스프라이트 알파 (0 = 완전히 사라짐)")]
    [SerializeField, Range(0f, 1f)] private float hiddenSpriteAlpha = 0f;

    [Header("은신 시 라이트 반경 배율 (시야가 이만큼 좁아짐)")]
    [SerializeField, Range(0f, 1f)] private float hiddenRadiusMultiplier = 0.45f;

    [Header("은신 시 라이트 밝기 배율")]
    [SerializeField, Range(0f, 1f)] private float hiddenIntensityMultiplier = 0.55f;

    [Header("전환 부드러움")]
    [SerializeField] private float blendSpeed = 7f;

    [Header("대상 (비우면 자기 하위에서 자동 수집)")]
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private Light2D[] lights;

    private readonly List<float> baseAlphas = new();
    private readonly List<float> baseIntensities = new();
    private readonly List<float> baseRadii = new();
    private float blend;   // 0 = 평상시, 1 = 완전 은신

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light2D>(true);

        CaptureBases();
    }

    private void CaptureBases()
    {
        baseAlphas.Clear();
        baseIntensities.Clear();
        baseRadii.Clear();

        foreach (var sr in renderers) baseAlphas.Add(sr != null ? sr.color.a : 1f);
        foreach (var l in lights)
        {
            baseIntensities.Add(l != null ? l.intensity : 1f);
            baseRadii.Add(l != null ? l.pointLightOuterRadius : 5f);
        }
    }

    private void Update()
    {
        bool hidden = Concealment.IsPlayerConcealed;

        // 숨지 않은 안정 상태에선 매 프레임 기준값 재캡처 —
        // 페이즈 시야 축소 등 외부 변경을 새 기준으로 흡수 (충돌 방지의 핵심)
        if (!hidden && blend <= 0f)
        {
            CaptureBases();
            return;
        }

        blend = Mathf.MoveTowards(blend, hidden ? 1f : 0f, blendSpeed * Time.deltaTime);
        Apply();
    }

    private void Apply()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = Mathf.Lerp(baseAlphas[i], baseAlphas[i] * hiddenSpriteAlpha, blend);
            renderers[i].color = c;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].intensity = Mathf.Lerp(baseIntensities[i], baseIntensities[i] * hiddenIntensityMultiplier, blend);
            lights[i].pointLightOuterRadius = Mathf.Lerp(baseRadii[i], baseRadii[i] * hiddenRadiusMultiplier, blend);
        }
    }

    private void OnDisable()
    {
        // 사망 연출 등으로 꺼질 때 투명/좁은 시야가 남지 않게 원복
        blend = 0f;
        Apply();
    }
}
