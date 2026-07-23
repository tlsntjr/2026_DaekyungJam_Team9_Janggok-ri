using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class TimingMinigame : MonoBehaviour, IMinigame
{
    [Header("UI ����")]
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject uiPanel;

    [Header("���� ���� UI ����")]
    [SerializeField] private RectTransform hitZoneUI;

    [Header("����")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private int targetSuccessCount = 2;

    [Header("���� ����")]
    [SerializeField] private EventReference successSound;
    [SerializeField] private EventReference failSound;

    [Header("실패 페널티 — 연속 실패 시 소음 유발(주변 몬스터 유인) + 진행도 초기화")]
    [SerializeField] private int failThreshold = 3;       // 이 횟수만큼 연속 실패하면 페널티 발동
    [SerializeField] private float failNoiseRadius = 12f;  // 페널티 소음 반경 (Generator.noiseRadius보다 좁게 — 실패의 대가)

    private int consecutiveFails = 0;

    private float safeZoneMin;
    private float safeZoneMax;

    public int SuccessCount { get; private set; }
    public bool IsComplete => SuccessCount >= targetSuccessCount;
    public bool IsPlaying => isPlaying;   // 외부(Generator 등)에서 진행 중 여부 확인용

    public event System.Action OnMinigameComplete;
    public event System.Action OnFailurePenalty;   // 연속 실패 임계치 도달 시 — Generator 등이 자기만의 페널티 사운드/연출을 붙일 수 있게

    private bool isPlaying = false;

    public void SetTargetSuccessCount(int count)
    {
        targetSuccessCount = count;
    }

    public void ResetMinigame()
    {
        SuccessCount = 0;
        if (slider != null) slider.value = 0f;
    }

    public void StartOrResume()
    {
        isPlaying = true;
        if (uiPanel != null) uiPanel.SetActive(true);

        RandomizeHitZonePosition();
        CalculateSafeZoneBounds();
    }

    public void Interrupt()
    {
        isPlaying = false;
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying || IsComplete) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Interrupt();
            return;
        }

        slider.value = Mathf.PingPong(Time.time * speed, 1f);

        if (Input.GetKeyDown(KeyCode.Space)) CheckTiming();
    }

    private void RandomizeHitZonePosition()
    {
        if (slider == null || hitZoneUI == null) return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        float sliderWidth = sliderRect.rect.width;
        float hitZoneWidth = hitZoneUI.rect.width * hitZoneUI.localScale.x;

        float maxMovableRange = (sliderWidth * 0.5f) - (hitZoneWidth * 0.5f);
        float randomX = Random.Range(-maxMovableRange, maxMovableRange);

        hitZoneUI.anchoredPosition = new Vector2(randomX, hitZoneUI.anchoredPosition.y);
    }

    private void CalculateSafeZoneBounds()
    {
        if (slider == null || hitZoneUI == null)
        {
            safeZoneMin = 0.4f;
            safeZoneMax = 0.6f;
            return;
        }

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        float sliderWidth = sliderRect.rect.width;
        float hitZoneLocalX = hitZoneUI.anchoredPosition.x;
        float hitZoneWidth = hitZoneUI.rect.width * hitZoneUI.localScale.x;

        float leftEdgeOfHitZone = (sliderWidth * 0.5f) + hitZoneLocalX - (hitZoneWidth * 0.5f);

        safeZoneMin = Mathf.Clamp01(leftEdgeOfHitZone / sliderWidth);
        safeZoneMax = Mathf.Clamp01((leftEdgeOfHitZone + hitZoneWidth) / sliderWidth);
    }

    private void CheckTiming()
    {
        CalculateSafeZoneBounds();

        if (slider.value >= safeZoneMin && slider.value <= safeZoneMax)
        {
            SuccessCount++;
            consecutiveFails = 0;   // 연속 실패 스트릭 초기화
            Debug.Log($"미니게임 성공! {SuccessCount}/{targetSuccessCount}");

            // [�ǵ�� �ݿ�] ���� ���� ��� (FMOD ���� ������ Null üũ ����)
            if (!successSound.IsNull)
            {
                SoundManager.Instance.PlayOneShot(successSound, transform.position);
            }

            // ��ǥ �޼� �� �Ϸ� ó��
            if (IsComplete)
            {
                Interrupt(); // ����ó�� �̴ϰ����� ���߰�(UI ���� ��)
                OnMinigameComplete?.Invoke(); // ���� ������� �ѱ�� �̺�Ʈ ����
            }
            else
            {
                // ���� �� ����� �ϸ� ���� ��ǥ ���� ����
                RandomizeHitZonePosition();
                CalculateSafeZoneBounds();
            }
        }
        // 2. Ÿ�̹� ���߱� ���� ��
        else
        {
            consecutiveFails++;
            Debug.Log($"미니게임 실패 ({consecutiveFails}/{failThreshold})");

            if (!failSound.IsNull)
            {
                SoundManager.Instance.PlayOneShot(failSound, transform.position);
            }

            if (consecutiveFails >= failThreshold)
                TriggerFailurePenalty();
        }
    }

    /// <summary>
    /// 연속 실패 임계치 도달 — "그냥 계속 딸깍하면 깨진다"는 허점을 막는 페널티.
    /// 소음을 방송해 주변 몬스터를 유인하고, 진행도를 초기화해 처음부터 다시 하게 만든다.
    /// 소음 대응(은신 등)에 신경 써야 하므로 미니게임에서도 강제 퇴장시켜 위협에 대응할 시간을 준다.
    /// </summary>
    private void TriggerFailurePenalty()
    {
        consecutiveFails = 0;

        Debug.Log("<color=red>[TimingMinigame]</color> 연속 실패로 소음 발생! 근처 위협이 반응합니다.");
        EventBus.RaiseNoiseEmitted((Vector2)transform.position, failNoiseRadius);

        ResetMinigame();
        Interrupt();   // UI 강제 종료 — 플레이어가 다가오는 위협에 대응하도록
        OnFailurePenalty?.Invoke();
    }
}