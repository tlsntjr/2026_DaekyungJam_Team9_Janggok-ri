using UnityEngine;
using System.Collections;
using FMOD.Studio;
using FMODUnity;

[RequireComponent(typeof(TimingMinigame))]
public class Generator : MonoBehaviour, IInteractable, ICounterCondition
{
	private IMinigame minigame;
	private TimingMinigame timingMinigame;

	private bool isPowerOn = false;
	public bool IsSatisfied => isPowerOn;
	public string Prompt => isPowerOn ? "이미 활성화됨" : "발전기 가동";
	public string InteractKey => "E";

	[Header("������ ����")]
	[SerializeField] private int generatorID = 1;
	[SerializeField] private float noiseRadius = 15f;
	[SerializeField] private float distractionDuration = 10f;

	[Header("Sounds")]
	[SerializeField] private EventReference generatorSuccess;
	[SerializeField] private EventReference generatorLoop;
	[SerializeField] private EventReference generatorFailed;

	[Header("시동 성공 대사 (비우면 스킵 — 발전기마다 다르게: 1번 '하나로는 부족한가...', 2번 '안테나가 살아났다...')")]
	[SerializeField, TextArea(2, 3)] private string poweredOnLine;

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
	private EventInstance loopInstance;
	private bool loopPlaying;

	private void Awake()
	{
		minigame			= GetComponent<IMinigame>();
		timingMinigame	= GetComponent<TimingMinigame>();

		if (timingMinigame != null)
		{
			int requiredCount = (generatorID == 1) ? 2 : 4;
			timingMinigame.SetTargetSuccessCount(requiredCount);
		}

		if (playerMovement == null)
		{
			GameObject playerObj = GameObject.FindWithTag("Player");
			if (playerObj != null)
				playerMovement = playerObj.GetComponent<PlayerMovement>();
		}
	}

	// 구독은 OnEnable에서 — Awake 구독은 페이즈 배선(enableOnStart 등)으로 오브젝트가
	// 껐다 켜질 때 OnDisable이 해제한 구독이 영영 복구되지 않아,
	// 미니게임 4/4 완료가 허공에 발사되는(시동·대사 무반응) 문제가 있었음
	private void OnEnable()
	{
		if (timingMinigame != null)
		{
			timingMinigame.OnMinigameComplete	+= StartGeneratorSequence;
			timingMinigame.OnFailurePenalty		+= HandleFailurePenalty;
		}
	}

	private void OnDisable()
	{
		if (timingMinigame != null)
		{
			timingMinigame.OnMinigameComplete -= StartGeneratorSequence;
			timingMinigame.OnFailurePenalty -= HandleFailurePenalty;
		}

		if (loopPlaying)
		{
			SoundManager.Instance.StopLoop(loopInstance, immediate: true);
			loopPlaying = false;
		}
	}

	/// <summary>
	/// 미니게임 연속 실패 페널티(소음 유발) 발동 시 — 발전기 고유의 "실패" 사운드 재생
	/// (미니게임의 매 판정 실패음과는 별개로, "발전기가 완전히 틀어졌다"는 신호)
	/// </summary>
	private void HandleFailurePenalty()
	{
		if (!generatorFailed.IsNull)
			SoundManager.Instance.PlayOneShot(generatorFailed, transform.position);
	}

	public void Interact()
	{
		if (isPowerOn || isWorking || isInterrupted)
		{
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

			if (DialogueSystem.Instance != null)
			{
				DialogueSystem.Instance.Show("...지나간 것 같다. 발전기를 다시 돌리자.");
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
			DialogueSystem.Instance.Show("들켰다! 시선을 피해서... 뒤에 숨어야 해!");
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

		// 시동 사운드 + 최초 1회 소음 방출 — 위협이 반응하는 건 이 순간뿐
		if (!generatorSuccess.IsNull)
			SoundManager.Instance.PlayOneShot(generatorSuccess, transform.position);

		EventBus.RaiseNoiseEmitted((Vector2)transform.position, noiseRadius);

		// 시동 성공 대사 — 발전기별로 스토리 비트를 실어 나름 (스토리 시퀀스 진행 중이면 덮지 않고 스킵)
		if (!string.IsNullOrEmpty(poweredOnLine) && DialogueSystem.Instance != null)
		{
			if (DialogueSystem.Instance.IsSequenceActive)
				Debug.LogWarning($"[Generator {generatorID}] 시동 대사 스킵 — IsSequenceActive가 켜져 있음. " +
					"시퀀스가 안 도는데도 이 경고가 나오면 플래그가 꼬인 것 (어떤 시퀀스가 닫히지 않았는지 추적 필요)");
			else
				DialogueSystem.Instance.Show(poweredOnLine);
		}

		// 시동 이후로는 계속 웅웅거리는 루프 — 추가 Noise 방출 없이 배경음으로만 재생
		if (!generatorLoop.IsNull)
		{
			loopInstance = SoundManager.Instance.PlayLoop(generatorLoop, transform);
			loopPlaying = true;
		}

		yield return new WaitForSeconds(distractionDuration);

		isPowerOn = true;
		isWorking = false;
		// 루프는 여기서 정지하지 않음 — 발전기가 켜져 있는 한 계속 재생 (OnDisable에서 정리)
	}
}