using UnityEngine;

/// <summary>
/// 괴성/글리치 연출 테스트용 도구. NoiseTestTool과 같은 방식.
///   H 키        : 마우스 커서 위치에서 소형 괴성 (돌진 텔레그래프)
///   Shift + H   : 대형 괴성 (광폭 진입 연출)
///          → 플레이어 가까이 찍으면 강하게, 멀리 찍으면 시각 연출은 스킵되고 소리만 감쇠되어 들림
///   J 키        : 패닉 글리치 단발 Pulse
///   K 키        : 패닉 글리치 지속형 토글 (환청 구간 흉내)
/// 테스트 끝나면 씬에서 제거할 것.
/// </summary>
public class ScreamTestTool : MonoBehaviour
{
    [Header("글리치 테스트 설정")]
    [SerializeField, Range(0f, 1f)] private float pulseStrength = 0.8f;
    [SerializeField, Range(0f, 1f)] private float baseLevel = 0.25f;

    private bool baseLevelOn;

    private void Update()
    {
        // H 키: 마우스 커서 위치에서 괴성 발생
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (Camera.main == null)
            {
                Debug.LogError("[ScreamTest] 메인 카메라를 찾을 수 없습니다.");
                return;
            }

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            bool isMajor = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Debug.Log($"<color=red>[ScreamTest]</color> {(isMajor ? "대형" : "소형")} 괴성 발생! 위치: {mousePos}");
            EventBus.RaiseMonsterScreamed(mousePos, isMajor);
        }

        // J 키: 글리치 단발
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (PanicGlitchDirector.Instance == null)
            {
                Debug.LogError("[ScreamTest] 씬에 PanicGlitchDirector가 없습니다.");
                return;
            }

            Debug.Log($"<color=cyan>[ScreamTest]</color> 글리치 Pulse({pulseStrength})");
            PanicGlitchDirector.Instance.Pulse(pulseStrength);
        }

        // K 키: 글리치 지속형 토글
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (PanicGlitchDirector.Instance == null)
            {
                Debug.LogError("[ScreamTest] 씬에 PanicGlitchDirector가 없습니다.");
                return;
            }

            baseLevelOn = !baseLevelOn;
            Debug.Log($"<color=cyan>[ScreamTest]</color> 글리치 지속형 {(baseLevelOn ? $"ON ({baseLevel})" : "OFF")}");
            PanicGlitchDirector.Instance.SetBaseLevel(baseLevelOn ? baseLevel : 0f);
        }
    }
}
