using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LitVisibility : MonoBehaviour
{
	[Header("�÷��̾� ������")]
	[SerializeField] private Transform visionCone;
	[SerializeField] private Light2D coneLight;					// 손전등 Light2D — 글리치/암전 시 실제로 꺼졌는지 판정용
	[SerializeField] private float litIntensityThreshold = 0.05f;	// 이 미만이면 "꺼짐"으로 간주
	[SerializeField] private float coneAngle	= 45f;			// Light2D�� Outer Spot Angle
	[SerializeField] private float coneRange	= 7f;			// Light2D�� Outer Radius

	[Header("�߹� ������")]
	[SerializeField] private Transform playerAura;
	[SerializeField] private float auraRange = 2f;

	[Header("�� ���̾�")]
	[SerializeField] private LayerMask wallMask;

	[Header("������")]
	[SerializeField] private SpriteRenderer[] renderers;		// ���� ��ü+�μ� ��������Ʈ��
	[SerializeField] private float fadeSpeed = 8f;          // �� ������ �ʰ� ���� ���̵�

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
	/// �÷��̾� �þ� (visionCone) ���ο� �����ִ���?
	/// </summary>
	/// <returns></returns>
    private bool InsideCone()
	{
		if (visionCone == null) return false;

		// 손전등이 글리치/암전으로 꺼져 있으면 시야콘 안이어도 안 보임
		if (coneLight != null && coneLight.intensity < litIntensityThreshold) return false;

		Vector2 to = transform.position - visionCone.position;
		float dist = to.magnitude;
		if (dist > coneRange) return false;
		if (Vector2.Angle(visionCone.up, to) > coneAngle * 0.5f) return false;   // up = ������ ���� ����
		if (Physics2D.Raycast(visionCone.position, to.normalized, dist, wallMask)) return false;
		return true;
	}

	/// <summary>
	/// �÷��̾��� �⺻ �ݰ� ���� �����ִ���?
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
	/// ����� ���� ���� ����
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
