using UnityEngine;

/// <summary>
/// 테스트 전용: SCENE_ENDING으로 강제 이동시킨다.
/// 실제로는 마지막 구역 클리어 시 자동으로 이동해야 하지만, 그 흐름이 아직 없어서
/// 엔딩 씬/EndingDirector 테스트용으로 수동 트리거를 제공한다.
/// </summary>
public class DebugGoToEnding : MonoBehaviour
{
    [SerializeField] private string endingSceneName = "SCENE_ENDING";

    [ContextMenu("Go To Ending")]
    private void GoToEnding()
    {
        SceneFlow.Instance.FadeAndLoad(endingSceneName);
    }
}
