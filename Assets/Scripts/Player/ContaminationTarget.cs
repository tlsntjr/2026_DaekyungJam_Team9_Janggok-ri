using UnityEngine;

/// <summary>
/// ���� ���� �� ������
/// </summary>
public class ContaminationTarget : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var threat = other.GetComponent<ThreatContact>();
        if (threat == null) return;

        // 은신 중엔 접촉 피해 면제 — 몬스터의 도약·순찰 경로가 은신처 위를 스치며
        // 숨어 있는 플레이어에게 피해를 주는 것 방지 (hitsConcealedPlayer 켠 위협은 예외)
        if (Concealment.IsPlayerConcealed && !threat.hitsConcealedPlayer) return;

        if (threat.killInstantly)
            EventBus.RaisePlayerDeath();
        else
            ContaminationSystem.Instance.Add(threat.contaminationAmount);
    }
}
