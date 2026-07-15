using UnityEngine;

/// <summary>
/// 몬스터가 시각 및 청각으로 인지 못하는 영역 클래스
/// </summary>
public class Concealment : MonoBehaviour
{
    static int insideCount;
    public static bool IsPlayerConcealed => insideCount > 0;

    public bool PlayerInside { get; private set; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerInside = true;
        insideCount++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerInside = false;
        insideCount--;
    }
}