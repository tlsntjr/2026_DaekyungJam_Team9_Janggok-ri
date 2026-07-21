using UnityEngine;

/// <summary>
/// 괴담 클리어와 무관하게, 밟으면 지정 씬으로 넘어가는 단순 전환 트리거.
/// 괴담이 없는 씬의 출구용 (인트로 → 갯벌, 튜토리얼 → 마을 등).
/// 괴담 구역의 출구는 이거 말고 ExitTrigger(클리어 검사 있음)를 쓸 것.
///
/// 필수 아이템을 지정하면 전부 보유해야 통과되고, 부족하면 안내 대사만 출력됨
/// (집: 손전등·메모를 얻기 전엔 "아직 무언가 찾지 못한 것 같다..." 하고 못 나가게).
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    [Header("전환 조건 — 전부 보유해야 통과 (비우면 무조건 통과)")]
    [SerializeField] private string[] requiredItemIds;
    [SerializeField, TextArea(2, 3)] private string blockedLine = "아직 무언가 찾지 못한 것 같다...";
    [SerializeField] private float blockedLineCooldown = 2f;   // 문앞에서 들락거릴 때 대사 스팸 방지

    private bool used;   // 페이드 중 재진입으로 중복 로드되는 것 방지
    private float lastBlockedTime = -999f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        if (!HasAllRequirements())
        {
            if (Time.time - lastBlockedTime >= blockedLineCooldown)
            {
                lastBlockedTime = Time.time;
                if (!string.IsNullOrEmpty(blockedLine) && DialogueSystem.Instance != null)
                    DialogueSystem.Instance.Show(blockedLine);
            }
            return;
        }

        used = true;
        SceneFlow.Instance.FadeAndLoad(targetSceneName);
    }

    private bool HasAllRequirements()
    {
        if (requiredItemIds == null || requiredItemIds.Length == 0) return true;

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[SceneTransitionTrigger] InventorySystem이 씬에 없어 조건 검사를 건너뜁니다 (통과 처리)");
            return true;
        }

        foreach (var itemId in requiredItemIds)
            if (!string.IsNullOrEmpty(itemId) && !InventorySystem.Instance.Has(itemId))
                return false;

        return true;
    }
}
