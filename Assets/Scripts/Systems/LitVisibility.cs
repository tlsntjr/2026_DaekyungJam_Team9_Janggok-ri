using UnityEngine;

public class LitVisibility : MonoBehaviour
{
	[Header("플레이어 손전등")]
	[SerializeField] private Transform visionCone;
	[SerializeField] private float coneAngle	= 45f;			// Light2D의 Outer Spot Angle
	[SerializeField] private float coneRange	= 7f;			// Light2D의 Outer Radius

	[Header("발밑 보조광")]
	[SerializeField] private Transform playerAura;
	[SerializeField] private float auraRange = 2f;

	[Header("벽 레이어")]
	[SerializeField] private LayerMask wallMask;

	[Header("렌더링")]
	[SerializeField] private SpriteRenderer[] renderers;		// 몬스터 본체+부속 스프라이트들
	[SerializeField] private float fadeSpeed = 8f;          // 뚝 끊기지 않게 알파 페이드

    private void Start()
	{
		SetAlpha(IsLit() ? 1f : 0f);
	}

    private void Update()
	{
		float target = IsLit() ? 1f : 0f;

		foreach (var r in renderers)
		{
			if (r == null) continue;
			Color c = r.color;
			c.a = Mathf.MoveTowards(c.a, target, fadeSpeed * Time.deltaTime);
			r.color = c;
		}
	}

    private bool IsLit() => InsideCone() || InsideAura();

	/// <summary>
	/// 플레이어 시야 (visionCone) 내부에 들어와있는지?
	/// </summary>
	/// <returns></returns>
    private bool InsideCone()
	{
		if (visionCone == null) return false;

		Vector2 to = transform.position - visionCone.position;
		float dist = to.magnitude;
		if (dist > coneRange) return false;
		if (Vector2.Angle(visionCone.up, to) > coneAngle * 0.5f) return false;   // up = 손전등 조준 방향
		if (Physics2D.Raycast(visionCone.position, to.normalized, dist, wallMask)) return false;
		return true;
	}

	/// <summary>
	/// 플레이어의 기본 반경 내에 들어와있는지?
	/// </summary>
	/// <returns></returns>
    private bool InsideAura()
	{
		if (playerAura == null) return false;

		Vector2 to = transform.position - playerAura.position;
		float dist = to.magnitude;
		if (dist > auraRange) return false;
		if (Physics2D.Raycast(playerAura.position, to.normalized, dist, wallMask)) return false;
		return true;
	}

	/// <summary>
	/// 결과에 따른 몬스터 알파
	/// </summary>
	/// <param name="a"></param>
    private void SetAlpha(float a)
	{
		foreach (var r in renderers)
		{
			if (r == null) continue;
			Color c = r.color;
			c.a = a;
			r.color = c;
		}
	}
}
