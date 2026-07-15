using UnityEngine;

/// 맨 처음으로 돌리기
public class RestartSaveModel : MonoBehaviour, ISaveModel
{
    [SerializeField] string startSceneName = "Main";

    void OnEnable()     => EventBus.OnPlayerDeath   += HandleDeath;
    void OnDisable()    => EventBus.OnPlayerDeath   -= HandleDeath;

    /// <summary>
    /// 사망시 처리
    /// </summary>
    private void HandleDeath() => RestoreOnDeath();

    /// <summary>
    /// Progress 저장 X, 만약 나중에 수정될 경우 대비 Interface 우선 구현
    /// </summary>
    public void SaveProgress() { }

    /// <summary>
    /// 사망 이후 재시작
    /// </summary>
    public void RestoreOnDeath() => SceneFlow.Instance.FadeAndLoad(startSceneName);

    /// <summary>
    /// 현재 체크포인트 Scene 이름
    /// </summary>
    /// <returns></returns>
    public string GetCheckpoint() => startSceneName;
}