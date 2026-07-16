using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PhaseVisionModifier : MonoBehaviour, IThreatBehavior
{
    [Header("시야 제어 대상")]
    [SerializeField] private Light2D playerFlashlight; // 플레이어의 손전등 

    [Header("시야 설정")]
    [SerializeField] private float visionMultiplier = 0.7f; // 30% 감소

    private float originalOuterAngle;
    private float originalOuterRadius;
    public bool IsNeutralized { get; private set; }

    public void Activate()
    {
        IsNeutralized = false;
        if (playerFlashlight != null)
        {
            originalOuterAngle = playerFlashlight.pointLightOuterAngle;
            originalOuterRadius = playerFlashlight.pointLightOuterRadius;

            // 시야 30% 감소 적용
            playerFlashlight.pointLightOuterAngle *= visionMultiplier;
            playerFlashlight.pointLightOuterRadius *= visionMultiplier;
            Debug.Log("<color=cyan>[PhaseVisionModifier]</color> 시야 30% 감소!");
        }
    }

    public void Neutralize()
    {
        if (IsNeutralized) return;
        if (playerFlashlight != null)
        {
            // 시야 원복
            playerFlashlight.pointLightOuterAngle = originalOuterAngle;
            playerFlashlight.pointLightOuterRadius = originalOuterRadius;
        }
        IsNeutralized = true;
    }

    public void Tick() { }
    public void SetProgress(float t) { }
}