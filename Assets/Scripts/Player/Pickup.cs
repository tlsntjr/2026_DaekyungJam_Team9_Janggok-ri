using FMODUnity;
using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [Header("Item id")]
    [SerializeField] private string itemId;

    [Header("획득 사운드 (비우면 스킵 — FMOD 이벤트는 2D 권장)")]
    [SerializeField] private EventReference pickupSfx;

    [Header("�ٽ� �������� ���")]
    [SerializeField] private string objectiveFlagId;

    [Header("������ �������� �����ϱ� ���� �������� ��� ����")]
    [SerializeField] private HauntController haunt;   // �� �������� ���� ������ ��ǥ���� ���� ����

    public string Prompt => "줍기";
    public string InteractKey => "E";
    /// <summary>
    /// ������ ��ȣ�ۿ�
    /// </summary>
    public void Interact()
    {
        if (!pickupSfx.IsNull)
            SoundManager.Instance.PlayOneShot(pickupSfx, transform.position);

        InventorySystem.Instance.Add(itemId);

        if (!string.IsNullOrEmpty(objectiveFlagId))
            ObjectiveSystem.Instance.SetFlag(objectiveFlagId);

        if (haunt != null)
            haunt.CompleteHaunt();

        gameObject.SetActive(false);
    }
}