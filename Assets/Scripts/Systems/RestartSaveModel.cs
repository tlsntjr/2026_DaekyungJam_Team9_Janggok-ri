using UnityEngine;

/// <summary>
/// 저장 모델: 맨 처음부터 다시.
/// ※ 사망 시 자동 리로드는 DeathDirector가 주도하는 것으로 변경됨 —
///   여기서 OnPlayerDeath를 직접 구독하면 사망 모션/UI가 나오기 전에 씬이 리로드되므로 제거.
/// </summary>
public class RestartSaveModel : MonoBehaviour, ISaveModel
{
    [SerializeField] string startSceneName = "SCENE_INTRO";

    public void SaveProgress() { }

    /// <summary>
    /// 처음부터 재시작 (DeathDirector의 '메인으로' 버튼 등에서 호출)
    /// </summary>
    public void RestoreOnDeath()
    {
        ObjectiveSystem.ClearAll();   // 스토리 플래그는 씬을 넘어 유지되므로, 처음부터 다시 갈 땐 명시적으로 초기화
        SceneFlow.Instance.FadeAndLoad(startSceneName);
    }

    public string GetCheckpoint() => startSceneName;
}
