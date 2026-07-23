using UnityEngine;

/// <summary>
/// 은신처. 두 가지 모드:
///   · Interact To Hide 켜짐 (기본) — 범위 안에서 E로 숨고, 숨은 직후 exitDelay 동안은 못 나가며,
///     그 후 다시 E로 나온다. 숨은 동안 이동 잠금 (옷장에 숨는 클래식 호러 방식).
///   · 꺼짐 — 기존처럼 걸어 들어가면 자동 은신, 나오면 해제 (수풀 통과형 존용).
///
/// 몬스터 시야(Gaze)·부름 판정(HideAndSeekCall) 등은 전부 static IsPlayerConcealed를 읽으므로
/// 모드와 무관하게 그대로 동작한다.
///
/// E-모드 세팅 주의: 이 오브젝트의 레이어가 PlayerInteractor의 Interactable Mask에 포함되어야
/// 프롬프트/상호작용이 뜬다 (Pickup과 같은 규칙).
/// </summary>
public class Concealment : MonoBehaviour, IInteractable
{
	static int insideCount;            // walk-in 모드 존들의 진입 카운트
	static Concealment hiddenZone;     // E-모드로 현재 숨어 있는 존 (한 번에 하나)

	public static bool IsPlayerConcealed => insideCount > 0 || hiddenZone != null;

	public bool PlayerInside { get; private set; }

	[Header("E키로 숨기/나오기 (끄면 기존처럼 걸어 들어가면 자동 은신)")]
	[SerializeField] private bool interactToHide		= true;
	[SerializeField] private float exitDelay				= 0.7f;          // 숨은 직후 이 시간 동안은 나가기 비활성
	[SerializeField] private string hidePrompt		= "숨는다";
	[SerializeField] private string exitPrompt			= "나온다";

	[Header("숨을 때 캐릭터가 고정될 지점 (비우면 이 오브젝트의 콜라이더 중앙)")]
	[SerializeField] private Transform hideAnchor;

	private float exitUnlockTime;
	private PlayerMovement playerMovement;
	private Vector3 exitPosition;   // 숨기 직전 위치 — 나올 때 여기로 복귀 (박스 안에 끼는 것 방지)

	public string InteractKey => "E";

	public string Prompt
	{
		get
		{
			if (!interactToHide) return "";                    // walk-in 존은 프롬프트 없음

			if (hiddenZone == this)
				return Time.time >= exitUnlockTime ? exitPrompt : "";   // 딜레이 중엔 프롬프트도 숨김

			return hiddenZone == null ? hidePrompt : "";       // 다른 곳에 숨어있는 중엔 표시 안 함
		}
	}

	private void Start()
	{
		playerMovement = FindAnyObjectByType<PlayerMovement>();
	}

	public void Interact()
	{
		if (!interactToHide) return;

		if (hiddenZone == this)
		{
			if (Time.time < exitUnlockTime) return; 
			ExitHiding();
		}
		else if (hiddenZone == null)
		{
			EnterHiding();
		}
	}


	private SpriteRenderer sr;

	private void EnterHiding()
	{
		hiddenZone = this;
		exitUnlockTime = Time.time + exitDelay;
		SetHiddenLock(true);

		// 캐릭터를 은신 지점 중앙으로 스냅 — 박스 "뒤에 들어간" 그림
		if (playerMovement != null)
		{
			exitPosition = playerMovement.transform.position;

			Vector3 anchor = hideAnchor != null
				? hideAnchor.position
				: (TryGetComponent(out Collider2D zone) ? (Vector3)zone.bounds.center : transform.position);
			anchor.z = playerMovement.transform.position.z;

			var rb = playerMovement.GetComponent<Rigidbody2D>();
			if (rb != null)
			{
				rb.position		= anchor;
				rb.bodyType	= RigidbodyType2D.Static;
			}
			playerMovement.transform.position = anchor;

			sr				= playerMovement.GetComponent<SpriteRenderer>();
			sr.enabled	= false;
		}

		Debug.Log($"<color=cyan>[Concealment]</color> 은신 — {exitDelay}초 후 나가기 가능");
	}

	private void ExitHiding()
	{
		if (hiddenZone == this) hiddenZone = null;
		SetHiddenLock(false);

		// 숨기 직전 위치로 복귀 — 박스 콜라이더 안에 끼인 채 해제되는 것 방지
		if (playerMovement != null)
		{
			var rb = playerMovement.GetComponent<Rigidbody2D>();
			if (rb != null)
			{
				rb.position		= exitPosition;
                rb.bodyType	= RigidbodyType2D.Dynamic;
            }
			playerMovement.transform.position	= exitPosition;
			sr.enabled									= true;
		}

		Debug.Log("<color=cyan>[Concealment]</color> 은신 해제");
	}

	private void SetHiddenLock(bool locked)
	{
		if (playerMovement == null)
		{
			GameObject player = GameObject.FindWithTag("Player");
			if (player != null) playerMovement = player.GetComponent<PlayerMovement>();
			if (playerMovement == null) return;
		}
		playerMovement.HiddenLock = locked;
	}

	// 숨어 있는 채로 존이 꺼지는 경우(페이즈 정리 등) 잠금·상태가 남지 않게 정리
	private void OnDisable()
	{
		if (hiddenZone == this) ExitHiding();

		if (!interactToHide && PlayerInside)
		{
			PlayerInside = false;
			insideCount--;
		}
	}

	// ===== walk-in 모드 (interactToHide 꺼짐) =====
	void OnTriggerEnter2D(Collider2D other)
	{
		if (interactToHide) return;
		if (!other.CompareTag("Player")) return;
		PlayerInside = true;
		insideCount++;
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (interactToHide) return;
		if (!other.CompareTag("Player")) return;
		PlayerInside = false;
		insideCount--;
	}
}
