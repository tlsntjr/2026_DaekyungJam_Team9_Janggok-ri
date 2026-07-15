using UnityEngine;

/// <summary>
/// 오염도 관련 시스템 총괄하는 클래스
/// </summary>
public class ContaminationSystem : MonoBehaviour
{
    public static ContaminationSystem Instance { get; private set; }

    [Header("단계별 임계값")]
    [SerializeField] private float stage1 = 0.3f;   // 시야 제한
    [SerializeField] private float stage2 = 0.6f;   // 환각·점프스케어
    [SerializeField] private float stage3 = 0.9f;   // 이동 저하·외형 변화

    [Header("자연 상승")]
    [SerializeField] private float passiveGainPerSecond = 0.001f;     // 시간 누적

    private float value;
    private int currentStage;

    public float Value      => value;
    public int Stage        => currentStage;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (passiveGainPerSecond > 0f && GameManager.Instance.CurrentState == GameState.Playing)
            Add(passiveGainPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// 오염도 연산 함수
    /// </summary>
    /// <param name="amount">오염도, 음수 허용</param>
    public void Add(float amount)
    {
        float before = value;
        value = Mathf.Clamp(value + amount, 0f, 1f);
        if (Mathf.Approximately(before, value)) return;

        EventBus.RaiseContaminationChanged(value);

        int newStage = CalculateStage(value);
        if (newStage != currentStage)
        {
            currentStage = newStage;
            EventBus.RaiseContaminationStageChanged(currentStage);
        }

        if (value >= 1f)
            EventBus.RaisePlayerDeath();
    }

    /// <summary>
    /// 현재 오염도 단계 반환
    /// </summary>
    /// <param name="v">현재 오염도</param>
    /// <returns></returns>
    private int CalculateStage(float v)
    {
        if (v >= stage3) return 3;
        if (v >= stage2) return 2;
        if (v >= stage1) return 1;
        return 0;
    }
}