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
}
