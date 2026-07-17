using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TimingMinigame))]
public class Generator : MonoBehaviour, IInteractable, ICounterCondition
{
    private IMinigame minigame;
    private TimingMinigame timingMinigame;

    private bool isPowerOn = false;
    public bool IsSatisfied => isPowerOn;
    public string Prompt => isPowerOn ? "이미 가동됨" : "E: 발전기 가동";

    [Header("발전기 설정")]
    [SerializeField] private int generatorID = 1;
    [SerializeField] private float noiseRadius = 15f;
    [SerializeField] private float distractionDuration = 10f;

    [Header("2번 발전기 전용 기믹")]
    [SerializeField] private float hideTimeRequired = 5f;
    [SerializeField] private FishMovement fishMovement;

    private int interruptCount = 0;
    private bool isInterrupted = false;
    private bool isWorking = false;
    private Coroutine concealmentCheckCoroutine;

    private void Awake()
    {
        minigame = GetComponent<IMinigame>();
        timingMinigame = GetComponent<TimingMinigame>();

        if (timingMinigame != null)
        {
            timingMinigame.OnMinigameComplete += StartGeneratorSequence;
            int requiredCount = (generatorID == 1) ? 2 : 4;
            timingMinigame.SetTargetSuccessCount(requiredCount);
        }
    }

    private void OnDisable()
    {
        if (timingMinigame != null)
            timingMinigame.OnMinigameComplete -= StartGeneratorSequence;
    }

    public void Interact()
    {
        if (isPowerOn || isWorking || isInterrupted)
        {
            Debug.LogWarning($"[Generator] 상호작용 불가 상태. PowerOn:{isPowerOn}, Working:{isWorking}, Interrupted:{isInterrupted}");
            return;
        }

        if (minigame != null)
        {
            minigame.StartOrResume();
        }
    }

    private void Update()
    {
        if (generatorID != 2 || isPowerOn) return;

        if (fishMovement != null && fishMovement.CurrentState == FishMovement.BehaviorState.Chase && !isInterrupted)
        {
            TriggerEmergencyInterrupt();
        }

        if (isInterrupted && fishMovement != null && fishMovement.CurrentState == FishMovement.BehaviorState.Patrol)
        {
            if (concealmentCheckCoroutine != null)
            {
                StopCoroutine(concealmentCheckCoroutine);
                concealmentCheckCoroutine = null;
            }

            isInterrupted = false;
            Debug.Log("<color=green>[Generator 2 해제 성공]</color> 인면어가 순찰(Patrol) 상태인 것이 확인되어 방해 락을 안전하게 해제합니다.");

            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.Show("위기가 넘어간 것 같다. 발전기를 다시 가동하자.");
            }
        }
    }

    private void TriggerEmergencyInterrupt()
    {
        isInterrupted = true;
        interruptCount++;

        if (timingMinigame != null)
        {
            timingMinigame.Interrupt();
        }

        Debug.Log($"<color=red>[Generator 2 인터럽트 가동]</color> 인면어 발각! 진행 카운트를 보존하고 은신 타이머를 켭니다. ({interruptCount}/2)");

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.Show("인면어의 시선이 이쪽을 향했다! 빨리 상자 뒤로 숨어라!");
        }

        if (concealmentCheckCoroutine != null) StopCoroutine(concealmentCheckCoroutine);
        concealmentCheckCoroutine = StartCoroutine(ConcealmentCheckRoutine());
    }

    private IEnumerator ConcealmentCheckRoutine()
    {
        float satisfiedTimer = 0f;

        while (satisfiedTimer < hideTimeRequired)
        {
            if (Concealment.IsPlayerConcealed)
            {
                satisfiedTimer += Time.deltaTime;

                if (fishMovement != null && fishMovement.CurrentState == FishMovement.BehaviorState.Chase)
                {
                    fishMovement.SetState(FishMovement.BehaviorState.Patrol);
                }
            }
            else
            {
                satisfiedTimer = 0f;
            }
            yield return null;
        }

        if (fishMovement != null)
        {
            Debug.Log("<color=cyan>[Concealment Timer Complete]</color> 5초 연속 은신 완료. 인면어를 Patrol로 전이시킵니다.");
            fishMovement.SetState(FishMovement.BehaviorState.Patrol);
        }

        yield return new WaitForEndOfFrame();
    }

    private void StartGeneratorSequence()
    {
        if (isWorking) return;
        StartCoroutine(GeneratorWorkingRoutine());
    }

    private IEnumerator GeneratorWorkingRoutine()
    {
        isWorking = true;
        EventBus.RaiseNoiseEmitted((Vector2)transform.position, noiseRadius);

        yield return new WaitForSeconds(distractionDuration);

        isPowerOn = true;
        isWorking = false;
    }
}