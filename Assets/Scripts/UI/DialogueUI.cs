using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("단순 대사")]
    [SerializeField] private GameObject linePanel;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private Button lineClickCatcher;

    [Header("타이핑 이펙트")]
	[SerializeField] private float charInterval = 0.03f;   // 한 글자당 간격(초)

	[Header("자동 진행 — 타이핑 완료 후 이 시간이 지나면 자동으로 다음 줄/닫기 (0 = 수동 클릭만)")]
	[SerializeField] private float autoAdvanceDelay = 2.2f;

	[Header("선택지")]
	[SerializeField] private GameObject choicePanel;
	[SerializeField] private TMP_Text choiceQuestionText;   // 선택지 위에 표시할 질문/직전 대사 (비우면 스킵)
	[SerializeField] private Button[] choiceButtons;   // 인스펙터에 최대 개수만큼 미리 배치
	[SerializeField] private TMP_Text[] choiceLabels;

	[Header("연속 팝업 (RapidPrompt)")]
	[SerializeField] private GameObject rapidPromptPanel;
	[SerializeField] private Image rapidPromptTimerFill;  // Image Type = Filled 로 설정
	[SerializeField] private KeyCode cancelKey = KeyCode.Space;

	[Header("Player")]
	[SerializeField] private PlayerMovement playerMovement;

	private Coroutine typingCoroutine;
	private Coroutine autoAdvanceCoroutine;
	private bool isTyping;
	private string currentFullLine;

	private Action<int> pendingChoiceCallback;

	// 연속 대사(시퀀스) 상태
	private readonly Queue<string> sequenceLines = new();
	private Action sequenceCallback;
	private Action<int> sequenceLineCallback;   // 각 줄 표시 순간 알림 (연출 훅)
	private int sequenceLineIndex;

	// 연속 팝업 발생 시 Queue로 처리
	private readonly Queue<(float timeout, Action onSurvive, Action onFail)> rapidQueue = new();
	private bool rapidRunning;

	private bool subscribed;

	// 씬 로드 중 오브젝트별 Awake/OnEnable 순서는 보장되지 않아서,
	// DialogueSystem.Instance가 아직 없을 때 OnEnable이 먼저 돌면 구독이 통째로 유실됨(대사 안 나옴) —
	// OnEnable과 Start 양쪽에서 시도해 어느 순서로 로드되든 구독이 성립하게 함
	private void OnEnable()
	{
		TrySubscribe();
		EventBus.OnPlayerDeath += HandlePlayerDeath;
	}

	private void Start() => TrySubscribe();

	private void TrySubscribe()
	{
		if (subscribed || DialogueSystem.Instance == null) return;
		subscribed = true;

		DialogueSystem.Instance.OnShowLine				+= HandleShowLine;
		DialogueSystem.Instance.OnShowSequence			+= HandleShowSequence;
		DialogueSystem.Instance.OnShowChoice			+= HandleShowChoice;
		DialogueSystem.Instance.OnShowRapidPrompt	+= HandleShowRapidPrompt;
        lineClickCatcher.onClick.AddListener(AdvanceLine);
    }

	private void OnDisable()
	{
		EventBus.OnPlayerDeath -= HandlePlayerDeath;

		if (!subscribed) return;
		subscribed = false;

		if (DialogueSystem.Instance != null)
		{
			DialogueSystem.Instance.OnShowLine				-= HandleShowLine;
			DialogueSystem.Instance.OnShowSequence			-= HandleShowSequence;
			DialogueSystem.Instance.OnShowChoice			-= HandleShowChoice;
			DialogueSystem.Instance.OnShowRapidPrompt	-= HandleShowRapidPrompt;
		}
        lineClickCatcher.onClick.RemoveListener(AdvanceLine);
    }

	/// <summary>
	/// 단순 대사 출력
	/// </summary>
	/// <param name="line">대사 출력</param>
	/// <summary>대사창/선택지 표시 상태를 DialogueSystem에 보고 — 투척 등 좌클릭 액션 잠금용</summary>
	private void ReportOpenState()
	{
		if (DialogueSystem.Instance != null)
			DialogueSystem.Instance.ReportDialogueOpen(
				(linePanel != null && linePanel.activeSelf) || (choicePanel != null && choicePanel.activeSelf));
	}

	private void HandleShowLine(string line)
	{
		linePanel.SetActive(true);
		ReportOpenState();
		currentFullLine = line;

		StopAutoAdvance();   // 새 줄이 시작되면 이전 자동 진행 타이머는 무효
		if (typingCoroutine != null) StopCoroutine(typingCoroutine);
		typingCoroutine = StartCoroutine(TypeLine(line));
	}


	/// <summary>
	/// 대사 출력하는데 한 글자씩 타이핑되는 형태
	/// </summary>
	/// <param name="line">대사</param>
	/// <returns></returns>
	private IEnumerator TypeLine(string line)
	{
		isTyping			= true;
		lineText.text		= "";

		int i = 0;
		while (i < line.Length)
		{
			// 리치 텍스트 태그(<color=...>, </color> 등)는 통째로 한 번에 붙임 —
			// 한 글자씩 찍으면 미완성 태그("<colo")가 파싱되지 못하고 그대로 화면에 보임
			if (line[i] == '<')
			{
				int close = line.IndexOf('>', i);
				if (close >= 0)
				{
					lineText.text += line.Substring(i, close - i + 1);
					i = close + 1;
					continue;   // 태그는 타이핑 간격을 소비하지 않음 (보이지 않는 글자니까)
				}
			}

			lineText.text += line[i];
			i++;
			yield return new WaitForSeconds(charInterval);
		}

		isTyping				= false;
		typingCoroutine	= null;
		StartAutoAdvance();   // 전체 출력 완료 — 자동 진행 타이머 시작
	}
    
	/// <summary>
	/// 연속 대사 시작 — 첫 줄을 바로 출력하고, 이후 클릭마다 다음 줄로
	/// </summary>
	private void HandleShowSequence(string[] lines, Action<int> onLineShown, Action onComplete)
	{
		// 진행 중이던 시퀀스를 새 시퀀스가 덮을 때 — 이전 완료 콜백을 유실하지 않고 먼저 발화.
		// 콜백에 페이즈 진행(FinishPickup·CompleteHaunt 등)이 걸려 있으면 유실 시 진행이 영구 정지됨
		if (sequenceCallback != null)
		{
			Debug.LogWarning("[DialogueUI] 시퀀스가 끝나기 전에 새 시퀀스가 시작됨 — 이전 완료 콜백을 먼저 발화하고 교체");
			var orphaned = sequenceCallback;
			sequenceCallback = null;
			sequenceLineCallback = null;
			orphaned.Invoke();
		}

		sequenceLines.Clear();
		if (lines != null)
			foreach (var line in lines) sequenceLines.Enqueue(line);

		sequenceCallback = onComplete;
		sequenceLineCallback = onLineShown;
		sequenceLineIndex = -1;

		if (sequenceLines.Count > 0)
		{
			DialogueSystem.Instance.ReportSequenceActive(true);   // 기믹 안내 대사가 끼어들지 않게 표시
			ShowNextSequenceLine();
		}
		else
		{
			// 빈 시퀀스 방어 — 콜백만 즉시 호출
			var cb = sequenceCallback;
			sequenceCallback = null;
			sequenceLineCallback = null;
			cb?.Invoke();
		}
	}

	/// <summary>시퀀스의 다음 줄 표시 + 줄 번호 콜백 발화 (특정 줄 연출 훅)</summary>
	private void ShowNextSequenceLine()
	{
		sequenceLineIndex++;
		HandleShowLine(sequenceLines.Dequeue());
		sequenceLineCallback?.Invoke(sequenceLineIndex);
	}

	/// <summary>
	/// 대사창 영역 클릭 시: 타이핑 중이면 전체 출력(스킵) →
	/// 시퀀스에 남은 줄이 있으면 다음 줄 → 다 끝났으면 닫고 완료 콜백
	/// </summary>
    private void AdvanceLine()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine	= null;
            lineText.text			= currentFullLine;
            isTyping				= false;
            StartAutoAdvance();   // 스킵으로 전체 출력된 경우에도 타이머 시작
        }
        else if (sequenceLines.Count > 0)
        {
            StopAutoAdvance();
            ShowNextSequenceLine();
        }
        else
        {
            StopAutoAdvance();
            linePanel.SetActive(false);
            ReportOpenState();

            DialogueSystem.Instance.ReportSequenceActive(false);

            var cb = sequenceCallback;
            sequenceCallback = null;
            sequenceLineCallback = null;
            cb?.Invoke();
        }
    }

	/// <summary>
	/// 사망 시 대화 UI 전체 강제 종료 — 대사창(전면 클릭 캐처)·선택지·연속 팝업이 남아 있으면
	/// 사망 화면의 버튼 클릭을 가로채는 문제 방지.
	/// 진행 중이던 시퀀스 완료 콜백은 발화하지 않고 폐기 — 사망 후 페이즈 진행이 이어지면 안 됨
	/// (재시작 시 RestartSaveModel이 상태를 복원하므로 여기서 흐름을 이을 이유가 없다).
	/// </summary>
	private void HandlePlayerDeath()
	{
		StopAllCoroutines();   // 타이핑·자동 진행·연속 팝업 코루틴 전부 정지
		typingCoroutine = null;
		autoAdvanceCoroutine = null;
		isTyping = false;
		rapidRunning = false;
		rapidQueue.Clear();

		sequenceLines.Clear();
		sequenceCallback = null;
		sequenceLineCallback = null;
		pendingChoiceCallback = null;

		if (linePanel != null) linePanel.SetActive(false);
		if (choicePanel != null) choicePanel.SetActive(false);
		if (rapidPromptPanel != null) rapidPromptPanel.SetActive(false);
		ReportOpenState();

		if (DialogueSystem.Instance != null)
			DialogueSystem.Instance.ReportSequenceActive(false);

		SetMovementLocked(false);   // 선택지 도중 사망 시 이동 잠금이 유령처럼 남지 않게
	}

	/// <summary>
	/// 자동 진행 — 타이핑 완료 후 autoAdvanceDelay가 지나면 클릭 없이도 다음 줄/닫기.
	/// 대사 종료 타이밍이 플레이어 손에 있으면 기믹 타이밍을 조작할 수 있어서(대사 띄워둔 채 이동 등) 도입.
	/// </summary>
	private void StartAutoAdvance()
	{
		if (autoAdvanceDelay <= 0f) return;
		StopAutoAdvance();
		autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
	}

	private void StopAutoAdvance()
	{
		if (autoAdvanceCoroutine != null)
		{
			StopCoroutine(autoAdvanceCoroutine);
			autoAdvanceCoroutine = null;
		}
	}

	private IEnumerator AutoAdvanceRoutine()
	{
		yield return new WaitForSeconds(autoAdvanceDelay);
		autoAdvanceCoroutine = null;
		AdvanceLine();
	}


	/// <summary>
	/// 선택지 주어지는 대사
	/// </summary>
	/// <param name="options">선택지 문장</param>
	/// <param name="onSelect">클릭 시 콜백 함수 작성 필요</param>
    private void HandleShowChoice(string question, string[] options, Action<int> onSelect)
	{
		choicePanel.SetActive(true);
		ReportOpenState();
		pendingChoiceCallback = onSelect;
		SetMovementLocked(true);   // 선택하는 동안 이동 정지 — 선택이 끝나면 해제

		// 질문/직전 대사 — 전달된 텍스트가 있으면 표시, 없으면 질문 칸 숨김 (기본값이 남는 문제 방지)
		if (choiceQuestionText != null)
		{
			bool hasQuestion = !string.IsNullOrEmpty(question);
			choiceQuestionText.gameObject.SetActive(hasQuestion);
			if (hasQuestion) choiceQuestionText.text = question;
		}

		for (int i = 0; i < choiceButtons.Length; i++)
		{
			bool active = i < options.Length;
			choiceButtons[i].gameObject.SetActive(active);
			if (!active) continue;

			choiceLabels[i].text = options[i];

			int idx = i; // 클로저 캡처 주의 — 반드시 지역변수로 복사
			choiceButtons[i].onClick.RemoveAllListeners();
			choiceButtons[i].onClick.AddListener(() => SelectChoice(idx));
		}
	}

	/// <summary>
	/// 선택지 선택
	/// </summary>
	/// <param name="index">선택지 인덱스</param>
	private void SelectChoice(int index)
	{
		choicePanel.SetActive(false);
		ReportOpenState();
		SetMovementLocked(false);

		var callback					= pendingChoiceCallback;
		pendingChoiceCallback	= null;
		callback?.Invoke(index);
	}

	/// <summary>
	/// 선택지 동안 플레이어 이동 잠금/해제 — PlayerMovement는 Player 태그로 1회 탐색 후 캐시
	/// </summary>
	private void SetMovementLocked(bool locked)
	{
		if (playerMovement == null)
		{
			GameObject player = GameObject.FindWithTag("Player");
			if (player != null) playerMovement = player.GetComponent<PlayerMovement>();
			if (playerMovement == null)
			{
				Debug.LogWarning("[DialogueUI] Player 태그의 PlayerMovement를 찾지 못해 이동 잠금 실패");
				return;
			}
		}

		playerMovement.MovementLocked = locked;
		Debug.Log($"<color=cyan>[DialogueUI]</color> 이동 잠금: {locked}");
	}

	/// <summary>
	/// 연속 팝업 처리
	/// </summary>
	/// <param name="timeout">제한 시간</param>
	/// <param name="onSurvive">성공시</param>
	/// <param name="onFail">실패시</param>
	private void HandleShowRapidPrompt(float timeout, Action onSurvive, Action onFail)
	{
		rapidQueue.Enqueue((timeout, onSurvive, onFail));
		if (!rapidRunning) StartCoroutine(ProcessRapidQueue());
	}

	/// <summary>
	/// 제한시간 내 성공하는 미니게임 시작
	/// </summary>
	/// <returns></returns>
	private IEnumerator ProcessRapidQueue()
	{
		rapidRunning = true;
		while (rapidQueue.Count > 0)
		{
			var (timeout, onSurvive, onFail) = rapidQueue.Dequeue();
			yield return RunOneRapidPrompt(timeout, onSurvive, onFail);
		}
		rapidRunning = false;
	}

    /// <summary>
    /// 여러 차례가 아닌 한 번
    /// </summary>
    /// <param name="timeout">제한 시간</param>
    /// <param name="onSurvive">성공시</param>
    /// <param name="onFail">실패시</param>
    /// <returns></returns>
    private IEnumerator RunOneRapidPrompt(float timeout, Action onSurvive, Action onFail)
	{
		rapidPromptPanel.SetActive(true);
		float remaining = timeout;
		bool canceled = false;

		while (remaining > 0f)
		{
			if (Input.GetKeyDown(cancelKey)) { canceled = true; break; }

			remaining -= Time.deltaTime;
			if (rapidPromptTimerFill != null)
				rapidPromptTimerFill.fillAmount = remaining / timeout;

			yield return null;
		}

		rapidPromptPanel.SetActive(false);

		if (canceled)		onSurvive?.Invoke();
		else				onFail?.Invoke();
	}
}
