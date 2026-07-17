using UnityEngine;
using UnityEngine.UI;

public class TimingMinigame : MonoBehaviour, IMinigame
{
    [Header("UI 연결")]
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject uiPanel;

    [Header("성공 영역 UI 지정")]
    [SerializeField] private RectTransform hitZoneUI;

    [Header("설정")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private int targetSuccessCount = 2;

    private float safeZoneMin;
    private float safeZoneMax;

    public int SuccessCount { get; private set; }
    public bool IsComplete => SuccessCount >= targetSuccessCount;

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
            Debug.Log($"성공! {SuccessCount}/{targetSuccessCount}");

            if (IsComplete)
            {
                Interrupt();
                OnMinigameComplete?.Invoke();
            }
            else
            {
                RandomizeHitZonePosition();
                CalculateSafeZoneBounds();
            }
        }
    }
}