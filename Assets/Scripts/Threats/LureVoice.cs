using UnityEngine;

public class LureVoice : MonoBehaviour, IThreatBehavior
{
	[Header("대사 연출")]
	[SerializeField] private string[] promptLines;				// "누나, 나랑 숨바꼭질할래?" 등
	[SerializeField] private bool useRapidPrompt;			// 3페이즈: 연속 팝업 모드
	[SerializeField] private float rapidTimeout = 3.5f;		// 연속 팝업 제한시간

	[Header("연결")]
	[SerializeField] private HauntController controller;
	[SerializeField] private RuleWatcher rule;					// 성공 및 실패 판정 위임

	// 해당 위협 비활성화됐는지 체크
	public bool IsNeutralized { get; private set; }

	/// <summary>
	/// 위협 활성화
	/// </summary>
	public void Activate()
	{
		IsNeutralized = false;

		if (useRapidPrompt)
		{
			// 연속 팝업, 캔슬 성공=만족, 실패=위반
			foreach (var line in promptLines)
				DialogueSystem.Instance.ShowRapidPrompt(
					rapidTimeout,
					onSurvive: rule.MarkSatisfied,
					onFail: rule.MarkViolated);
		}
		else
		{
			// 일반 선택지: 0번 = 안전한 선택([대답하지 않는다]/[...])이라고 약속
			DialogueSystem.Instance.ShowChoice(
				new[] { "[대답하지 않는다]", "대답한다" },
				onSelect: i =>
				{
					if (i == 0) rule.MarkSatisfied();
					else controller.Fail(FailReason.Caught);   // 갯벌은 대답 -> 즉사
				});
		}
	}

	public void Tick() { }											// 미끼 목소리에선 사용 X
	public void Neutralize() => IsNeutralized = true;
	public void SetProgress(float t) { }							// 미끼 목소리에선 사용 X
}
