using UnityEngine;
using System.Collections;
using FMODUnity;

/// <summary>
/// 인면어 이동/전투 상태 머신.
///   - Chase(1~2페이즈)  : 발각 순간 소형 괴성 1회 → 단순 실시간 추격. 파훼는 은신처.
///   - Berserk(3페이즈)   : "괴성(위치 고정) → 대기 → 관통 돌진 → 경직(두리번) → 범위 안이면 재돌진" 사이클 무한 반복.
///   - 물속 매복 모드(waterLurkMode, 양식장): 물속을 헤엄치다 시야/소음에 감지되면
///     "괴성(위치 고정) → 포물선 도약으로 덮침 → 착수 → 범위 안이면 재도약" — 돌진의 물 버전.
///     루트는 지면 직선 경로를 이동(판정 담당)하고 자식 스프라이트만 위로 떠서 높이를 표현한다 (Throwable과 같은 트릭).
/// 괴성 사용 규칙: 발각(추격 시작) = 소형 / 먼 소음(발전기 등)에 반응해 달려올 때 = 대형 / 광폭 = 전부 대형.
/// 돌진 확정 판정(괴성~돌진 종료) 동안은 외부 상태 전환(SetState)과 소음 반응이 잠긴다 —
/// 어그로 풀림/소음 유인 때문에 돌진이 도중에 어정쩡하게 끊기는 것 방지.
/// </summary>
public class FishMovement : MonoBehaviour
{
	public enum BehaviorState { Patrol, Suspect, Chase, Retreat, Berserk }

	[Header("상태 머신")]
	[SerializeField] private BehaviorState currentState = BehaviorState.Patrol;
	public BehaviorState CurrentState => currentState;

	[Header("참조 필드")]
	[SerializeField] private Transform playerTransform;
	[SerializeField] private HauntController controller;

	[Header("괴담 연동 (ChaseMusicController 등 huntId 기반 시스템용 — HauntDefinition.huntId와 일치시킬 것)")]
	[SerializeField] private string huntId;

	[Header("몸통 회전 — 방향별 스프라이트(FishAnimator) 사용 시 끔. 끄면 오브젝트는 회전하지 않고 내부 방향값만 갱신")]
	[SerializeField] private bool rotateBody = false;
	private Vector2 facingDir = Vector2.up;   // 진행/조준 방향 — rotateBody가 꺼져 있어도 항상 갱신됨

	[Header("순찰 설정 — 중심점 기준 반경 안을 자유 배회 (웨이포인트 불필요)")]
	[SerializeField] private float patrolRadius = 4f;
	[SerializeField] private Transform patrolCenter;          // 비우면 씬 시작 위치가 중심
	[SerializeField] private float patrolArrivalDistance = 0.2f;
	[SerializeField] private float patrolIdleMin = 0.5f;      // 지점 도착 후 잠깐 머무는 시간 (두리번거리는 여백)
	[SerializeField] private float patrolIdleMax = 1.5f;
	private Vector3 patrolHome;                               // 시작 위치 (patrolCenter 미지정 시 중심)
	private Vector3 patrolTarget;
	private bool hasPatrolTarget;
	private float patrolIdleUntil;

	[Header("이동 속도 설정")]
	[SerializeField] private float patrolSpeed = 2f;
	[SerializeField] private float suspectSpeed = 3.5f;
	[SerializeField] private float chaseSpeed = 5.5f;
	[SerializeField] private float noiseScreamWindup = 1.0f;      // 소음 반응 시 괴성을 지르며 이만큼 정지한 뒤 달려감
	[SerializeField] private float noiseArrivalDistance = 1.2f;   // 소음 지점 도착 판정 거리 — 발전기 등 충돌체가 있는 목표는 정확히 겹칠 수 없으므로 여유를 둠(안 두면 벽/오브젝트에 제자리서 계속 밀려붙는 현상 발생)

	[Header("추격(Chase) — 1~2페이즈: 발각 시 소형 괴성 1회 후 단순 추격 (파훼는 은신)")]
	[SerializeField] private float chaseScreamCooldown = 6f;      // 발각 괴성 재발화 최소 간격 (시야 경계에서 재발각 스팸 방지)

	[Header("광폭(Berserk) — 이 페이즈 인덱스에 도달하면 자동 발동 (0부터 셈 — 4페이즈 구성의 탈출 페이즈면 3)")]
	[SerializeField] private int berserkPhaseIndex = 3;
	[SerializeField] private float chargeWaitTime = 0.8f;
	[SerializeField] private float dashSpeed = 12f;
	[SerializeField] private float cooldownTime = 2.0f;
	[SerializeField] private float berserkEntryDelay = 1.2f;      // 광폭 진입 대형 괴성이 들릴 시간 확보 후 첫 돌진

	[Header("돌진 공통")]
	[SerializeField] private float dashDuration = 1.5f;           // 돌진 최대 시간 (목표 도달 시 조기 종료)
	[SerializeField] private float dashOvershoot = 3.5f;          // 고정 위치를 지나쳐 더 나아가는 거리 — 상어가 먹이를 물고 관통하듯
	[SerializeField] private float reDashRange = 5f;              // 경직이 끝난 시점에 이 범위 안에 남아 있으면(은신 제외) 즉시 재돌진
	[SerializeField] private float staggerScanAngle = 50f;        // 경직 중 좌우로 두리번거리는 각도(±)

	[Header("물속 매복 모드 (양식장) — 물속을 헤엄치다 감지 시 포물선 도약으로 덮침")]
	[SerializeField] private bool waterLurkMode = false;
	[SerializeField] private Collider2D[] waterZones;             // 물 영역 콜라이더(Is Trigger 권장) — 착수 후 복귀 지점이 이 안으로 강제됨. 순찰 웨이포인트는 물 안에 배치할 것
	[SerializeField] private Transform leapVisual;                // 포물선 높이를 표현할 자식 스프라이트 (비우면 자동 탐색 — 스프라이트가 루트에 직접 붙어 있으면 높이 연출 불가)
	[SerializeField] private float leapSpeed = 9f;                // 지면 거리 ÷ 이 값 = 체공 시간 (멀수록 오래 난다)
	[SerializeField] private float leapMinDuration = 0.35f;
	[SerializeField] private float leapMaxDuration = 0.95f;
	[SerializeField] private float leapArcHeight = 2.6f;          // 포물선 최대 높이 상한 (거리 비례로 낮아짐)
	[SerializeField] private float leapOvershoot = 2.5f;          // 고정 위치를 지나쳐 날아가는 거리 — 관통 돌진의 도약 버전
	[SerializeField] private float noiseLeapRange = 7f;           // 소음 조사 중 이 거리 안까지 접근하면 소음 지점을 향해 도약
	[SerializeField, Range(0f, 1f)] private float submergedAlpha = 0.55f;   // 물속에 있을 때 스프라이트 알파 (도약 중엔 원래대로)
	[SerializeField] private float waterReturnSpeed = 6f;         // 물 밖(부두 위 등)에 착지했을 때 파닥이며 물로 되돌아가는 속도
	[SerializeField] private EventReference splashOutSfx;         // 물을 가르고 솟구칠 때 (비우면 스킵)
	[SerializeField] private EventReference splashInSfx;          // 착수할 때 (비우면 스킵)
	[SerializeField] private LayerMask landingBlockedMask;        // 착수 금지 레이어 (다리·부두 등 — 바다 콜라이더가 밑에 깔려 있어도 이 위엔 착수 안 함)
	[SerializeField, Range(0f, 1f)] private float diveAlpha = 0.12f;   // 은신당해 잠수할 때 알파 (거의 안 보임)
	[SerializeField] private float diveDuration = 2.5f;           // 잠수한 채 자기 구역으로 물러나는 시간

	[SerializeField] private bool isMovingActive = false;

	private Vector3 dashTarget;
	private bool isCharging = false;   // 괴성~돌진 시작 사이 (위치 고정됨)
	private bool isDashing = false;    // 돌진 이동 중
	private Coroutine dashCycleCoroutine;
	private float berserkEntryUntil;

	private Vector3 targetNoisePosition;
	private Coroutine berserkTimerCoroutine;
	private Coroutine suspectTimerCoroutine;
	private bool hasTriggeredBerserkMode = false;
	private bool isNoiseWindup = false;    // 소음 반응 괴성 중 (제자리 정지)
	private int lastThreatLevel = -1;      // ThreatStateChanged 중복 발화 방지
	private float lastChaseScreamTime = -999f;

	// ── 물속 매복 모드 내부 상태 ──
	private bool noiseLeapDone;            // 같은 소음에 도약 1회만 (연속 도약 스팸 방지)
	private Vector2 lastSuspectPos;        // 소음 접근 정체 감지용 (물가에 막혔는지)
	private float suspectStallTimer;
	private SpriteRenderer[] bodySprites;
	private float[] baseAlphas;
	private Vector3 leapVisualBaseLocal;
	private Transform leapShadow;          // 도약 중 그림자 — 코루틴 강제 종료 시 정리용으로 멤버 보관

	/// <summary>
	/// 상태 변경의 단일 통로 — 상태를 바꾸면서 위협 레벨(0=평시, 1=수상, 2=추격)을 브로드캐스트.
	/// ChaseMusicController가 이 이벤트로 추격 BGM을 켜고 끈다.
	/// Chase 진입(발각) 순간엔 소형 괴성 1회 — 재발각 스팸은 chaseScreamCooldown으로 방지.
	/// </summary>
	private void ApplyState(BehaviorState s)
	{
		bool enteredChase = s == BehaviorState.Chase && currentState != BehaviorState.Chase;
		currentState = s;

		// Suspect를 벗어나는 전환이면 잔여 복귀 타이머를 반드시 정리 —
		// 안 하면 10초 뒤 타이머가 Chase/Berserk 상태를 Patrol로 강등시키면서 긴장 오디오(BGM·심장)를 꺼버림
		if (s != BehaviorState.Suspect && suspectTimerCoroutine != null)
		{
			StopCoroutine(suspectTimerCoroutine);
			suspectTimerCoroutine = null;
			isNoiseWindup = false;
		}

		// 위협 레벨: 0=평시, 1=수상, 2=추격, 3=광폭 — FMOD에서 광폭 전용 레이어를 구분할 수 있게 분리
		// (ChaseMusicController 등 기존 구독자는 >=2 판정이라 영향 없음)
		RaiseThreatLevel(s == BehaviorState.Berserk ? 3
					   : s == BehaviorState.Chase ? 2
					   : s == BehaviorState.Suspect ? 1 : 0);

		// 물속 모드에선 도약 루틴 자체가 매번 괴성(조준 텔레그래프)을 지르므로 진입 괴성은 생략 — 이중 발화 방지
		if (enteredChase && !waterLurkMode && Time.time - lastChaseScreamTime >= chaseScreamCooldown)
		{
			lastChaseScreamTime = Time.time;
			EventBus.RaiseMonsterScreamed(transform.position);   // 발각! — 소형 괴성 (파동·글리치는 디렉터가 처리)
		}
	}

	private void RaiseThreatLevel(int level)
	{
		if (level == lastThreatLevel) return;
		lastThreatLevel = level;
		if (!string.IsNullOrEmpty(huntId))
			EventBus.RaiseThreatStateChanged(huntId, level);
	}

	/// <summary>
	/// 에디터 테스트용 — 플레이 중 FishMovement 컴포넌트 헤더 우클릭 → "TEST: 추격(도약) 강제 시작".
	/// Gaze·페이즈 등록 없이 도약 사이클만 바로 확인할 수 있다.
	/// </summary>
	[ContextMenu("TEST: 추격(도약) 강제 시작")]
	private void DebugForceChase()
	{
		if (playerTransform == null) playerTransform = GameObject.FindWithTag("Player")?.transform;
		isMovingActive = true;
		ApplyState(BehaviorState.Chase);
		Debug.Log("<color=cyan>[FishMovement TEST]</color> Chase 강제 진입 — 물속 모드면 괴성 후 도약이 나와야 정상");
	}

	/// <summary>괴성~돌진 종료까지 = 확정 판정 구간. 외부 상태 전환·소음 반응을 받지 않음.</summary>
	private bool IsDashLocked => isCharging || isDashing;

	/// <summary>현재 바라보는(진행) 방향 — FishAnimator가 방향별 모션을 고르는 데 사용 (회전 여부와 무관)</summary>
	public Vector2 FacingDirection => facingDir;

	/// <summary>공격 판정 구간(조준 괴성~도약/돌진 비행) 여부 — FishAnimator가 공격 모션으로 전환</summary>
	public bool IsAttacking => isCharging || isDashing;

	private void Awake()
	{
		// 도약 높이 표현용 자식 스프라이트 — 루트는 지면(판정), 자식만 위로 뜬다
		if (leapVisual == transform)
		{
			Debug.LogWarning("[FishMovement] leapVisual에 루트 자신이 들어 있음 — 자식 스프라이트여야 함. 해제하고 자동 탐색으로 대체");
			leapVisual = null;
		}
		if (leapVisual == null)
		{
			var sr = GetComponentInChildren<SpriteRenderer>();
			if (sr != null && sr.transform != transform) leapVisual = sr.transform;
		}
		if (leapVisual != null) leapVisualBaseLocal = leapVisual.localPosition;
		else if (waterLurkMode)
			Debug.LogWarning("[FishMovement] 물속 모드: 스프라이트가 루트에 직접 붙어 있어 도약 높이 연출 불가 — 스프라이트를 자식 오브젝트로 분리 권장");

		bodySprites		= GetComponentsInChildren<SpriteRenderer>();
		baseAlphas		= new float[bodySprites.Length];
		for (int i = 0; i < bodySprites.Length; i++) baseAlphas[i] = bodySprites[i].color.a;

		patrolHome = transform.position;   // 순찰 중심 폴백 — patrolCenter 미지정 시 시작 위치

		if (waterLurkMode) SetSubmergedVisual(true);
	}

	private void OnEnable()
	{
		isMovingActive = true;

		if (controller != null && controller.CurrentPhaseIndex >= berserkPhaseIndex)
		{
			TriggerBerserkModeImmediate();
		}
	}

	private void Start()
	{
		isMovingActive = true;
	}

	private void Update()
	{
		if (controller != null && controller.CurrentPhaseIndex >= berserkPhaseIndex && !hasTriggeredBerserkMode)
		{
			TriggerBerserkModeImmediate();
		}

		if (!isMovingActive) return;

		ExecuteMovement();
	}

	private void TriggerBerserkModeImmediate()
	{
		hasTriggeredBerserkMode = true;
		isMovingActive = true;

		EventBus.RaiseMonsterScreamed(transform.position, true);   // 광폭 진입 선언 — 대형 괴성
		berserkEntryUntil = Time.time + berserkEntryDelay;         // 대형 괴성과 첫 돌진의 소형 괴성이 겹치지 않게

		ForceState(BehaviorState.Berserk, 60f);
		Debug.Log("<color=red>[FishMovement 광폭 기동]</color> 3페이즈 진입 — 돌진 사이클을 무한 반복합니다!");
	}

	private void ExecuteMovement()
	{
		// 스토리 시퀀스(녹음기·수첩 낭독 등) 동안은 정지 — 낭독 중 습격으로 스토리 진행이 끊기는 것 방지.
		// 이미 진행 중이던 도약/돌진 판정(코루틴)은 완주하되, 새 행동은 시작하지 않는다
		if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsSequenceActive) return;

		// 돌진/도약 사이클이 도는 동안엔 상태 브랜치 이동 코드가 개입하지 않음 — 이동은 사이클 코루틴이 전담
		if (dashCycleCoroutine != null) return;

		switch (currentState)
		{
			case BehaviorState.Patrol:
				ExecuteRadiusPatrol();
				break;

			case BehaviorState.Suspect:
				if (isNoiseWindup) return;   // 괴성을 지르는 동안 제자리 — "반응했다"는 텔레그래프

				// 물속 모드: 소음 지점 사정거리 안까지 헤엄쳐 접근했으면 소음 지점을 향해 1회 도약 —
				// 조개껍데기가 떨어진 자리·시동 건 발전기를 물속에서 덮치는 그림
				if (waterLurkMode && !noiseLeapDone
					&& Vector3.Distance(transform.position, targetNoisePosition) <= noiseLeapRange)
				{
					noiseLeapDone = true;
					dashCycleCoroutine = StartCoroutine(NoiseLeapRoutine());
					return;
				}

				// 목표 지점 근처(noiseArrivalDistance 이내)에 도착했으면 더 밀고 들어가지 않고 그 자리에서 주시만 함 —
				// 발전기 등 충돌체가 있는 목표는 정확히 겹칠 수 없어서, 여유 없이 계속 MoveTowards하면
				// 벽/오브젝트에 영구히 밀려붙어 "벽 보고 제자리 이동"하는 것처럼 보이는 문제 방지
				if (Vector3.Distance(transform.position, targetNoisePosition) > noiseArrivalDistance)
					transform.position = ClampToWater(
						Vector3.MoveTowards(transform.position, targetNoisePosition, suspectSpeed * Time.deltaTime));

				LookAtTarget(targetNoisePosition);

				// 물가에 막혀 접근이 정체됐으면 — 사거리(noiseLeapRange) 밖이어도 그 자리에서 도약.
				// 소음 지점이 부두 안쪽이면 물가 최근접점에 붙은 채 영원히 바라보기만 하던 문제 수정
				if (waterLurkMode && !noiseLeapDone)
				{
					float moved = ((Vector2)transform.position - lastSuspectPos).magnitude;
					lastSuspectPos = transform.position;

					if (moved < suspectSpeed * Time.deltaTime * 0.2f) suspectStallTimer += Time.deltaTime;
					else suspectStallTimer = 0f;

					if (suspectStallTimer >= 0.6f)
					{
						Debug.Log("<color=orange>[FishMovement]</color> 물가에 막힘 — 사거리 밖이지만 소음 지점을 향해 도약");
						noiseLeapDone = true;
						dashCycleCoroutine = StartCoroutine(NoiseLeapRoutine());
					}
				}
				break;

			case BehaviorState.Chase:
				if (playerTransform == null) return;

				// 물속 모드: 추격 = 도약 사이클 (소형 괴성) — 물 밖으로 걸어나가는 추격은 없음.
				// 은신 중엔 새 사이클을 시작하지 않음 — 사이클 종료↔재시작 프레임 사이에 Gaze의
				// Patrol 강등보다 먼저 재시작되어 숨은 플레이어에게 도약을 반복하는 구멍 방지
				if (waterLurkMode)
				{
					if (!Concealment.IsPlayerConcealed)
						dashCycleCoroutine = StartCoroutine(LeapCycleRoutine(false));
					return;
				}

				// 1~2페이즈: 단순 추격 — 파훼는 은신처. 돌진 사이클은 광폭(3페이즈) 전용
				transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, chaseSpeed * Time.deltaTime);
				LookAtTarget(playerTransform.position);
				break;

			case BehaviorState.Berserk:
				if (playerTransform == null)
				{
					playerTransform = GameObject.FindWithTag("Player")?.transform;
					return;
				}
				if (Time.time < berserkEntryUntil) return;   // 진입 대형 괴성 연출 시간

				// 광폭 — 매 돌진/도약 대형 괴성 (미친 듯이 반복되는 게 의도).
				// 물속 모드에선 은신 중 재시작 금지 (Chase와 같은 이유 — 숨은 목표에 도약 스팸 방지.
				// 은신에서 나오는 순간 다시 시작되므로 광폭의 압박은 유지됨)
				if (waterLurkMode && Concealment.IsPlayerConcealed) return;

				dashCycleCoroutine = StartCoroutine(waterLurkMode
					? LeapCycleRoutine(true)
					: DashCycleRoutine(chargeWaitTime, dashSpeed, cooldownTime, true));
				break;
		}
	}

	/// <summary>
	/// 돌진 사이클 공통 루틴: 괴성(이 순간 플레이어 위치 고정) → 대기 → 고정 위치를 "관통해 지나가는" 돌진
	/// → 경직(제자리에서 좌우로 두리번) → 재돌진 판정.
	/// 상어가 먹이를 물고 그대로 헤엄쳐 지나가듯 고정 위치에서 멈추지 않고 dashOvershoot만큼 더 나아가고,
	/// 경직이 끝났는데 플레이어가 reDashRange 안에 남아 있으면(은신 제외) 접근 없이 즉시 다음 돌진으로 이어진다 —
	/// 경직은 휴식이 아니라 "지금 도망가지 않으면 또 온다"는 압박 구간.
	/// Chase(소형 괴성)/Berserk(대형 괴성)가 파라미터만 달리해 공유. 이동도 이 코루틴이 직접 수행한다.
	/// </summary>
	private IEnumerator DashCycleRoutine(float chargeWait, float dashSpd, float cooldown, bool bigScream)
	{
		while (true)
		{
			if (playerTransform == null) break;

			// 1) 괴성 + 위치 고정 — "지금 서 있으면 맞는다"는 텔레그래프
			isCharging = true;
			EventBus.RaiseMonsterScreamed(transform.position, bigScream);

			Vector3 lockedPos = playerTransform.position;
			Vector3 dashDir = lockedPos - transform.position;
			dashDir = dashDir.sqrMagnitude > 0.0001f ? dashDir.normalized : (Vector3)facingDir;
			dashTarget = lockedPos + dashDir * dashOvershoot;   // 고정 위치를 지나쳐 관통하는 지점까지
			LookAtTarget(dashTarget);
			Debug.Log($"<color=red>[FishMovement]</color> 괴성({(bigScream ? "대형" : "소형")})! 위치 고정 → {chargeWait}초 후 돌진");

			yield return new WaitForSeconds(chargeWait);

			// 2) 고정 위치로 관통 돌진 (실시간 추적 아님 — 회피가 성립하는 이유)
			isCharging = false;
			isDashing = true;

			float elapsed = 0f;
			while (elapsed < dashDuration && Vector3.Distance(transform.position, dashTarget) > 0.1f)
			{
				if (!isMovingActive) break;   // StopMovement 등 외부 정지 존중

				elapsed += Time.deltaTime;
				transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpd * Time.deltaTime);
				yield return null;
			}

			isDashing = false;
			if (!isMovingActive) break;

			// 3) 경직 — 제자리에서 좌우로 두리번거리며 주변을 살핌 (이동·상태 전환은 잠긴 상태)
			Debug.Log($"<color=yellow>[FishMovement]</color> 돌진 종료 → 경직 {cooldown}초 (주변 탐색)");
			float baseAngle = transform.eulerAngles.z;
			Vector2 baseFacing = facingDir;
			float t = 0f;
			while (t < cooldown)
			{
				if (!isMovingActive) break;

				t += Time.deltaTime;
				float wobble = Mathf.Sin(t * 3f) * staggerScanAngle;
				if (rotateBody)
					transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + wobble);
				else
					facingDir = Quaternion.Euler(0f, 0f, wobble) * baseFacing;   // 회전 대신 방향값을 흔듦 — 애니메이션이 두리번거림을 표현
				yield return null;
			}
			if (!isMovingActive) break;

			// 4) 재돌진 판정 — 경직이 끝났는데 아직 범위 안이면(은신 중이면 제외) 접근 없이 바로 다음 돌진
			bool stillClose = playerTransform != null
				&& !Concealment.IsPlayerConcealed
				&& Vector3.Distance(transform.position, playerTransform.position) <= reDashRange;
			if (!stillClose) break;

			Debug.Log("<color=red>[FishMovement]</color> 경직 종료 — 아직 범위 안! 재돌진!");
		}

		// 사이클 종료 — "물었나?" 확인하듯 플레이어 쪽으로 몸을 돌리고, 재발견/상태 판정은 Gaze에 넘김
		// (관통 돌진 후 등지고 있으면 Gaze 시야에 안 걸려 바로 순찰로 새는 것 방지)
		if (playerTransform != null) LookAtTarget(playerTransform.position);

		dashCycleCoroutine = null;
	}

	// ──────────────────────── 물속 매복 모드 (waterLurkMode) ────────────────────────

	/// <summary>
	/// 도약 사이클 — 돌진 사이클의 물 버전. 괴성(위치 고정) → 포물선 도약으로 관통 →
	/// 착수(물 밖이면 파닥이며 복귀) → 플레이어가 아직 범위 안이면(은신 제외) 재도약.
	/// Chase(소형 괴성)/Berserk(대형 괴성)가 파라미터만 달리해 공유.
	/// </summary>
	private IEnumerator LeapCycleRoutine(bool bigScream)
	{
		// try/finally — 사이클 도중 예외가 나거나 코루틴이 중단돼도 잠금(dashCycleCoroutine·플래그)이
		// 반드시 풀리게. 안 풀리면 SetState·소음 반응·이동이 전부 영구 정지(벽돌)됨.
		try
		{
			while (true)
			{
				if (playerTransform == null) break;
				if (Concealment.IsPlayerConcealed) break;   // 이미 숨은 목표에겐 도약을 시작하지 않음

				yield return LeapOnce(playerTransform.position, bigScream, overshoot: true);
				if (!isMovingActive) break;

				bool stillClose = playerTransform != null
					&& !Concealment.IsPlayerConcealed
					&& Vector3.Distance(transform.position, playerTransform.position) <= reDashRange;
				if (!stillClose) break;

				Debug.Log("<color=red>[FishMovement]</color> 착수 후에도 아직 범위 안! 재도약!");
			}

			// 은신으로 목표를 잃었으면 — 깊이 잠수해 자기 구역으로 물러나는 연출 (그냥 뒤돌아 헤엄치면 김샘)
			if (isMovingActive && Concealment.IsPlayerConcealed)
				yield return DiveAwayRoutine();

			// 사이클 종료 — 플레이어 쪽으로 몸을 돌리고 재발견 판정은 Gaze에 넘김 (돌진 사이클과 동일한 이유)
			if (playerTransform != null) LookAtTarget(playerTransform.position);
		}
		finally
		{
			dashCycleCoroutine = null;
			isCharging = false;
			isDashing = false;
		}
	}

	/// <summary>
	/// 잠수 이탈 — 은신당해 목표를 잃었을 때: 알파를 더 낮춰 수면 아래로 사라지듯 가라앉고,
	/// 잠수한 채 자기 순찰 구역 쪽으로 물러난 뒤 평소의 잠긴 모습으로 다시 떠오른다.
	/// dashCycleCoroutine 안에서 실행되므로 잠수 동안 외부 상태 전환이 잠겨 연출이 끊기지 않는다.
	/// </summary>
	private IEnumerator DiveAwayRoutine()
	{
		Debug.Log("<color=cyan>[FishMovement]</color> 목표 상실(은신) — 수면 아래로 잠수해 물러남");
		if (!splashInSfx.IsNull)
			SoundManager.Instance.PlayOneShot(splashInSfx, transform.position);

		// 가라앉음 — 거의 안 보이는 알파까지
		yield return FadeVisualAlpha(submergedAlpha, diveAlpha, 0.4f);

		// 잠수한 채 자기 구역으로 물러남 (순찰 속도 — 사냥이 끝난 포식자의 느긋함)
		Vector3 retreatTarget = ClampToWater(patrolCenter != null ? patrolCenter.position : patrolHome);
		float t = 0f;
		while (t < diveDuration)
		{
			if (!isMovingActive) yield break;
			t += Time.deltaTime;
			transform.position = Vector3.MoveTowards(transform.position, retreatTarget, patrolSpeed * Time.deltaTime);
			LookAtTarget(retreatTarget);
			yield return null;
		}

		// 다시 떠오름 — 평소의 잠긴 모습으로
		yield return FadeVisualAlpha(diveAlpha, submergedAlpha, 0.6f);
	}

	private IEnumerator FadeVisualAlpha(float from, float to, float duration)
	{
		float t = 0f;
		while (t < duration)
		{
			t += Time.deltaTime;
			SetVisualAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
			yield return null;
		}
		SetVisualAlpha(to);
	}

	/// <summary>소음 지점을 향한 단발 도약 — 이후 상태 복귀는 SuspectTimerRoutine이 처리</summary>
	private IEnumerator NoiseLeapRoutine()
	{
		try
		{
			yield return LeapOnce(targetNoisePosition, bigScream: true, overshoot: false);
			if (isMovingActive) LookAtTarget(targetNoisePosition);
		}
		finally
		{
			// 예외가 나도 잠금 해제 — LeapCycleRoutine과 같은 이유
			dashCycleCoroutine = null;
			isCharging = false;
			isDashing = false;
		}
	}

	/// <summary>
	/// 도약 1회: 괴성 + 목표 고정 → 대기 → 포물선 비행 → 착수 → 회복(물 복귀 + 몸부림).
	/// 루트는 from→landing 지면 직선을 그대로 이동하므로 고정 위치를 지나는 순간
	/// ThreatContact 접촉 판정이 자연히 성립한다. 높이는 자식 스프라이트의 월드 Y 오프셋으로만 표현.
	/// </summary>
	private IEnumerator LeapOnce(Vector3 lockedPos, bool bigScream, bool overshoot)
	{
		// 1) 괴성 + 위치 고정 — "지금 서 있으면 맞는다"는 텔레그래프 (외부 전환·소음 잠금 시작)
		isCharging = true;
		EventBus.RaiseMonsterScreamed(transform.position, bigScream);
		lockedPos.z = transform.position.z;

		Vector3 dir = lockedPos - transform.position;
		dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : (Vector3)facingDir;

		// 착수 지점: 고정 위치를 관통(t > 0)하면서 "물 안"인 지점을 도약 방향 선상에서 탐색 —
		// 다리/부두 위에 떨어지지 않고 반드시 바다로 들어간다. 방향 선상에 물이 없을 때만
		// 예전처럼 지나쳐 떨어진 뒤 파닥이며 복귀하는 폴백이 동작.
		Vector3 landing = FindWaterLanding(lockedPos, dir, overshoot ? leapOvershoot : 0.75f);
		landing.z = transform.position.z;
		LookAtTarget(landing);
		Debug.Log($"<color=red>[FishMovement]</color> 물속 괴성({(bigScream ? "대형" : "소형")})! 위치 고정 → {chargeWaitTime}초 후 도약");

		yield return new WaitForSeconds(chargeWaitTime);
		isCharging = false;
		if (!isMovingActive) yield break;

		// 2) 포물선 비행 — 물을 가르고 솟구쳐 고정 위치를 관통해 지나간다
		isDashing = true;
		SetSubmergedVisual(false);
		if (!splashOutSfx.IsNull)
			SoundManager.Instance.PlayOneShot(splashOutSfx, transform.position);

		Vector3 from = transform.position;
		float dist = Vector2.Distance(from, landing);
		float duration = Mathf.Clamp(dist / leapSpeed, leapMinDuration, leapMaxDuration);
		float arc = Mathf.Min(leapArcHeight, dist * 0.35f + 0.5f);

		CreateLeapShadow();
		Vector3 shadowBaseScale = leapShadow != null ? leapShadow.localScale : Vector3.one;

		float t = 0f;
		while (t < duration)
		{
			if (!isMovingActive) break;
			t += Time.deltaTime;
			float p = Mathf.Clamp01(t / duration);
			float h = Mathf.Sin(p * Mathf.PI);   // 0→1→0 높이 곡선

			transform.position = Vector3.Lerp(from, landing, p);

			// 회전(진행 방향)과 무관하게 화면 위쪽으로 떠야 하므로 로컬이 아닌 월드 오프셋
			if (leapVisual != null)
			{
				leapVisual.localPosition = leapVisualBaseLocal;
				leapVisual.position += Vector3.up * (h * arc);
			}

			if (leapShadow != null)
			{
				leapShadow.position = transform.position;                               // 그림자는 지면 경로를 따라감
				leapShadow.localScale = shadowBaseScale * Mathf.Lerp(1f, 0.55f, h);     // 높이 오르면 축소 — 높이감의 핵심
			}

			yield return null;
		}

		if (leapVisual != null) leapVisual.localPosition = leapVisualBaseLocal;
		if (leapShadow != null) { Destroy(leapShadow.gameObject); leapShadow = null; }
		isDashing = false;
		if (!isMovingActive) yield break;

		// 3) 착수 + 회복 — 물 밖(부두 위 등)에 떨어졌으면 파닥이며 가장 가까운 물로 미끄러져 복귀.
		//    회복 시간은 경직(cooldownTime)을 그대로 사용 — "지금 도망가지 않으면 또 온다"는 압박 구간
		if (!splashInSfx.IsNull)
			SoundManager.Instance.PlayOneShot(splashInSfx, transform.position);
		SetSubmergedVisual(true);

		Vector3 waterPoint = ClampToWater(transform.position);
		float baseAngle = transform.eulerAngles.z;
		Vector2 baseFacing = facingDir;
		float rt = 0f;
		while (rt < cooldownTime)
		{
			if (!isMovingActive) yield break;
			rt += Time.deltaTime;

			if ((transform.position - waterPoint).sqrMagnitude > 0.01f)
				transform.position = Vector3.MoveTowards(transform.position, waterPoint, waterReturnSpeed * Time.deltaTime);

			// 물속 몸부림 — 경직 두리번의 물 버전 (작게 파닥임)
			float squirm = Mathf.Sin(rt * 4f) * 12f;
			if (rotateBody)
				transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + squirm);
			else
				facingDir = Quaternion.Euler(0f, 0f, squirm) * baseFacing;
			yield return null;
		}
	}

	/// <summary>도약용 즉석 그림자 — 스프라이트를 복제해 검게 누름 (Throwable과 동일 기법, 별도 에셋 불필요)</summary>
	private void CreateLeapShadow()
	{
		if (leapShadow != null) { Destroy(leapShadow.gameObject); leapShadow = null; }

		SpriteRenderer src = leapVisual != null ? leapVisual.GetComponentInChildren<SpriteRenderer>() : null;
		if (src == null && bodySprites != null && bodySprites.Length > 0) src = bodySprites[0];
		if (src == null) return;

		var go = new GameObject("LeapShadow");
		var sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = src.sprite;
		sr.color = new Color(0f, 0f, 0f, 0.35f);
		sr.sortingLayerID = src.sortingLayerID;
		sr.sortingOrder = src.sortingOrder - 1;

		Vector3 s = src.transform.lossyScale * 0.9f;
		s.y *= 0.6f;   // 납작하게 눌러 타원 그림자
		go.transform.localScale = s;
		leapShadow = go.transform;
	}

	/// <summary>물속/물 밖 시각 전환 — 물속에선 반투명하게 가라앉아 보이게</summary>
	private void SetSubmergedVisual(bool submerged) => SetVisualAlpha(submerged ? submergedAlpha : 1f);

	private void SetVisualAlpha(float mul)
	{
		if (!waterLurkMode || bodySprites == null) return;
		for (int i = 0; i < bodySprites.Length; i++)
		{
			if (bodySprites[i] == null) continue;
			Color c = bodySprites[i].color;
			c.a = baseAlphas[i] * mul;
			bodySprites[i].color = c;
		}
	}

	/// <summary>
	/// 이 지점이 "열린 물"인가 — 물 영역 안이면서 다리·부두(landingBlockedMask) 위가 아닌 곳.
	/// 바다 콜라이더가 다리 밑까지 깔려 있어도 다리 위를 물로 오판하지 않게 하는 핵심 검사.
	/// </summary>
	private bool IsInWater(Vector2 p)
	{
		if (waterZones == null) return false;
		if (landingBlockedMask.value != 0 && Physics2D.OverlapPoint(p, landingBlockedMask) != null) return false;

		foreach (var zone in waterZones)
			if (zone != null && zone.OverlapPoint(p)) return true;
		return false;
	}

	/// <summary>
	/// 착수 지점 탐색 — 도약 방향 선상에서 고정 위치 너머(t ≥ 0.4) 지점들을 훑어,
	/// 물 안이면서 희망 관통 거리(desiredOvershoot)에 가장 가까운 곳을 고른다.
	/// 반드시 고정 위치를 지나친 뒤 떨어지므로 비행 경로가 플레이어 지점을 통과하는 것(접촉 판정)은 유지된다.
	/// 방향 선상에 물이 아예 없으면(막다른 부두 안쪽 등) 희망 거리 그대로 착지 — 이후 회복 단계가 물로 복귀시킴.
	/// </summary>
	private Vector3 FindWaterLanding(Vector3 lockedPos, Vector3 dir, float desiredOvershoot)
	{
		if (waterZones == null || waterZones.Length == 0)
			return lockedPos + dir * desiredOvershoot;

		const float minBeyond = 0.4f;   // 최소 관통 거리 — 고정 지점을 확실히 지나치게
		const float step = 0.25f;
		float maxScan = Mathf.Max(desiredOvershoot * 3f, desiredOvershoot + 4f);

		float bestT = -1f;
		float bestScore = float.MaxValue;
		for (float t = minBeyond; t <= maxScan; t += step)
		{
			Vector2 p = lockedPos + dir * t;
			if (!IsInWater(p)) continue;

			float score = Mathf.Abs(t - desiredOvershoot);   // 희망 관통 거리에 가까울수록 좋음
			if (score < bestScore) { bestScore = score; bestT = t; }
		}

		if (bestT > 0f) return lockedPos + dir * bestT;
		return lockedPos + dir * desiredOvershoot;   // 물이 없는 방향 — 착지 후 복귀 폴백이 처리
	}

	/// <summary>지점을 물 영역 안으로 강제 — 이미 물 안이면 그대로, 밖이면 가장 가까운 물가 지점</summary>
	private Vector3 ClampToWater(Vector3 pos)
	{
		if (!waterLurkMode || waterZones == null || waterZones.Length == 0) return pos;

		Vector2 p = pos;
		foreach (var zone in waterZones)
			if (zone != null && zone.OverlapPoint(p)) return pos;

		Vector2 best = p;
		float bestSqr = float.MaxValue;
		foreach (var zone in waterZones)
		{
			if (zone == null) continue;
			Vector2 c = zone.ClosestPoint(p);
			float d = (c - p).sqrMagnitude;
			if (d < bestSqr) { bestSqr = d; best = c; }
		}
		return new Vector3(best.x, best.y, pos.z);
	}

	// GameObject 비활성화 시 코루틴은 조용히 죽지만 참조·플래그는 남음 —
	// 정리하지 않으면 재활성화 후 dashCycleCoroutine != null 로 영구 정지(이동 불능)됨
	private void OnDisable()
	{
		dashCycleCoroutine = null;
		isCharging = false;
		isDashing = false;
		isNoiseWindup = false;

		// 도약 중 강제 종료 시 잔여물 정리 — 그림자·스프라이트 높이 오프셋·반투명 상태
		if (leapShadow != null) { Destroy(leapShadow.gameObject); leapShadow = null; }
		if (leapVisual != null) leapVisual.localPosition = leapVisualBaseLocal;
		SetSubmergedVisual(true);
	}

	/// <summary>
	/// 반경 순찰 — 중심점(patrolCenter, 없으면 시작 위치) 기준 patrolRadius 안의 무작위 지점을 뽑아
	/// 헤엄쳐 가고, 도착하면 잠깐 머문 뒤 다음 지점을 뽑는다. 추격으로 멀리 벗어났다가 순찰로
	/// 복귀하면 자연히 자기 구역으로 돌아온다 (다음 지점이 항상 중심 반경 안에서 뽑히므로).
	/// </summary>
	private void ExecuteRadiusPatrol()
	{
		if (Time.time < patrolIdleUntil) return;   // 지점 도착 후 잠깐 머묾

		if (!hasPatrolTarget)
		{
			patrolTarget = PickPatrolPoint();
			hasPatrolTarget = true;
		}

		transform.position = Vector3.MoveTowards(transform.position, patrolTarget, patrolSpeed * Time.deltaTime);
		LookAtTarget(patrolTarget);

		if (Vector3.Distance(transform.position, patrolTarget) <= patrolArrivalDistance)
		{
			hasPatrolTarget = false;
			patrolIdleUntil = Time.time + Random.Range(patrolIdleMin, patrolIdleMax);
		}
	}

	private Vector3 PickPatrolPoint()
	{
		Vector3 center = patrolCenter != null ? patrolCenter.position : patrolHome;
		Vector2 offset = Random.insideUnitCircle * patrolRadius;
		Vector3 p = new Vector3(center.x + offset.x, center.y + offset.y, transform.position.z);
		return ClampToWater(p);   // 물속 모드: 반경이 물가를 벗어나도 지점이 물 안으로 스냅됨
	}

	public void SetState(BehaviorState newState)
	{
		if (currentState == BehaviorState.Berserk) return;
		// 사이클 전체(경직 포함) 동안 외부 전환 잠금 — 경직 중 Gaze가 "놓쳤다"고 Patrol로 돌려서
		// 걸어 나가는 것 방지. Gaze는 매 프레임 재시도하므로 사이클이 끝나면 올바른 상태로 수렴함.
		if (dashCycleCoroutine != null) return;

		if (newState == BehaviorState.Patrol)
		{
			hasPatrolTarget = false;   // 순찰 복귀 시 현 위치 기준으로 새 지점을 뽑게 (반경 밖에 있으면 자기 구역으로 귀환)
		}

		ApplyState(newState);
	}

	public void HandleNoise(Vector2 position, float radius)
	{
		if (currentState == BehaviorState.Berserk)
		{
			Debug.Log("<color=grey>[FishMovement]</color> 소음 무시 — 광폭 중 (의도된 동작)");
			return;
		}
		if (IsDashLocked)
		{
			Debug.Log("<color=grey>[FishMovement]</color> 소음 무시 — 돌진 판정 중");
			return;
		}

		float distanceToNoise = Vector2.Distance((Vector2)transform.position, position);
		if (distanceToNoise > radius)
		{
			Debug.Log($"<color=grey>[FishMovement]</color> 소음 무시 — 반경 밖 (거리 {distanceToNoise:F1} > 반경 {radius})");
			return;
		}

		targetNoisePosition = new Vector3(position.x, position.y, transform.position.z);

		if (Mathf.Approximately(position.x, 0f) && Mathf.Approximately(position.y, 0f))
		{
			Generator[] generators = FindObjectsByType<Generator>(FindObjectsSortMode.None);
			foreach (Generator gen in generators)
			{
				if (gen != null && !gen.IsSatisfied)
				{
					targetNoisePosition = new Vector3(gen.transform.position.x, gen.transform.position.y, transform.position.z);
					break;
				}
			}
		}

		// 먼 소음(발전기 가동 등)에 처음 반응하는 순간 — 대형 괴성 + 윈드업 후 달려감.
		// 이미 Suspect로 이동 중일 땐 괴성·윈드업 없이 목표만 갱신 (연속 소음에 괴성 연발 방지)
		bool newEntry = currentState != BehaviorState.Suspect;
		if (newEntry)
			EventBus.RaiseMonsterScreamed(transform.position, true);

		// 이전 윈드업 루틴이 도중에 교체될 때 isNoiseWindup=true가 남아
		// Suspect 이동이 영구 정지되는 누수 방지 — 새 루틴이 필요하면 다시 켠다
		isNoiseWindup = false;
		noiseLeapDone = false;   // 새 소음엔 새 도약 허용 (물속 모드)
		suspectStallTimer = 0f;
		lastSuspectPos = transform.position;

		if (suspectTimerCoroutine != null) StopCoroutine(suspectTimerCoroutine);
		suspectTimerCoroutine = StartCoroutine(SuspectTimerRoutine(newEntry));

		Debug.Log($"<color=orange>[FishMovement]</color> 소음 반응! → {targetNoisePosition} (신규 진입: {newEntry})");
	}

	private IEnumerator SuspectTimerRoutine(bool withWindup)
	{
		ApplyState(BehaviorState.Suspect);

		// 괴성 윈드업 — 소음 쪽으로 몸을 돌리고 울부짖는 동안 정지, 그 후 내달림
		if (withWindup)
		{
			isNoiseWindup = true;
			LookAtTarget(targetNoisePosition);
			yield return new WaitForSeconds(noiseScreamWindup);
			isNoiseWindup = false;
		}

		yield return new WaitForSeconds(10f);

		// 소음 도약(물속 모드)이 진행 중이면 사이클이 끝날 때까지 복귀를 미룸 —
		// 도약 코루틴과 Patrol 이동 코드가 transform을 동시에 잡는 것 방지
		yield return new WaitUntil(() => dashCycleCoroutine == null);

		ApplyState(BehaviorState.Patrol);
	}

	public void ForceState(BehaviorState newState, float duration)
	{
		ApplyState(newState);
		if (newState == BehaviorState.Berserk)
		{
			if (berserkTimerCoroutine != null) StopCoroutine(berserkTimerCoroutine);
			berserkTimerCoroutine = StartCoroutine(BerserkTimerRoutine(duration));
		}
	}

	private IEnumerator BerserkTimerRoutine(float duration)
	{
		yield return new WaitForSeconds(duration);
		if (currentState == BehaviorState.Berserk && controller != null)
		{
			controller.Fail(FailReason.Caught);
		}
	}

	public void ActivateMovement()
	{
		isMovingActive = true;
		if (currentState != BehaviorState.Berserk)
		{
			ApplyState(BehaviorState.Patrol);
			hasPatrolTarget = false;
		}
	}

	public void StopMovement()
	{
		isMovingActive = false;
		if (berserkTimerCoroutine != null) StopCoroutine(berserkTimerCoroutine);
		if (suspectTimerCoroutine != null) StopCoroutine(suspectTimerCoroutine);

		// 돌진 사이클도 완전히 정리 — 플래그가 남으면 재가동 시 상태 잠금이 풀리지 않음
		if (dashCycleCoroutine != null)
		{
			StopCoroutine(dashCycleCoroutine);
			dashCycleCoroutine = null;
		}
		isCharging = false;
		isDashing = false;
		isNoiseWindup = false;

		// 도약 중 정지 시 잔여물 정리 (그림자·높이 오프셋·반투명)
		if (leapShadow != null) { Destroy(leapShadow.gameObject); leapShadow = null; }
		if (leapVisual != null) leapVisual.localPosition = leapVisualBaseLocal;
		SetSubmergedVisual(true);

		RaiseThreatLevel(0);   // 위협 비활성화 — 추격 BGM 페이드아웃
	}

	private void LookAtTarget(Vector3 targetPos)
	{
		Vector2 dir = (targetPos - transform.position).normalized;
		if (dir.sqrMagnitude > 0.0001f) facingDir = dir;   // 방향값은 항상 갱신 — FishAnimator가 이걸 읽음

		// 방향별 스프라이트 모드에선 오브젝트를 돌리지 않음 — 콜라이더·자식 스프라이트가 같이 도는 것 방지
		if (!rotateBody) return;

		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
		transform.rotation = Quaternion.Euler(0, 0, angle);
	}
}
