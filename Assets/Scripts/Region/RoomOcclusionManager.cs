using UnityEngine;

/// <summary>
/// 방 단위 시야 가림 관리자. 각 방마다 그 방을 덮는 "가림막"(검은 스프라이트 묶음)을 만들어두고,
/// 플레이어가 들어간 방의 가림막만 끄고 나머지는 전부 켠다.
///
/// 셰이더로 화면을 마스킹하는 대신 실제 월드 스프라이트를 켜고 끄는 방식이라:
///   - 카메라가 움직여도 벽처럼 자연스럽게 따라다님 (뷰포트 좌표 재계산 없음 — 슬라이딩 어색함 없음)
///   - ㄱ자 등 비정형 방도 가림막을 여러 조각(스프라이트)으로 나눠 그 모양대로 채우면 됨 (셰이더 도형 계산 불필요)
///
/// 타일맵은 그대로 하나로 두면 됨 — 가림막은 그 위에 얹는 별도 오브젝트라 맵 구조와 완전히 무관.
/// 씬에 1개 배치.
/// </summary>
public class RoomOcclusionManager : MonoBehaviour
{
    public static RoomOcclusionManager Instance { get; private set; }

    [Header("씬의 모든 방 가림막 (시작 시 시작 방 제외 전부 켜짐 = 가려짐)")]
    [SerializeField] private GameObject[] allRoomCovers;

    [Header("시작 시 보여줄 방의 가림막 (비우면 전부 가려진 채 시작)")]
    [SerializeField] private GameObject startingRoomCover;

    private GameObject currentCover;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => EnterRoom(startingRoomCover);

    /// <summary>
    /// 이 방의 가림막을 끄고(공개), 나머지는 전부 켬(은폐). RoomZone이 진입 시 호출.
    /// </summary>
    public void EnterRoom(GameObject roomCover)
    {
        if (roomCover == currentCover) return;
        currentCover = roomCover;

        foreach (var cover in allRoomCovers)
            if (cover != null) cover.SetActive(cover != roomCover);
    }
}
