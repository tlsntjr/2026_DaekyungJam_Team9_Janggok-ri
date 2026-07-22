using UnityEngine;

/* 
	* �ڱ� �ݰ� �ȿ��� ������ �� �� ������ ���� ���� ����
	* �Ҹ� �����ؼ� �����̴� �� ���� AI���� ����
*/
public class DistractionCounter : MonoBehaviour, ICounterCondition
{
	[Header("���� ����")]
	[SerializeField] private float hearRadius = 2.5f;

	[Header("�� ������� �����ؾ� �ϴ� Ƚ��")]
	[SerializeField] private int requiredCount = 1;		// ������ �� �ʿ��� ī��Ʈ

	[Header("유인 성공 대사 (비우면 스킵)")]
	[SerializeField, TextArea(2, 3)] private string lureLine = "좋아, 제대로 따돌렸어.";
	[SerializeField, TextArea(2, 3)] private string finalLine = "";   // 필요 횟수를 전부 채운 순간 (비우면 lureLine 그대로)

	int count;
	public bool IsSatisfied => count >= requiredCount;

	void OnEnable() => EventBus.OnNoiseEmitted += HandleNoise;
	void OnDisable() => EventBus.OnNoiseEmitted -= HandleNoise;

	/// <summary>
	/// ����� ����
	/// </summary>
	/// <param name="pos">������ �߻� ��ġ</param>
	/// <param name="radius">/param>
	void HandleNoise(Vector2 pos, float radius)
	{
		if (Vector2.Distance(transform.position, pos) > hearRadius) return;
		count++;

		// 유인 성공 피드백 — 파훼가 통했다는 확인 대사 (스토리 시퀀스 진행 중이면 덮지 않고 스킵)
		string line = (count >= requiredCount && !string.IsNullOrEmpty(finalLine)) ? finalLine : lureLine;
		if (!string.IsNullOrEmpty(line) && DialogueSystem.Instance != null && !DialogueSystem.Instance.IsSequenceActive)
			DialogueSystem.Instance.Show(line);

        Debug.Log($"<color=orange>[DistractionCounter]</color> {gameObject.name}�� ������ �����߽��ϴ�! ���� ī��Ʈ: {count}/{requiredCount}");

        if (count >= requiredCount)
        {
            Debug.Log($"<color=green>[DistractionCounter]</color> {gameObject.name} ���� ���� ���� �Ϸ� (IsSatisfied = true)!");
        }
    }

	/// <summary>
	/// ī��Ʈ ����
	/// </summary>
	public void ResetCount() => count = 0;
}