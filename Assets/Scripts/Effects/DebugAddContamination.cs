using UnityEngine;

/// <summary>
/// 테스트 전용: ContaminationSystem에 오염도를 강제로 더해서 단계 전환(점프스케어 등)을
/// 빠르게 테스트한다.
/// </summary>
public class DebugAddContamination : MonoBehaviour
{
    [SerializeField] private float amount = 0.7f;

    [ContextMenu("Add Contamination")]
    private void AddContamination()
    {
        ContaminationSystem.Instance.Add(amount);
        Debug.Log($"[DebugAddContamination] 오염도 {amount} 추가. 현재: {ContaminationSystem.Instance.Value} (단계 {ContaminationSystem.Instance.Stage})");
    }
}
