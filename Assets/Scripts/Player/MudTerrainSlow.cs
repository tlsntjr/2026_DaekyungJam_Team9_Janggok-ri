using UnityEngine;

/// <summary>
/// 갯벌 진흙 감속 — 플레이어가 진흙 바닥 레이어 위에 서 있으면 이동속도를 낮춘다 (기본 30% 감소).
/// 플레이어에 부착. 발소리(FootstepSurface)와 같은 "발밑 바닥 레이어 감지" 방식이라
/// 별도 존을 칠할 필요 없이, 바닥 타일맵의 트리거 콜라이더+레이어 세팅을 그대로 재활용한다.
///
/// 판정 우선순위: 면제 레이어(다리 등 안전 지대) 위 → 정상 속도.
///                아니면서 진흙 레이어 위 → 감속. 그 외 → 정상.
/// (다리가 진흙 위에 겹쳐 깔려 있어도 다리가 이김 — "다리 위는 안전"이라는 기획 그대로)
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class MudTerrainSlow : MonoBehaviour
{
    [Header("진흙 바닥 레이어 (밟으면 감속)")]
    [SerializeField] private LayerMask mudMask;

    [Header("면제 레이어 (다리 등 — 진흙과 겹쳐 있어도 이 위면 정상 속도)")]
    [SerializeField] private LayerMask exemptMask;

    [Header("감속 배율 (0.7 = 30% 감소)")]
    [SerializeField, Range(0.1f, 1f)] private float mudMultiplier = 0.7f;

    [Header("전환 부드러움 (경계 넘을 때 속도가 스냅되지 않게)")]
    [SerializeField] private float blendSpeed = 5f;

    private PlayerMovement movement;
    private float current = 1f;

    private void Awake() => movement = GetComponent<PlayerMovement>();

    private void OnDisable()
    {
        if (movement != null) movement.TerrainMultiplier = 1f;   // 비활성화 시 감속이 영구히 남지 않게
    }

    private void Update()
    {
        float target = ComputeTarget();
        current = Mathf.MoveTowards(current, target, blendSpeed * Time.deltaTime);
        movement.TerrainMultiplier = current;
    }

    private float ComputeTarget()
    {
        Vector2 pos = transform.position;   // 피벗 = 발밑 기준

        if (Physics2D.OverlapPoint(pos, exemptMask) != null) return 1f;       // 다리 위 — 안전
        if (Physics2D.OverlapPoint(pos, mudMask) != null) return mudMultiplier;   // 진흙 위 — 감속
        return 1f;
    }
}
