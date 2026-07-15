using System.Linq;
using UnityEngine;

public enum HauntState		{ Dormant, Triggered, Threatened, Countered, ObjectiveReady, Cleared }
public enum FailReason		{ RuleViolation, DeadlineExpired, Caught }

[System.Serializable]
public class HauntPhase
{
	[Header("현재 페이즈 내 위험체")]
	public MonoBehaviour[] threatBehaviours;                        // Interface -> 직렬화 불가, MonoBehaviour로 우선 받고 Convert 예정

	[Header("페이즈 카운터 조건 (페이즈 클리어)")]
	public MonoBehaviour[] counterConditions;					// 위와 마찬가지, MonoBehaviour 이후 Convert

	[Header("옵션")]
	public bool useDeadline;				// 타이머 사용 여부
	public float deadlineDuration;		// 타이머 시간
	public float contaminationPerSecondDuringDeadline;		// 시간 당 지속 피해량

	// ===== 런타임 캐싱 =====
	IThreatBehavior[] threats;
	ICounterCondition[] counters;

	// ===== MonoBehaviour 직렬화 =====
	public IThreatBehavior[] Threats			=> threats ??= threatBehaviours.OfType<IThreatBehavior>().ToArray();
	public ICounterCondition[] Counters		=> counters ??= counterConditions.OfType<ICounterCondition>().ToArray();
}

public class HauntController : MonoBehaviour
{
	[Header("현재 구역 괴담 정의")]
	[SerializeField] private HauntDefinition definition;

	[Header("페이즈 수")]
	[SerializeField] private HauntPhase[] phases;

	[Header("타이머")]
	[SerializeField] private DeadlineTimer deadlineTimer;

	[SerializeField] private float failContaminationAmount = 0.25f;

	public HauntState State { get; private set; } = HauntState.Dormant;
	public int CurrentPhaseIndex { get; private set; }

	private HauntPhase Current => phases[CurrentPhaseIndex];

	/// <summary>
	/// 게임 시작 트리거
	/// 트리거 -> 맵 입구에서 Trigger Collider로 호출하면 됨
	/// </summary>
	public void Trigger()
	{
		if (State != HauntState.Dormant) return;

		State						= HauntState.Triggered;
		CurrentPhaseIndex	= 0;
		ActivateThreat();
	}

	/// <summary>
	/// 구역 내 위협 Activate
	/// </summary>
	private void ActivateThreat()
	{
		State = HauntState.Threatened;

		foreach (var t in Current.Threats)
			t.Activate();

		// 타이머 페이즈 -> 타이머 가동
		if (Current.useDeadline)
		{
			deadlineTimer.OnTick			+= HandleDeadlineTick;
			deadlineTimer.OnExpired	+= HandleDeadlineExpired;

			deadlineTimer.StartTimer(Current.deadlineDuration);
		}

		EventBus.RaiseHauntPhaseAdvanced(definition.huntId, CurrentPhaseIndex);
	}

	private void Update()
	{
		if (State != HauntState.Threatened) return;

		// 활성 위협 틱
		foreach (var t in Current.Threats)
			if (!t.IsNeutralized) t.Tick();

		// 파훼 조건 폴링 — 전부 만족하면 자동으로 다음 단계
		if (Current.Counters.All(c => c.IsSatisfied))
			MarkCountered();
	}

	/// <summary>
	/// 각 페이즈 진행 성공 시 다음 페이즈로 넘김
	/// ObjectiveReady 상태 => 모든 기믹 성공
	/// </summary>
	private void MarkCountered()
	{
		State = HauntState.Countered;
		CleanupPhase();

		if (CurrentPhaseIndex + 1 < phases.Length)
		{
			CurrentPhaseIndex++;
			ActivateThreat();
		}
		else
		{
			State = HauntState.ObjectiveReady;
		}
	}

    /// <summary>
    /// 최종 페이즈까지 임무 완료
    /// </summary>
    public void CompleteHaunt()
	{
		if (State != HauntState.ObjectiveReady) return;
		State = HauntState.Cleared;
		EventBus.RaiseHauntCleared(definition.huntId);
	}

	/// <summary>
	/// 규칙 위반 혹은 페이즈 진행 중 실패 발생 시
	/// </summary>
	/// <param name="reason">실패 사유</param>
	public void Fail(FailReason reason)
	{
		switch (reason)
		{
			case FailReason.Caught:					// 즉사
				EventBus.RaisePlayerDeath();
				break;
			default:										// 비치명 -> 오염도 상승
				ContaminationSystem.Instance.Add(failContaminationAmount);
				break;
		}
	}

	/// <summary>
	/// 타이머 활성화 이후 지속 피해
	/// </summary>
	/// <param name="remaining"></param>
	private void HandleDeadlineTick(float remaining)
	{
		// 밀물형: 데드라인 동안 지속 피해
		if (Current.contaminationPerSecondDuringDeadline > 0f)
			ContaminationSystem.Instance.Add(Current.contaminationPerSecondDuringDeadline * Time.deltaTime);
	}

	/// <summary>
	/// 타이머 종료
	/// </summary>
    private void HandleDeadlineExpired() => Fail(FailReason.DeadlineExpired);

    /// <summary>
    /// 현재 페이즈 데이터 정리 및 다음 페이즈 혹은 레벨 준비
    /// </summary>
    private void CleanupPhase()
	{
		foreach (var t in Current.Threats)
			t.Neutralize();

		if (Current.useDeadline)
		{
			deadlineTimer.Cancel();
			deadlineTimer.OnTick -= HandleDeadlineTick;
			deadlineTimer.OnExpired -= HandleDeadlineExpired;
		}
	}
}
