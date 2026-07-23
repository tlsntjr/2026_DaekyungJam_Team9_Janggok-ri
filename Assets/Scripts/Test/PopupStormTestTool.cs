using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팝업 폭풍 테스트용 도구. 다른 테스트 툴들과 같은 방식.
///   P 키 : 팝업 폭풍 시작 — 계단식 더미 난사 + 글리치 출렁임 → 끝에 진짜 팝업 출력
///          (테스트용으로 진짜 팝업의 모든 버튼은 "누르면 전체 정리"로 자동 연결)
///   O 키 : 폭풍 즉시 종료·정리
/// 테스트 끝나면 씬에서 제거할 것.
/// </summary>
public class PopupStormTestTool : MonoBehaviour
{
    [SerializeField] private float stormDuration = 5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (PopupStormDirector.Instance == null)
            {
                Debug.LogError("[PopupStormTest] 씬에 PopupStormDirector가 없습니다 (캔버스 아래 배치 필요).");
                return;
            }

            Debug.Log($"<color=cyan>[PopupStormTest]</color> 팝업 폭풍 시작 ({stormDuration}초) — 글리치 출렁임과 계단식 배치 확인");
            PopupStormDirector.Instance.StartStorm(stormDuration, OnFinalPopup);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            if (PopupStormDirector.Instance == null) return;

            Debug.Log("<color=cyan>[PopupStormTest]</color> 팝업 폭풍 강제 종료");
            PopupStormDirector.Instance.StopStorm();
        }
    }

    /// <summary>
    /// 테스트용 콜백 — 진짜 팝업의 모든 버튼(OK, X 등)을 "누르면 전체 정리"로 연결.
    /// 실제 게임에선 이 자리에 이벤트별 고유 동작을 연결하면 됨.
    /// </summary>
    private void OnFinalPopup(GameObject popup)
    {
        Debug.Log("<color=cyan>[PopupStormTest]</color> 진짜 팝업 등장 — 버튼을 누르면 닫힙니다");

        foreach (var button in popup.GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(() => PopupStormDirector.Instance.StopStorm());
    }
}
