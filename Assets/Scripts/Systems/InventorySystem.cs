using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventorySystem : MonoBehaviour, IInventory
{
    public static InventorySystem Instance { get; private set; }

    [SerializeField] ItemDefinition[] itemCatalog;  // ��ü ������ ����(������ ����)

    readonly HashSet<string> keyItems = new();              // ������: ����, �ߺ� ����
    readonly Dictionary<string, int> consumables = new();   // �Ҹ�ǰ: ���� ����

    public event Action OnChanged;

	void Awake()
	{
		// 씬 전환 후 살아남은 옛 인스턴스는 컴포넌트만 제거하고 현재 씬 것이 승계 (DDOL 좀비 방지)
		if (Instance != null && Instance != this) Destroy(Instance);
		Instance = this;
	}

	ItemDefinition Find(string id) => itemCatalog.FirstOrDefault(d => d.itemId == id);

	public bool Has(string itemId) =>
		keyItems.Contains(itemId) || (consumables.TryGetValue(itemId, out int n) && n > 0);

	/// <summary>보유 수량 — 소모품은 개수, 열쇠 아이템은 1/0 (HUD 표시용)</summary>
	public int GetCount(string itemId)
	{
		if (keyItems.Contains(itemId)) return 1;
		return consumables.TryGetValue(itemId, out int n) ? n : 0;
	}

	public void Add(string itemId)
	{
		var def = Find(itemId);
		if (def == null) { Debug.LogWarning($"[Inventory] �̵�� ������: {itemId}"); return; }

		if (def.isKeyItem)
		{
			if (!keyItems.Add(itemId)) return;  // ������ �ߺ� ȹ���� ����
		}
		else
		{
			consumables.TryGetValue(itemId, out int n);
			consumables[itemId] = n + 1;
		}
		OnChanged?.Invoke();
	}

	public void Remove(string itemId)
	{
		if (keyItems.Remove(itemId)) { OnChanged?.Invoke(); return; }

        if (consumables.TryGetValue(itemId, out int n) && n > 0)
        {
            consumables[itemId] = n - 1;
            OnChanged?.Invoke();
        }
    }
}