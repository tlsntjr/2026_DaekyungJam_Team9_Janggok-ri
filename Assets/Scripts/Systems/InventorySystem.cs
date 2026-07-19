using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventorySystem : MonoBehaviour, IInventory
{
    public static InventorySystem Instance { get; private set; }

    [SerializeField] ItemDefinition[] itemCatalog;  // 전체 아이템 정의(레시피 포함)

    readonly HashSet<string> keyItems = new();              // 열쇠템: 영구, 중복 없음
    readonly Dictionary<string, int> consumables = new();   // 소모품: 개수 스택

    public event Action OnChanged;

	void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
	}

	ItemDefinition Find(string id) => itemCatalog.FirstOrDefault(d => d.itemId == id);

	public bool Has(string itemId) =>
		keyItems.Contains(itemId) || (consumables.TryGetValue(itemId, out int n) && n > 0);

	public void Add(string itemId)
	{
		var def = Find(itemId);
		if (def == null) { Debug.LogWarning($"[Inventory] 미등록 아이템: {itemId}"); return; }

		if (def.isKeyItem)
		{
			if (!keyItems.Add(itemId)) return;  // 열쇠템 중복 획득은 무시
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