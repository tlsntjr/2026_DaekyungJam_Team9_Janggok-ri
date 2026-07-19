using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
	[SerializeField] private Animator animator;
	[SerializeField] private SpriteRenderer body;
	[SerializeField] private PlayerMovement movement;
	[SerializeField] private Camera cam;

    private static readonly int MoveDirHash = Animator.StringToHash("MoveDir");

    // Animator Controller의 상태 이름과 정확히 일치해야 함
    private static readonly string[] IdleStates				= { "Idle_Front",				"Idle_Side",					"Idle_Back" };
	private static readonly string[] WalkStates				= { "Walk_Front",				"Walk_Side",					"Walk_Back" };
	private static readonly string[] PollutedIdleStates		= { "Polluted_Idle_Front",	"Polluted_Idle_Side",		"Polluted_Idle_Back" };
	private static readonly string[] PollutedWalkStates	= { "Polluted_Walk_Front",	"Polluted_Walk_Side",		"Polluted_Walk_Back" };
	private string currentState;

	private void Reset() => cam = Camera.main;

	private void Update()
	{
		Vector2 toMouse = cam.ScreenToWorldPoint(Input.mousePosition) - transform.position;

        Vector2 move = movement.MoveInput;
        float playDir = 1f;

        if (move.sqrMagnitude > 0.0001f)
        {
            float dot = Vector2.Dot(move.normalized, toMouse.normalized);
            if (dot < -0.2f) playDir = -1f;
        }

        animator.SetFloat(MoveDirHash, playDir);

        // 방향 판정
        bool isSide	= Mathf.Abs(toMouse.x) > Mathf.Abs(toMouse.y);
		int dir			= isSide ? 1 : (toMouse.y > 0f ? 2 : 0);
		body.flipX	= !(isSide && toMouse.x < 0f);

        // Update() 안의 상태 선택 부분만 교체
        bool infected	= ContaminationSystem.Instance.Stage >= 3;
		string[] set		= movement.IsMoving
								?	(infected ? PollutedWalkStates : WalkStates)
								:	(infected ? PollutedIdleStates	: IdleStates);

        string next = set[dir];

        if (next != currentState)
		{
			currentState = next;
			animator.Play(next);
		}
	}
}