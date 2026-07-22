using UnityEngine;

/// <summary>
/// 몬스터 혹은 함정 등 오염도, 즉사 등
/// </summary>
public class ThreatContact : MonoBehaviour
{
    // 위협에 닿았을 때 위협 수치
    public float contaminationAmount = 0.2f;

    // 닿았을 때 즉사
    public bool killInstantly;

    // 은신 중인 플레이어도 타격하는가 — 기본 꺼짐(은신하면 안전).
    // 밀물벽(AdvancingTideWall)처럼 은신째로 삼켜야 하는 위협만 켤 것
    public bool hitsConcealedPlayer;
}
