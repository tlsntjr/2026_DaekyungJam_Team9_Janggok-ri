using UnityEngine;

[CreateAssetMenu(menuName = "Game Objects/Item Definition")]
public class ItemDefinition : ScriptableObject
{
	public string itemId;
	public string displayName;
	[TextArea] public string description;
	public Sprite icon;

	public bool isKeyItem;				// true=열쇠템(영구), false=소모품(스택)
}