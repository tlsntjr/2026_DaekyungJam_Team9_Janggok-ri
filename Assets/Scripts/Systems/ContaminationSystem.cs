using UnityEngine;

/// <summary>
/// ������ ���� �ý��� �Ѱ��ϴ� Ŭ����
/// </summary>
public class ContaminationSystem : MonoBehaviour
{
    public static ContaminationSystem Instance { get; private set; }

    [Header("�ܰ躰 �Ӱ谪")]
    [SerializeField] private float stage1 = 0.3f;   // �þ� ����
    [SerializeField] private float stage2 = 0.6f;   // ȯ�����������ɾ�
    [SerializeField] private float stage3 = 0.9f;   // �̵� ���ϡ����� ��ȭ

    [Header("�ڿ� ���")]
    [SerializeField] private float passiveGainPerSecond = 0.001f;     // �ð� ����

    private float value;
    private int currentStage;

    public float Value      => value;
    public int Stage        => currentStage;

    private void Awake()
    {
        // 씬 전환 후 살아남은 옛 인스턴스는 컴포넌트만 제거하고 현재 씬 것이 승계 (DDOL 좀비 방지)
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;
    }

    private void Update()
    {
        if (passiveGainPerSecond > 0f && GameManager.Instance.CurrentState == GameState.Playing)
            Add(passiveGainPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// ������ ���� �Լ�
    /// </summary>
    /// <param name="amount">������, ���� ���</param>
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
    /// 오염도 완전 초기화 — 사망 후 재시작/메인 복귀 시 호출.
    /// (이 시스템은 씬을 넘어 유지되므로, 리셋 없이 씬만 리로드하면 오염 100%인 채 시작해 즉시 또 사망함)
    /// </summary>
    public void ResetAll()
    {
        value = 0f;
        currentStage = 0;
        EventBus.RaiseContaminationChanged(value);
        EventBus.RaiseContaminationStageChanged(currentStage);
    }

    /// <summary>
    /// ���� ������ �ܰ� ��ȯ
    /// </summary>
    /// <param name="v">���� ������</param>
    /// <returns></returns>
    private int CalculateStage(float v)
    {
        if (v >= stage3) return 3;
        if (v >= stage2) return 2;
        if (v >= stage1) return 1;
        return 0;
    }
}