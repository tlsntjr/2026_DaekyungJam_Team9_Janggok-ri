using FMODUnity;
using UnityEngine;

/// <summary>
/// 대사 시퀀스의 특정 줄에서 발동할 연출 묶음 — StoryTrigger·Pickup 등의 인스펙터에서 지정.
/// 예: 수첩 대사 2번째 줄(『...양식장으로 끌고 갔다.』)에서 셰이크 + 글리치 + 스팅.
/// </summary>
[System.Serializable]
public class DialogueLineEffect
{
    [Header("발동할 줄 번호 (0 = 첫 줄)")]
    public int lineIndex;

    [Header("카메라 셰이크 (Duration 0이면 스킵)")]
    public float shakeDuration = 0f;
    public float shakeMagnitude = 0.2f;

    [Header("글리치 펄스 (0이면 스킵)")]
    [Range(0f, 1f)] public float glitchPulse = 0f;

    [Header("효과음 (비우면 스킵 — 2D 권장)")]
    public EventReference sfx;

    /// <summary>이 이펙트를 실제로 발동 — 각 항목은 씬에 해당 시스템이 있을 때만 동작</summary>
    public void Apply(Vector3 worldPos)
    {
        if (shakeDuration > 0f && CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);

        if (glitchPulse > 0f && PanicGlitchDirector.Instance != null)
            PanicGlitchDirector.Instance.Pulse(glitchPulse);

        if (!sfx.IsNull)
            SoundManager.Instance.PlayOneShot(sfx, worldPos);
    }

    /// <summary>배열에서 해당 줄의 이펙트들을 전부 발동</summary>
    public static void ApplyAll(DialogueLineEffect[] effects, int lineIndex, Vector3 worldPos)
    {
        if (effects == null) return;
        foreach (var effect in effects)
            if (effect != null && effect.lineIndex == lineIndex)
                effect.Apply(worldPos);
    }
}
