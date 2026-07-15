using System.Linq;
using UnityEngine;

public enum EndingType { Bad, Normal, True }

public class EndingEvaluator : MonoBehaviour
{
    [SerializeField] EndingCondition[] conditions;

    /// 우선순위가 높은(숫자가 작은) 조건부터 검사해 첫 만족 조건의 엔딩 반환.
    /// 아무것도 만족 못 하면 배드 엔딩으로 기본값 반환
    public EndingType Evaluate(IObjective state)
    {
        foreach (var c in conditions.OrderBy(c => c.priority))
            if (c.IsSatisfied(state))
                return c.endingType;

        return EndingType.Bad;
    }
}
