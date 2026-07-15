using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
	[Header("상호작용 범위 및 세팅")]
	[SerializeField] private float interactRadius		= 1.2f;
	[SerializeField] private KeyCode interactKey		= KeyCode.E;
	[SerializeField] private LayerMask interactableMask;

	public string CurrentPrompt						{ get; private set; }
	public Transform CurrentTargetTransform		{ get; private set; }

	private IInteractable current;

	private void Update()
	{
		current						= FindNearest(out Transform targetTransform);
		CurrentPrompt				= current?.Prompt;
		CurrentTargetTransform	= targetTransform;

		if (current != null && Input.GetKeyDown(interactKey))
			current.Interact();
	}

	/// <summary>
	/// 근처 상호작용 가능한 오브젝트 탐색
	/// 가장 가까운 것부터
	/// </summary>
	/// <returns></returns>
	private IInteractable FindNearest(out Transform targetTransform)
	{
		var hits							= Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableMask);
		IInteractable best				= null;
		Transform bestTransform	= null;
		float bestDist					= float.MaxValue;

		foreach (var hit in hits)
		{
			var interactable = hit.GetComponent<IInteractable>();
			if (interactable == null) continue;

			float d = Vector2.Distance(transform.position, hit.transform.position);
			if (d < bestDist) { bestDist = d; best = interactable; bestTransform = hit.transform; }
		}

		targetTransform = bestTransform;
		return best;
	}
}