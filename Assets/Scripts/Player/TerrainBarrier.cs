using UnityEngine;

/// <summary>
/// 통행 불가 지형 차단 — 물리 벽 없이 "이동 입력" 단계에서 막는다.
/// 플레이어에 부착. MudTerrainSlow와 같은 발밑 레이어 판정(OverlapPoint) 방식이라
/// 지형 콜라이더는 전부 Is Trigger로 두면 되고, 물리 밀어내기·끼임·모서리 튕김이 원천적으로 없다.
///
/// 판정 우선순위: 면제 레이어(부두·다리 등 통행로) 위 → 통과.
///                떠 있는 SinkingBlock 위 → 통과 (블럭이 가라앉아 있으면 자동으로 막힘 — 기믹 동선이 공짜로 성립).
///                차단 레이어(바다 등) 위 → 이동 불가. 그 외 → 통과.
/// 실제 차단은 PlayerMovement.FixedUpdate가 CanStandAt으로 조회해 수행 —
/// 축 분리 판정이라 물가를 따라 미끄러지듯 걸을 수 있다.
///
/// 씬 세팅: 바다 타일맵 = TilemapCollider2D(Is Trigger) + 차단 레이어,
///          통행로 타일맵 = TilemapCollider2D(Is Trigger) + 면제 레이어.
///          두 타일맵 모두 타일 에셋의 Collider Type을 "Grid"로 (None이면 콜라이더가 안 생김).
///          CompositeCollider2D·Rigidbody2D는 필요 없음.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class TerrainBarrier : MonoBehaviour
{
    [Header("차단 레이어 (바다 등 — 이 위로는 이동 불가)")]
    [SerializeField] private LayerMask blockedMask;

    [Header("면제 레이어 (부두·다리 등 — 차단 지형과 겹쳐 있어도 이 위면 통과)")]
    [SerializeField] private LayerMask exemptMask;

    [Header("떠 있는 SinkingBlock 위를 통행 가능으로 취급")]
    [SerializeField] private bool includeSinkingBlocks = true;

    [Header("판정 히트박스 — 발밑용으로 세팅한 콜라이더를 등록하면 그 오프셋·크기 그대로 검사")]
    [SerializeField] private Collider2D footCollider;   // 플레이어의 발밑 콜라이더 (BoxCollider2D 등). 비우면 아래 Footprint 값 사용

    [Header("Footprint 폴백 — footCollider가 없을 때의 판정 발판 절반 크기 (0이면 발밑 한 점만)")]
    [SerializeField] private Vector2 footprintExtents = new Vector2(0.16f, 0.1f);   // 가장 좁은 통행로 절반 폭보다 작게 유지할 것

    /// <summary>
    /// 이 지점에 설 수 있는가 — PlayerMovement가 이동 적용 직전에 목적지를 검사.
    /// footCollider가 등록돼 있으면 그 콜라이더의 현재 오프셋·크기를 후보 위치로 평행이동해 판정 —
    /// 인스펙터에서 세팅한 발밑 영역과 판정이 정확히 일치한다.
    /// 발판 네 모서리를 각각 판정(모서리마다 면제 우선 적용)하므로,
    /// 부두 끝에 서면 발판 전체가 통행로 안에 있어야 통과된다.
    /// </summary>
    public bool CanStandAt(Vector2 pos)
    {
        Vector2 center = pos;
        Vector2 ext = footprintExtents;

        if (footCollider != null)
        {
            Bounds b = footCollider.bounds;
            center = pos + ((Vector2)b.center - (Vector2)transform.position);   // 피벗→콜라이더 중심 오프셋 유지
            ext = b.extents;
        }

        if (ext.sqrMagnitude <= 0f) return PointOk(center);

        return PointOk(center)
            && PointOk(center + new Vector2( ext.x,  ext.y))
            && PointOk(center + new Vector2(-ext.x,  ext.y))
            && PointOk(center + new Vector2( ext.x, -ext.y))
            && PointOk(center + new Vector2(-ext.x, -ext.y));
    }

    private bool PointOk(Vector2 p)
    {
        if (Physics2D.OverlapPoint(p, exemptMask) != null) return true;
        if (includeSinkingBlocks && SinkingBlock.IsAnyBlockSafeAt(p)) return true;
        return Physics2D.OverlapPoint(p, blockedMask) == null;
    }
}
