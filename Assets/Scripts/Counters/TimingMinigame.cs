using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

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

    [Header("사운드 설정")]
    [SerializeField] private EventReference successSound;
    [SerializeField] private EventReference failSound;

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

            // [피드백 반영] 성공 사운드 재생 (FMOD 에러 방지용 Null 체크 포함)
            if (!successSound.IsNull)
            {
                SoundManager.Instance.PlayOneShot(successSound, transform.position);
            }

            // 목표 달성 시 완료 처리
            if (IsComplete)
            {
                Interrupt(); // 기존처럼 미니게임을 멈추고(UI 끄기 등)
                OnMinigameComplete?.Invoke(); // 다음 페이즈로 넘기는 이벤트 발행
            }
            else
            {
                // 아직 더 맞춰야 하면 다음 목표 지점 세팅
                RandomizeHitZonePosition();
                CalculateSafeZoneBounds();
            }
        }
        // 2. 타이밍 맞추기 실패 시
        else
        {
            Debug.Log("실패! 카운트가 초기화됩니다.");

            // [피드백 반영] 실패 사운드 재생
            if (!failSound.IsNull)
            {
                SoundManager.Instance.PlayOneShot(failSound, transform.position);
            }

            // 실패 시 카운트 초기화 (기존 스크립트의 ResetMinigame 활용)
            //ResetMinigame();
        }
    }
}