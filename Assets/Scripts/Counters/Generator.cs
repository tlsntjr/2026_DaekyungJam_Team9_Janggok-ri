using UnityEngine;
using System.Collections;
using FMODUnity;

[RequireComponent(typeof(TimingMinigame))]
public class Generator : MonoBehaviour, IInteractable, ICounterCondition
{
	private IMinigame minigame;
	private TimingMinigame timingMinigame;

	private bool isPowerOn = false;
	public bool IsSatisfied => isPowerOn;
	public string Prompt => isPowerOn ? "�̹� ������" : "������ ����";
	public string InteractKey => "E";

	[Header("������ ����")]
	[SerializeField] private int generatorID = 1;
	[SerializeField] private float noiseRadius = 15f;
	[SerializeField] private float distractionDuration = 10f;

	[Header("Sounds")]
	[SerializeField] private EventReference generatorSuccess;
	[SerializeField] private EventReference generatorLoop;
	[SerializeField] private EventReference generatorFailed;

	[Header("2�� ������ ���� ���")]
	[SerializeField] private float hideTimeRequired = 5f;
	[SerializeField] private FishMovement fishMovement;

	[Header("이동 시 미니게임 취소")]
	[SerializeField] private PlayerMovement playerMovement;   // 비우면 Player 태그로 자동 탐색
	[SerializeField] private float moveCancelGrace = 0.15f;   // 이동하며 E를 누른 직후 즉시 취소되는 것 방지 유예
	private float minigameStartTime;

	private int interruptCount = 0;
	private bool isInterrupted = false;
	private bool isWorking = false;
	private Coroutine concealmentCheckCoroutine;

	private void Awake()
	{
		minigame			= GetComponent<IMinigame>();
		timingMinigame	= GetComponent<TimingMinigame>();

		if (timingMinigame != null)
		{
			timingMinigame.OnMinigameComplete		+= StartGeneratorSequence;
			int requiredCount									= (generatorID == 1) ? 2 : 4;
			timingMinigame.SetTargetSuccessCount(requiredCount);
		}

		if (playerMovement == null)
		{
			GameObject playerObj = GameObject.FindWithTag("Player");
			if (playerObj != null) 
				playerMovement = playerObj.GetComponent<PlayerMovement>();
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
			Debug.LogWarning($"[Generator] ��ȣ�ۿ� �Ұ� ����. PowerOn:{isPowerOn}, Working:{isWorking}, Interrupted:{isInterrupted}");
			return;
		}

		// 이미 진행 중인데 E를 또 누르면 StartOrResume이 재실행되어
		// 판정 존이 계속 재배치되는 버그 방지 — 진행 중엔 무시 (타이밍 판정 키는 Space)
		if (timingMinigame != null && timingMinigame.IsPlaying) return;

		if (minigame != null)
		{
			minigame.StartOrResume();
			minigameStartTime = Time.time;
		}
	}

	private void Update()
	{
		CheckMoveCancel();   // 모든 발전기 공통 — 아래 2번 발전기 전용 early return보다 먼저

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
			Debug.Log("<color=green>[Generator 2 ���� ����]</color> �θ� ����(Patrol) ������ ���� Ȯ�εǾ� ���� ���� �����ϰ� �����մϴ�.");

			if (DialogueSystem.Instance != null)
			{
				DialogueSystem.Instance.Show("���Ⱑ �Ѿ �� ����. �����⸦ �ٽ� ��������.");
			}
		}
	}

	/// <summary>
	/// 미니게임 진행 중 플레이어가 이동하면 취소.
	/// (수리하는 동안은 자리에 묶임 = 무방비 — 발전기 소음이 위협을 부르는 긴장 구조와 맞물림.
	///  거리 기반 취소보다 엄격: 멀어지려면 어차피 움직여야 하므로 이동 감지가 거리 취소를 포함함)
	/// 진행도(SuccessCount)는 유지되므로 다시 상호작용하면 이어서 진행.
	/// </summary>
	private void CheckMoveCancel()
	{
		if (timingMinigame == null || !timingMinigame.IsPlaying) return;
		if (Time.time - minigameStartTime < moveCancelGrace) return;   // 이동하며 E 누른 직후 보호
		if (playerMovement == null || !playerMovement.IsMoving) return;

		timingMinigame.Interrupt();
		Debug.Log("<color=yellow>[Generator]</color> 이동이 감지되어 미니게임을 취소했습니다.");

		if (DialogueSystem.Instance != null)
		{
			DialogueSystem.Instance.Show("움직이면서 고칠 수는 없다. 자리에 서서 집중하자.");
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

		Debug.Log($"<color=red>[Generator 2 ���ͷ�Ʈ ����]</color> �θ�� �߰�! ���� ī��Ʈ�� �����ϰ� ���� Ÿ�̸Ӹ� �մϴ�. ({interruptCount}/2)");

		if (DialogueSystem.Instance != null)
		{
			DialogueSystem.Instance.Show("�θ���� �ü��� ������ ���ߴ�! ���� ���� �ڷ� �����!");
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
			Debug.Log("<color=cyan>[Concealment Timer Complete]</color> 5�� ���� ���� �Ϸ�. �θ� Patrol�� ���̽�ŵ�ϴ�.");
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