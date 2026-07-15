using UnityEngine;

[CreateAssetMenu(menuName = "Project Objects/Haunt Definition")]
public class HauntDefinition : ScriptableObject
{
	public string huntId;			// "mudflat", "lighthouse", "fishfarm"
	public string regionLabel;	// FMOD "Region" 파라미터로 넘길 라벨
}