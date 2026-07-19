using UnityEngine;

/// <summary>
/// [테스트 전용 — 제출 빌드에서 제거]
/// 팀원1의 GhostAI가 아직 없어도 추격 BGM/괴성을 테스트할 수 있게
/// ThreatState 이벤트를 키보드로 강제 발행한다.
/// T = 추격 시작(2) / Y = 경계(1) / U = 평온(0)
/// </summary>
public class DebugThreatState : MonoBehaviour
{
    [SerializeField] private string huntId = "debug";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) EventBus.RaiseThreatStateChanged(huntId, 2);
        if (Input.GetKeyDown(KeyCode.Y)) EventBus.RaiseThreatStateChanged(huntId, 1);
        if (Input.GetKeyDown(KeyCode.U)) EventBus.RaiseThreatStateChanged(huntId, 0);
    }
}
