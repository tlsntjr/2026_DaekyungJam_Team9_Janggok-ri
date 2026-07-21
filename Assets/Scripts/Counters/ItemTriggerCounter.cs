using UnityEngine;

public class ItemTriggerCounter : MonoBehaviour, ICounterCondition
{
    private bool isSatisfied = false;
    public bool IsSatisfied => isSatisfied;

    // [수정] 아이템이 비활성화될 때(즉, 주워질 때) 자동으로 호출되게 합니다.
    private void OnDisable()
    {
        // 씬이 종료되는 상황이 아닌, 실제로 게임 중에 비활성화될 때만 처리
        if (gameObject.scene.isLoaded)
        {
            SetSatisfied();
        }
    }

    public void SetSatisfied()
    {
        if (isSatisfied) return; // 중복 호출 방지
        isSatisfied = true;
    }
}