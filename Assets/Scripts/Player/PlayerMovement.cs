using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
	[Header("기본 플레이어 세팅")]
	[SerializeField] private float moveSpeed							= 5f;
	[SerializeField] private float contaminationSpeedMultiplier	= 0.7f; // 오염도 감속

	private Rigidbody2D rb;
	private Vector2 input;
	private float speedMultiplier = 1f;

    private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
	}
	private void OnEnable()	=> EventBus.OnContaminationStageChanged		+= HandleStage;
    private void OnDisable()	=> EventBus.OnContaminationStageChanged		-= HandleStage;
    /// <summary>
    /// 오염도 단계 변화시
    /// </summary>
    /// <param name="stage">현재 오염도 단계</param>
    private void HandleStage(int stage) => speedMultiplier = stage >= 3 ? contaminationSpeedMultiplier : 1f;

    private void Update()
	{
		input.x = Input.GetAxisRaw("Horizontal");
		input.y = Input.GetAxisRaw("Vertical"); 
		input = input.normalized;                 
	}

    private void FixedUpdate()
	{
		rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
	}
}
