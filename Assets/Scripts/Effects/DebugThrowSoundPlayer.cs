using UnityEngine;

/// <summary>
/// 테스트 전용: 조개껍데기가 착지할 때 방송되는 EventBus.OnNoiseEmitted에 맞춰
/// 지정된 AudioClip을 재생한다. 정식 사운드는 FMOD 이벤트로 만들어 SoundManager
/// 경유로 재생해야 하지만(팀 컨벤션), FMOD 뱅크 제작 전 빠른 청취 확인용으로 사용.
/// </summary>
public class DebugThrowSoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip testClip;
    [SerializeField] private float volume = 1f;

    private void OnEnable()  => EventBus.OnNoiseEmitted += HandleNoise;
    private void OnDisable() => EventBus.OnNoiseEmitted -= HandleNoise;

    private void HandleNoise(Vector2 pos, float radius)
    {
        if (testClip != null)
            AudioSource.PlayClipAtPoint(testClip, pos, volume);
    }
}
