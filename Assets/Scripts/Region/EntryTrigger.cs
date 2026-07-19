using UnityEngine;

/// <summary>
/// 구역 입구에 놓는 트리거. ExitTrigger의 반대(종료가 아니라 시작).
/// 플레이어가 닿으면 HauntController.Trigger() 호출 → 0번 페이즈부터 시작.
/// </summary>
public class EntryTrigger : MonoBehaviour
{
    [SerializeField] private HauntController haunt;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("[Test] Entry Activated!");
        haunt.Trigger();
    }
}
