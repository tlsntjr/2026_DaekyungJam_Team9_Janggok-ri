using UnityEngine;

[RequireComponent(typeof(TimingMinigame))]
public class Generator : MonoBehaviour, IInteractable, ICounterCondition
{
    private IMinigame minigame;
    private bool isPowerOn = false;
    public bool IsSatisfied => isPowerOn;
    // 인터페이스 멤버 구현 (오류 해결)
    public string Prompt => isPowerOn ? "이미 가동됨" : "E: 발전기 가동";

    [SerializeField] private float noiseRadius = 5f; // 소음 반경 추가

    private void Awake()
    {
        minigame = GetComponent<IMinigame>();
        if (minigame is TimingMinigame timing)
            timing.OnMinigameComplete += TurnOn;
    }

    private void OnDisable()
    {
        if (minigame is TimingMinigame timing)
            timing.OnMinigameComplete -= TurnOn;
    }

    public void Interact()
    {
        Debug.Log($"[Debug] 플레이어가 {gameObject.name}에 상호작용을 시도했습니다. (가동 여부: {isPowerOn})");

        if (isPowerOn) return;
        minigame.StartOrResume();
    }

    private void TurnOn()
    {
        if (isPowerOn) return;
        isPowerOn = true;
  
        EventBus.RaiseNoiseEmitted(transform.position, noiseRadius);
        Debug.Log("발전기 가동 완료!");
    }
}