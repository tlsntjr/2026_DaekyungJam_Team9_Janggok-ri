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
		rb.MovePosition(rb.position + input * (moveSpeed * speedMultiplier * backpedalFactor) * Time.fixedDeltaTime);
	}
}
