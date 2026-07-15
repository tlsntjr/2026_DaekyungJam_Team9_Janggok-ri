using UnityEngine;
using UnityEngine.UI;

public class TimingMinigame : MonoBehaviour, IMinigame
{
    [Header("UI 연결")]
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject uiPanel;

    [Header("설정")]
    // [SerializeField]를 붙여야 인스펙터에서 변수값이 바인딩됩니다.
    [SerializeField] private float speed = 2f;
    [SerializeField] private float safeZoneMin = 0.4f;
    [SerializeField] private float safeZoneMax = 0.6f;
    [SerializeField] private int targetSuccessCount = 2;

    public int SuccessCount { get; private set; }
    public bool IsComplete => SuccessCount >= targetSuccessCount;

    // 외부에서 완료를 감지할 수 있도록 이벤트 추가
    public event System.Action OnMinigameComplete;

    private bool isPlaying = false;

    public void StartOrResume()
    {
        isPlaying = true;
        if (uiPanel != null) uiPanel.SetActive(true);
    }

    public void Interrupt()
    {
        isPlaying = false;
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying || IsComplete) return;

        // 이제 speed 변수를 정상적으로 찾을 수 있습니다.
        slider.value = Mathf.PingPong(Time.time * speed, 1f);

        if (Input.GetKeyDown(KeyCode.Space)) CheckTiming();
    }

    private void CheckTiming()
    {
        // 이제 safeZoneMin, safeZoneMax 변수를 정상적으로 찾을 수 있습니다.
        if (slider.value >= safeZoneMin && slider.value <= safeZoneMax)
        {
            SuccessCount++;
            Debug.Log($"성공! {SuccessCount}/{targetSuccessCount}");

            if (IsComplete)
            {
                Interrupt();
                OnMinigameComplete?.Invoke(); // 완료 알림 발행
            }
        }
        else
        {
            Debug.Log("실패!");
        }
    }
}