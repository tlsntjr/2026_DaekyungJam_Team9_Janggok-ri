using UnityEngine;

/// <summary>
/// 인면어 방향별 스프라이트 애니메이터 — 사이드/정면/후면 × 헤엄/공격 6모션.
///
/// FishMovement는 루트를 진행 방향으로 회전(z)시키는데, 방향별 스프라이트는 회전하면 안 되므로
/// 이 컴포넌트가 매 프레임 스프라이트 회전을 수직으로 상쇄하고, 대신 진행 방향에 맞는 모션을 고른다.
/// 좌우는 사이드 모션 + flipX로 처리 (원본이 오른쪽을 보면 Side Faces Right 체크).
///
/// 셋업: 스프라이트 자식 오브젝트(도약 높이용 leapVisual과 같은 오브젝트)에 Animator + 이 컴포넌트.
///       Animator 컨트롤러에 스테이트 6개만 던져 넣으면 됨 — 트랜지션 연결 불필요 (코드가 Play로 직접 점프).
///       스테이트 이름이 다르면 인스펙터에서 맞춰줄 것. 헤엄 클립은 Loop Time 켜기, 공격 클립은 취향껏.
/// </summary>
public class FishAnimator : MonoBehaviour
{
    [Header("참조 (비우면 자동 탐색 — movement는 부모, 나머지는 자신)")]
    [SerializeField] private FishMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;

    [Header("스테이트 이름 — Animator 컨트롤러의 스테이트와 일치시킬 것")]
    [SerializeField] private string swimSide = "SwimSide";
    [SerializeField] private string swimFront = "SwimFront";   // 아래(화면 앞)로 이동 — 얼굴이 보이는 방향
    [SerializeField] private string swimBack = "SwimBack";     // 위로 이동 — 등이 보이는 방향
    [SerializeField] private string attackSide = "AttackSide";
    [SerializeField] private string attackFront = "AttackFront";
    [SerializeField] private string attackBack = "AttackBack";

    [Header("사이드 원본이 바라보는 방향 — 오른쪽을 보면 체크 (왼쪽 이동 시 자동 flipX)")]
    [SerializeField] private bool sideFacesRight = true;

    [Header("사이드 우대 배율 — 대각선을 사이드로 취급하는 정도 (1 = 45° 반반, 클수록 정면/후면 구간이 좁아짐)")]
    [SerializeField, Range(1f, 3f)] private float sidePreference = 1.6f;   // 1.6 ≈ 수직 ±32° 안쪽만 정면/후면

    [Header("방향 전환 히스테리시스 — 경계에서 사이드/정면이 파닥파닥 바뀌는 것 방지 (0 = 끔)")]
    [SerializeField, Range(0f, 0.4f)] private float directionHysteresis = 0.15f;

    [Header("진단 — 모션 전환을 콘솔에 출력 (세팅 확인용, 잡히면 끌 것)")]
    [SerializeField] private bool debugLog = false;

    private string currentState;
    private bool wasAttacking;
    private bool lastWasSide;
    private float nextWarnTime;

    private void Awake()
    {
        if (movement == null) movement = GetComponentInParent<FishMovement>();
        if (animator == null) animator = GetComponent<Animator>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (movement == null)
            Debug.LogWarning("[FishAnimator] 부모에서 FishMovement를 찾지 못함 — 모션 전환 불가");
        if (animator == null)
            Debug.LogWarning("[FishAnimator] Animator가 없음 — 같은 오브젝트에 Animator 추가 필요");
        else if (animator.runtimeAnimatorController == null)
            Debug.LogWarning("[FishAnimator] Animator에 컨트롤러가 비어 있음 — 6개 스테이트가 든 컨트롤러 연결 필요");
    }

    private void LateUpdate()
    {
        if (movement == null || animator == null) return;

        // 루트 회전 상쇄 — 루트의 z회전은 조준/이동 방향 계산용일 뿐, 방향별 스프라이트는 항상 수직
        transform.rotation = Quaternion.identity;

        Vector2 dir = movement.FacingDirection;
        bool attacking = movement.IsAttacking;

        // 사이드/정후면 판정 — 사이드 우대(대각선은 사이드로) + 히스테리시스(경계 파닥임 방지).
        // 45° 반반 분할이면 비스듬한 돌진이 죄다 정면 모션으로 읽혀 "계속 정면으로 온다"는 느낌이 됨
        float bias = lastWasSide ? directionHysteresis : -directionHysteresis;
        bool side = Mathf.Abs(dir.x) * sidePreference + bias >= Mathf.Abs(dir.y);
        lastWasSide = side;

        string state;
        if (side)
        {
            state = attacking ? attackSide : swimSide;
            if (sr != null && Mathf.Abs(dir.x) > 0.001f)
                sr.flipX = (dir.x < 0f) == sideFacesRight;
        }
        else
        {
            if (sr != null) sr.flipX = false;
            state = dir.y > 0f
                ? (attacking ? attackBack : swimBack)
                : (attacking ? attackFront : swimFront);
        }

        // 모션이 바뀌었거나 새 공격이 시작된 순간이면 처음부터 재생 (같은 방향 연속 도약도 매번 처음부터)
        bool attackJustStarted = attacking && !wasAttacking;
        wasAttacking = attacking;

        if (state == currentState && !attackJustStarted) return;

        // 스테이트 존재 검증 — Play는 없는 이름을 받으면 "조용히" 실패해서 모션이 영영 안 바뀜
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (Time.time >= nextWarnTime)
            {
                nextWarnTime = Time.time + 2f;
                Debug.LogWarning($"[FishAnimator] Animator 컨트롤러에 '{state}' 스테이트가 없음 — " +
                                 "컨트롤러의 스테이트 이름과 인스펙터의 이름 필드를 맞춰줄 것 (레이어 0 기준)");
            }
            return;
        }

        if (debugLog)
            Debug.Log($"<color=cyan>[FishAnimator]</color> 모션 전환: {currentState ?? "(없음)"} → {state} (dir: {dir.x:F2}, {dir.y:F2}, 공격: {attacking})");

        currentState = state;
        animator.Play(state, 0, 0f);
    }
}
