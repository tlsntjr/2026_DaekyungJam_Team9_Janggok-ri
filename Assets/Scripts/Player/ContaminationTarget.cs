using UnityEngine;

/// <summary>
/// À§Çù Á¢ÃË ½Ã µ¥¹ÌÁö
/// </summary>
public class ContaminationTarget : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var threat = other.GetComponent<ThreatContact>();
        if (threat == null) return;

        if (threat.killInstantly)
            EventBus.RaisePlayerDeath();
        else
            ContaminationSystem.Instance.Add(threat.contaminationAmount);
    }
}
