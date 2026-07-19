using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [Header("Item id")]
    [SerializeField] private string itemId;

    [Header("핵심 아이템일 경우")]
    [SerializeField] private string objectiveFlagId;

    [Header("마지막 구역으로 진입하기 위한 아이템일 경우 연결")]
    [SerializeField] private HauntController haunt;   // 이 아이템이 구역 마무리 목표물일 때만 연결

    public string Prompt => "줍기";
    public string InteractKey => "E";
    /// <summary>
    /// 아이템 상호작용
    /// </summary>
    public void Interact()
    {
        InventorySystem.Instance.Add(itemId);

        if (!string.IsNullOrEmpty(objectiveFlagId))
            ObjectiveSystem.Instance.SetFlag(objectiveFlagId);

        if (haunt != null)
            haunt.CompleteHaunt();

        gameObject.SetActive(false);
    }
}