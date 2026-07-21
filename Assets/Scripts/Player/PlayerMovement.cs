using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
	[Header("캐릭터 움직임")]
	[SerializeField] private float moveSpeed							= 5f;
	[SerializeField] private float contaminationSpeedMultiplier	= 0.7f; // ������ ����

	[Header("뒷걸음질 감속")]
	[SerializeField] private float backpedalMultiplier = 0.65f;	// 조준 반대 방향 이동 시 최대 감속 배율
	[SerializeField] private Camera cam;								// 마우스 방향 계산용 (비우면 감속 비활성)

	private Rigidbody2D rb;
	private Vector2 input;
	private float speedMultiplier = 1f;
	private float backpedalFactor = 1f;

	/// <summary>
	/// 지형 이동 배율 (1 = 정상). 갯벌 진흙 위 감속 등 — MudTerrainSlow가 구동.
	/// 오염 감속(speedMultiplier)과 별도 채널이라 서로 덮어쓰지 않고 곱으로 중첩됨.
	/// </summary>
	public float TerrainMultiplier { get; set; } = 1f;

	/// <summary>
	/// 이동 잠금 — 선택지 창 등 "손을 뗄 수 없는 순간"에 DialogueUI가 걸어줌.
	/// 입력을 0으로 만들므로 IsMoving도 false가 되어 발소리·정적 감지 등에 안전.
	/// </summary>
	public bool MovementLocked { get; set; }

	/// <summary>
	/// 은신 잠금 — 은신처에 숨어 있는 동안 Concealment가 걸어줌.
	/// MovementLocked(선택지)와 별도 채널: 숨은 채로 선택지가 떴다 닫혀도 은신 잠금이 풀리지 않게.
	/// </summary>
	public bool HiddenLock { get; set; }

	// ===== 병합 시 유실 복구 =====
	public bool IsMoving		=> input.sqrMagnitude > 0.0001f;	// PlayerAnimator가 Idle/Walk 판정에 사용
	public Vector2 MoveInput	=> input;								// PlayerAnimator가 방향 판정에 사용

    private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
	}
	private void OnEnable()	=> EventBus.OnContaminationStageChanged		+= HandleStage;
    private void OnDisable()	=> EventBus.OnContaminationStageChanged		-= HandleStage;
    /// <summary>
    /// ������ �ܰ� ��ȭ��
    /// </summary>
    /// <param name="stage">���� ������ �ܰ�</param>
    private void HandleStage(int stage) => speedMultiplier = stage >= 3 ? contaminationSpeedMultiplier : 1f;

    private void Update()
	{
		if (MovementLocked || HiddenLock)
		{
			input = Vector2.zero;   // IsMoving도 자연히 false — 발소리·정적 감지에 안 걸림
			return;
		}

		input.x = Input.GetAxisRaw("Horizontal");
		input.y = Input.GetAxisRaw("Vertical");
		input = input.normalized;

		UpdateBackpedalFactor();
	}

	/// <summary>
	/// 조준(마우스) 방향과 이동 방향의 내적 기반 감속 계산.
	/// 정면/옆 이동 = 1, 완전 후진 = backpedalMultiplier, 그 사이는 각도 비례.
	/// "위협을 보며 천천히 물러날 것인가, 등 돌리고 전력으로 도망칠 것인가"의 딜레마 형성용.
	/// </summary>
	private void UpdateBackpedalFactor()
	{
		backpedalFactor = 1f;
		if (cam == null || input.sqrMagnitude < 0.0001f) return;

		Vector2 toMouse = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) - rb.position;
		if (toMouse.sqrMagnitude < 0.0001f) return;

		float dot = Vector2.Dot(input, toMouse.normalized);				// 1=정면, 0=옆, -1=완전 후진
		backpedalFactor = Mathf.Lerp(1f, backpedalMultiplier, Mathf.Clamp01(-dot));
	}

    private void FixedUpdate()
	{
		rb.MovePosition(rb.position + input * (moveSpeed * speedMultiplier * backpedalFactor * TerrainMultiplier) * Time.fixedDeltaTime);
	}
}
