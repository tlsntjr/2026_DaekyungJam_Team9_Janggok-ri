using UnityEngine;

/// <summary>
/// 테스트 전용: 인벤토리에 아이템을 강제로 넣어주는 디버그 헬퍼.
/// E키 상호작용 드라이버(IInteractable을 실제로 호출하는 쪽)가 아직 없어서
/// 정상적인 줍기 흐름을 테스트할 수 없을 때 임시로 사용. 실제 빌드에는 포함하지 말 것.
/// </summary>
public class DebugGiveItem : MonoBehaviour
{
    [SerializeField] private string itemId = "shell";

    [ContextMenu("Add Item To Inventory")]
    private void AddItem()
    {
        InventorySystem.Instance.Add(itemId);
        Debug.Log($"[DebugGiveItem] '{itemId}' 아이템을 인벤토리에 추가했습니다.");
    }
}
