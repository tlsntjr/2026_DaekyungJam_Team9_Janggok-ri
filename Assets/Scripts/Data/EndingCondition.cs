using UnityEngine;

[CreateAssetMenu(menuName = "Game Objects/Ending Condition")]
public class EndingCondition : ScriptableObject
{
	public EndingType endingType;
	public int priority;							// 우선도, 작을 수록 먼저 검사함
	public string[] requiredFlags;			// 완료를 위해 필요한 플래그들

	/// <summary>
	/// 엔딩에 필요한 플래그들을 전부 만족했는지 검사
	/// </summary>
	/// <param name="state">괴담 진행에 따라 쌓인 IObjective 데이터</param>
	/// <returns></returns>
	public bool IsSatisfied(IObjective state)
	{
		foreach (var f in requiredFlags)
			if (!state.HasFlag(f)) return false;

		return true;
	}
}