using UnityEngine;

/* 
	* 자기 반경 안에서 소음이 몇 번 났는지 세는 파훼 조건
	* 소리 반응해서 움직이는 건 몬스터 AI에서 구현
*/
public class DistractionCounter : MonoBehaviour, ICounterCondition
{
	[Header("반응 범위")]
	[SerializeField] private float hearRadius = 2.5f;

	[Header("이 페이즈에서 수행해야 하는 횟수")]
	[SerializeField] private int requiredCount = 1;		// 페이즈 별 필요한 카운트

	int count;
	public bool IsSatisfied => count >= requiredCount;

	void OnEnable() => EventBus.OnNoiseEmitted += HandleNoise;
	void OnDisable() => EventBus.OnNoiseEmitted -= HandleNoise;

	/// <summary>
	/// 노이즈에 반응
	/// </summary>
	/// <param name="pos">노이즈 발생 위치</param>
	/// <param name="radius">/param>
	void HandleNoise(Vector2 pos, float radius)
	{
		if (Vector2.Distance(transform.position, pos) > hearRadius) return;
		count++;

        Debug.Log($"<color=orange>[DistractionCounter]</color> {gameObject.name}가 소음을 감지했습니다! 현재 카운트: {count}/{requiredCount}");

        if (count >= requiredCount)
        {
            Debug.Log($"<color=green>[DistractionCounter]</color> {gameObject.name} 파훼 조건 충족 완료 (IsSatisfied = true)!");
        }
    }

	/// <summary>
	/// 카운트 리셋
	/// </summary>
	public void ResetCount() => count = 0;
}