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

	[Header("판정 기준점 (비우면 스프라이트 중심 자동 사용)")]
	[SerializeField] private Transform litPoint;				// 피벗이 발밑이라 그대로 쓰면 위/아래 판정이 비대칭됨

	/// <summary>
	/// 빛 판정에 쓸 기준 좌표.
	/// 피벗(transform.position)은 발밑이라 위에서 비출 때 몸은 빛에 닿아도 판정이 빗나감 →
	/// 지정된 litPoint, 없으면 스프라이트 바운드 중심을 사용.
	/// </summary>
	private Vector2 TargetPoint
	{
		get
		{
			if (litPoint != null) return litPoint.position;
			if (renderers != null && renderers.Length > 0 && renderers[0] != null)
				return renderers[0].bounds.center;
			return transform.position;
		}
	}

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

		Vector2 target = TargetPoint;
		Vector2 to = target - (Vector2)visionCone.position;
		float dist = to.magnitude;
		if (dist > coneRange) return false;
		if (Vector2.Angle(visionCone.up, to) > coneAngle * 0.5f) return false;   // up = ������ ���� ����
		if (BlockedByWall(visionCone.position, target)) return false;
		return true;
	}

	/// <summary>
	/// �÷��̾��� �⺻ �ݰ� ���� �����ִ���?
	/// </summary>
	/// <returns></returns>
    private bool InsideAura()
	{
		if (playerAura == null) return false;

		Vector2 target = TargetPoint;
		Vector2 to = target - (Vector2)playerAura.position;
		float dist = to.magnitude;
		if (dist > auraRange) return false;
		if (BlockedByWall(playerAura.position, target)) return false;
		return true;
	}

	/// <summary>
	/// 벽 차폐 판정. 몬스터 자신의 콜라이더는 무시
	/// (자기 몸에 레이가 막혀 "위에서 비추면 안 보이는" 비대칭 버그 방지)
	/// </summary>
	private bool BlockedByWall(Vector2 from, Vector2 to)
	{
		var hits = Physics2D.RaycastAll(from, (to - from).normalized, Vector2.Distance(from, to), wallMask);
		foreach (var hit in hits)
		{
			if (hit.collider == null) continue;
			if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
			return true;
		}
		return false;
	}

#if UNITY_EDITOR
	/// <summary>
	/// [디버그] 판정에 실제로 쓰이는 시야콘을 씬 뷰에 그림.
	/// 노란 선 = 판정 콘의 중심축과 좌우 경계. 이 축이 손전등 비주얼과 다른 방향이면
	/// visionCone 필드에 회전하지 않는 오브젝트가 연결된 것.
	/// 초록/빨강 선 = 몬스터 판정점까지의 선 (초록=보임, 빨강=안 보임)
	/// </summary>
	private void OnDrawGizmosSelected()
	{
		if (visionCone == null) return;

		Vector3 origin = visionCone.position;
		Vector3 axis = visionCone.up;

		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(origin, origin + axis * coneRange);
		Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0, 0,  coneAngle * 0.5f) * axis) * coneRange);
		Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0, 0, -coneAngle * 0.5f) * axis) * coneRange);

		Gizmos.color = Application.isPlaying && IsLit() ? Color.green : Color.red;
		Gizmos.DrawLine(origin, TargetPoint);
		Gizmos.DrawWireSphere(TargetPoint, 0.15f);
	}
#endif

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
