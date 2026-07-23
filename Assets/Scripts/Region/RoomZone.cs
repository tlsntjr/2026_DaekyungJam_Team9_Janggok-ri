using UnityEngine;

/// <summary>
/// 방 진입 트리거이자 그 방의 "보이는 영역" 정의.
/// 이 오브젝트에 Collider2D(Is Trigger)를 방 모양대로 여러 개 추가하면 됨 —
/// 사각형 방 = 1개, ㄱ자 방 = 2개 이어 붙이기 (최대 4개까지 셰이더가 지원).
/// 콜라이더 크기가 곧 화면에 드러날 영역이므로, 실제 방의 바닥/벽 안쪽에 맞춰 배치할 것.
/// 플레이어가 닿으면 RoomRevealDirector에 이 방의 사각형들만 보이게 요청.
/// </summary>
public class RoomZone : MonoBehaviour
{
    public const int MaxAreas = 4;

    [SerializeField] private string playerTag = "Player";

    public Collider2D[] Areas { get; private set; }

    private void Awake()
    {
        Areas = GetComponents<Collider2D>();

        if (Areas.Length == 0)
            Debug.LogWarning($"[RoomZone] {name}: Collider2D가 하나도 없습니다.");
        else if (Areas.Length > MaxAreas)
            Debug.LogWarning($"[RoomZone] {name}: Collider2D가 {Areas.Length}개라 셰이더 지원 한도({MaxAreas})를 넘습니다. 앞 {MaxAreas}개만 반영됩니다.");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        if (RoomRevealDirector.Instance == null) return;

        RoomRevealDirector.Instance.SetRoom(this);
    }
}
