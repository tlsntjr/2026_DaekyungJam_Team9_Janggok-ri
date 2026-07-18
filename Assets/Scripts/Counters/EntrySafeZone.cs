using UnityEngine;

public class EntrySafeZone : MonoBehaviour, ICounterCondition
{
    [SerializeField] private string playerTag = "Player";
    private bool isPlayerInside = false;

    // HauntController가 이 값을 매 프레임 확인합니다.
    public bool IsSatisfied => isPlayerInside;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            isPlayerInside = true;
            Debug.Log("<color=green>[SafeZone]</color> 안전 발판 도달! 페이즈 조건 충족.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            isPlayerInside = false;
        }
    }
}