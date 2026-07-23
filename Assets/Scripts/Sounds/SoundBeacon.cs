using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 소리 길잡이 — 켜지는 순간부터 이 위치에서 루프 사운드를 재생해 플레이어를 소리로 유도한다.
/// "발전기 가동 → 안테나가 살아남 → 멀리서 무선 장치가 울림 → 소리를 따라가면 녹음기" 같은
/// 소리 추적 동선의 핵심 부품. PhaseObjectToggler로 페이즈 진입 시 활성화되는 루트 밑에 배치하면 끝.
///
/// 주의: FMOD 이벤트는 반드시 3D(Spatializer)여야 거리감·방향감이 생겨 "소리를 따라간다"가 성립함.
/// Min/Max Distance로 들리기 시작하는 거리를 조절. EventBus 소음(몬스터 유인)은 방출하지 않음 —
/// 인면어가 무선 장치로 도약하는 사고 방지.
/// </summary>
public class SoundBeacon : MonoBehaviour
{
    [Header("루프 사운드 (3D 이벤트 필수)")]
    [SerializeField] private EventReference loopSfx;

    [Header("켜진 뒤 재생 시작까지 지연 — 페이즈 전환 대사가 끝날 여유")]
    [SerializeField] private float startDelay = 0f;

    private EventInstance instance;
    private bool playing;

    private void OnEnable()
    {
        if (loopSfx.IsNull) return;

        if (startDelay > 0f) Invoke(nameof(StartLoop), startDelay);
        else StartLoop();
    }

    private void StartLoop()
    {
        if (playing || SoundManager.Instance == null) return;
        instance = SoundManager.Instance.PlayLoop(loopSfx, transform);
        playing = true;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(StartLoop));
        if (playing)
        {
            SoundManager.Instance.StopLoop(instance, immediate: false);   // 페이드아웃 — 뚝 끊기지 않게
            playing = false;
        }
    }

    /// <summary>연출용 수동 정지 (녹음기 획득 순간 무선 장치가 뚝 끊기는 연출 등)</summary>
    public void StopBeacon(bool immediate = true)
    {
        CancelInvoke(nameof(StartLoop));
        if (!playing) return;
        SoundManager.Instance.StopLoop(instance, immediate);
        playing = false;
    }
}
