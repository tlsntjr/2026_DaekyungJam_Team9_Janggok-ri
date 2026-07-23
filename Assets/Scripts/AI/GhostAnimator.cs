using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 갯벌 귀신(덩어리형) 애니메이터 — 걷기 모션 없이 방향별 idle 3종 + 정면 비명 1종.
///
/// 이동 방향은 "위치 변화"로 추정하므로 GhostPatrol/StalkerGhost 어느 쪽이든 코드 수정 없이 동작.
/// 멈춰 있으면 마지막 방향의 idle을 유지한다. 좌우는 사이드 + flipX.
///
/// 셋업: 스프라이트 오브젝트에 Animator + 이 컴포넌트.
///       컨트롤러에 스테이트 4개 (IdleSide/IdleFront/IdleBack/ScreamFront — 트랜지션 불필요, 코드가 Play로 점프).
///       idle 클립 3개는 Loop Time 켜기, 비명 클립은 끄기 (재생 길이만큼 유지 후 idle 복귀).
/// 비명 트리거: ① Scream On Player Near 체크 (반경 안 접근 시 자동) ② 외부에서 PlayScream() 호출.
/// </summary>
public class GhostAnimator : MonoBehaviour
{
    [Header("참조 (비우면 자동 탐색 — moveRoot는 부모, 나머지는 자신)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform moveRoot;     // 실제로 이동하는 루트 — 방향 추정 기준

    [Header("스테이트 이름 — Animator 컨트롤러와 일치시킬 것")]
    [SerializeField] private string idleSide = "IdleSide";
    [SerializeField] private string idleFront = "IdleFront";     // 아래로 이동/기본 — 얼굴이 보이는 방향
    [SerializeField] private string idleBack = "IdleBack";       // 위로 이동 — 등이 보이는 방향
    [SerializeField] private string screamFront = "ScreamFront";

    [Header("방향 판정 (FishAnimator와 동일 규칙)")]
    [SerializeField] private bool sideFacesRight = true;              // 사이드 원본이 오른쪽을 보면 체크
    [SerializeField, Range(1f, 3f)] private float sidePreference = 1.6f;
    [SerializeField, Range(0f, 0.4f)] private float directionHysteresis = 0.15f;
    [SerializeField] private float moveSpeedThreshold = 0.05f;        // 초당 이 거리 미만이면 정지로 보고 방향 유지

    [Header("근접 비명 (선택) — 플레이어가 반경 안에 들어오면 자동으로 비명")]
    [SerializeField] private bool screamOnPlayerNear = false;
    [SerializeField] private float screamRadius = 3f;
    [SerializeField] private float screamCooldown = 10f;
    [SerializeField] private Transform player;   // 비우면 Player 태그 자동 탐색

    [Header("비명 사운드 (비우면 모션만 — 3D 이벤트 권장, 거리 페이드)")]
    [SerializeField] private EventReference screamSfx;

    private Vector3 lastPos;
    private Vector2 facing = Vector2.down;   // 시작은 정면
    private bool lastWasSide;
    private string currentState;
    private float screamUntil;               // 이 시각까지는 비명 유지 (idle 전환 금지)
    private float nextScreamAllowed;
    private float nextWarnTime;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (moveRoot == null) moveRoot = transform.parent != null ? transform.parent : transform;
        lastPos = moveRoot.position;

        if (animator == null)
            Debug.LogWarning("[GhostAnimator] Animator가 없음 — 같은 오브젝트에 추가 필요");
        else if (animator.runtimeAnimatorController == null)
            Debug.LogWarning("[GhostAnimator] Animator 컨트롤러가 비어 있음 — 스테이트 4개짜리 컨트롤러 연결 필요");
    }

    private void Start()
    {
        if (screamOnPlayerNear && player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void LateUpdate()
    {
        if (animator == null) return;

        // 이동 방향 추정 — 충분히 움직였을 때만 갱신 (멈추면 마지막 방향 유지)
        Vector3 pos = moveRoot.position;
        Vector2 delta = pos - lastPos;
        lastPos = pos;

        if (Time.deltaTime > 0f && delta.magnitude / Time.deltaTime >= moveSpeedThreshold)
            facing = delta.normalized;

        // 근접 자동 비명
        if (screamOnPlayerNear && player != null
            && Time.time >= nextScreamAllowed
            && Vector2.Distance(player.position, moveRoot.position) <= screamRadius)
            PlayScream();

        // 비명 재생 중엔 idle로 덮지 않음
        if (Time.time < screamUntil) return;

        // 방향별 idle 선택 — 사이드 우대 + 히스테리시스 (FishAnimator와 동일 규칙)
        float bias = lastWasSide ? directionHysteresis : -directionHysteresis;
        bool side = Mathf.Abs(facing.x) * sidePreference + bias >= Mathf.Abs(facing.y);
        lastWasSide = side;

        string state;
        if (side)
        {
            state = idleSide;
            if (sr != null && Mathf.Abs(facing.x) > 0.001f)
                sr.flipX = (facing.x < 0f) == sideFacesRight;
        }
        else
        {
            if (sr != null) sr.flipX = false;
            state = facing.y > 0f ? idleBack : idleFront;
        }

        TryPlay(state);
    }

    /// <summary>정면 비명 재생 — 재생 길이만큼 유지 후 자동으로 idle 복귀. 외부(발각 연출 등)에서 호출 가능</summary>
    public void PlayScream()
    {
        if (animator == null) return;
        nextScreamAllowed = Time.time + screamCooldown;

        if (sr != null) sr.flipX = false;   // 비명은 정면 고정
        if (!TryPlay(screamFront)) return;

        if (!screamSfx.IsNull && SoundManager.Instance != null)
            SoundManager.Instance.PlayOneShot(screamSfx, moveRoot.position);

        StartCoroutine(HoldScream());
    }

    private IEnumerator HoldScream()
    {
        yield return null;   // Play가 스테이트에 반영될 프레임까지 대기 후 실제 클립 길이 조회
        screamUntil = Time.time + animator.GetCurrentAnimatorStateInfo(0).length;
    }

    /// <summary>스테이트 존재 검증 포함 재생 — 없는 이름이면 경고 (Play는 조용히 실패하므로)</summary>
    private bool TryPlay(string state)
    {
        if (state == currentState) return true;

        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (Time.time >= nextWarnTime)
            {
                nextWarnTime = Time.time + 2f;
                Debug.LogWarning($"[GhostAnimator] Animator 컨트롤러에 '{state}' 스테이트가 없음 — 이름을 맞춰줄 것 (레이어 0 기준)");
            }
            return false;
        }

        currentState = state;
        animator.Play(state, 0, 0f);
        return true;
    }
}
