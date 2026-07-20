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

    private float safeZoneMin;
    private float safeZoneMax;

    public int SuccessCount { get; private set; }
    public bool IsComplete => SuccessCount >= targetSuccessCount;
    public bool IsPlaying => isPlaying;   // 외부(Generator 등)에서 진행 중 여부 확인용

    public event System.Action OnMinigameComplete;

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
            Debug.Log("미니게임 실패");

            // [�ǵ�� �ݿ�] ���� ���� ���
            if (!failSound.IsNull)
            {
                SoundManager.Instance.PlayOneShot(failSound, transform.position);
            }

            // ���� �� ī��Ʈ �ʱ�ȭ (���� ��ũ��Ʈ�� ResetMinigame Ȱ��)
            //ResetMinigame();
        }
    }
}