using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 배경 환경음 루프 재생기. 두 가지 용도로 사용:
///
/// 1) 씬 전체 앰비언스 — 씬에 하나 배치 (갯벌 바람/파도, 양식장 물소리 등).
///    FMOD 이벤트를 2D(스페이셜라이저 없음)로 만들 것.
/// 2) 지점 음원 — 소리 나는 오브젝트에 부착 (발전기 웅웅거림, 물 새는 소리 등).
///    FMOD 이벤트를 3D로 만들면 거리·방향감이 자동 적용됨.
///
/// 씬 전환/오브젝트 파괴 시 자동으로 페이드아웃 정지
/// (페이드 길이는 FMOD 이벤트 Master 트랙의 AHDSR Release로 조절).
/// </summary>
public class SceneAmbience : MonoBehaviour
{
    [Header("환경음 (루프 이벤트)")]
    [SerializeField] private EventReference ambience;

    private EventInstance instance;
    private bool playing;

    private void Start()
    {
        if (ambience.IsNull)
        {
            Debug.LogWarning($"[SceneAmbience] {gameObject.name}: 이벤트가 비어 있어 재생을 건너뜁니다");
            return;
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogError($"[SceneAmbience] {gameObject.name}: SoundManager가 없어 재생 불가");
            return;
        }

        instance = SoundManager.Instance.PlayLoop(ambience, transform);
        playing = true;

        // EventReference.Path는 에디터 전용 프로퍼티 — 빌드에선 컴파일 에러가 나므로 반드시 가드
#if UNITY_EDITOR
        Debug.Log($"<color=green>[SceneAmbience]</color> {gameObject.name}: 루프 재생 시작 ({ambience.Path})");
#else
        Debug.Log($"<color=green>[SceneAmbience]</color> {gameObject.name}: 루프 재생 시작");
#endif
    }

    private void OnDestroy()
    {
        if (!playing) return;
        if (SoundManager.Instance == null) return;   // 앱 종료 시 파괴 순서 대비

        SoundManager.Instance.StopLoop(instance, immediate: false);
    }
}
