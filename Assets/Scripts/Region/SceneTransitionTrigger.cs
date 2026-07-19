using UnityEngine;

/// <summary>
/// 괴담 클리어와 무관하게, 밟으면 지정 씬으로 넘어가는 단순 전환 트리거.
/// 괴담이 없는 씬의 출구용 (인트로 → 갯벌, 튜토리얼 → 마을 등).
/// 괴담 구역의 출구는 이거 말고 ExitTrigger(클리어 검사 있음)를 쓸 것.
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    private bool used;   // 페이드 중 재진입으로 중복 로드되는 것 방지

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        used = true;
        SceneFlow.Instance.FadeAndLoad(targetSceneName);
    }
}
