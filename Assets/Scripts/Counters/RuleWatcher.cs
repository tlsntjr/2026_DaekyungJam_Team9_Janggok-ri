using UnityEngine;

public class RuleWatcher : MonoBehaviour, ICounterCondition
{
    [SerializeField] private HauntController controller;
    [SerializeField] private FailReason violationPenalty = FailReason.RuleViolation;

    public bool IsSatisfied { get; private set; }

    /// <summary>
    /// 올바른 선택
    /// </summary>
    public void MarkSatisfied() => IsSatisfied = true;

    /// <summary>
    /// 룰 어겼을 때 패널티
    /// </summary>
    public void MarkViolated() => controller.Fail(violationPenalty);

    /// <summary>
    /// 페이즈 내 같은 클래스 사용 시 리셋
    /// </summary>
    public void ResetRule() => IsSatisfied = false;
}