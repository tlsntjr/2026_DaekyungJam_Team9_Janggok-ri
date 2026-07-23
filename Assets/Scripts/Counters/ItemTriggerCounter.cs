using UnityEngine;

public class ItemTriggerCounter : MonoBehaviour, ICounterCondition
{
    private bool isSatisfied = false;
    public bool IsSatisfied => isSatisfied;

    // 주의: 예전엔 OnDisable에서 자동 충족했지만, 페이즈 배선(enableOnStart/disableOnStart·토글러)이
    // 오브젝트를 껐다 켜는 것만으로 "주운 것"으로 오인돼 페이즈가 통째로 스킵되는 사고가 있었음.
    // 이제 충족은 실제 획득 순간 Pickup.FinishPickup이 SetSatisfied()를 직접 호출할 때만 일어난다.

    public void SetSatisfied()
    {
        if (isSatisfied) return; // 중복 호출 방지
        isSatisfied = true;
        Debug.Log($"<color=green>[ItemTriggerCounter]</color> {gameObject.name} 획득 처리 — 카운터 충족");
    }
}