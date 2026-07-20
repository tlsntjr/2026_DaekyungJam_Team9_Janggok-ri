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

	[Header("선택지")]
	[SerializeField] private GameObject choicePanel;
	[SerializeField] private Button[] choiceButtons;   // 인스펙터에 최대 개수만큼 미리 배치
	[SerializeField] private TMP_Text[] choiceLabels;

	[Header("연속 팝업 (RapidPrompt)")]
	[SerializeField] private GameObject rapidPromptPanel;
	[SerializeField] private Image rapidPromptTimerFill;  // Image Type = Filled 로 설정
	[SerializeField] private KeyCode cancelKey = KeyCode.Space;

	private Coroutine typingCoroutine;
	private bool isTyping;
	private string currentFullLine;

	private Action<int> pendingChoiceCallback;

	// 연속 대사(시퀀스) 상태
	private readonly Queue<string> sequenceLines = new();
	private Action sequenceCallback;

	// 연속 팝업 발생 시 Queue로 처리
	private readonly Queue<(float timeout, Action onSurvive, Action onFail)> rapidQueue = new();
	private bool rapidRunning;

	private void OnEnable()
	{
		DialogueSystem.Instance.OnShowLine				+= HandleShowLine;
		DialogueSystem.Instance.OnShowSequence		+= HandleShowSequence;
		DialogueSystem.Instance.OnShowChoice			+= HandleShowChoice;
		DialogueSystem.Instance.OnShowRapidPrompt	+= HandleShowRapidPrompt;
        lineClickCatcher.onClick.AddListener(AdvanceLine);
    }

	private void OnDisable()
	{
		DialogueSystem.Instance.OnShowLine				-= HandleShowLine;
		DialogueSystem.Instance.OnShowSequence		-= HandleShowSequence;
		DialogueSystem.Instance.OnShowChoice			-= HandleShowChoice;
		DialogueSystem.Instance.OnShowRapidPrompt	-= HandleShowRapidPrompt;
        lineClickCatcher.onClick.RemoveListener(AdvanceLine);
    }

	/// <summary>
	/// 단순 대사 출력
	/// </summary>
	/// <param name="line">대사 출력</param>
	private void HandleShowLine(string line)
	{
		linePanel.SetActive(true);
		currentFullLine = line;

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

		foreach (char c in line)
		{
			lineText.text += c;
			yield return new WaitForSeconds(charInterval);
		}

		isTyping				= false;
		typingCoroutine	= null;
	}
    
	/// <summary>
	/// 연속 대사 시작 — 첫 줄을 바로 출력하고, 이후 클릭마다 다음 줄로
	/// </summary>
	private void HandleShowSequence(string[] lines, Action onComplete)
	{
		sequenceLines.Clear();
		if (lines != null)
			foreach (var line in lines) sequenceLines.Enqueue(line);

		sequenceCallback = onComplete;

		if (sequenceLines.Count > 0)
			HandleShowLine(sequenceLines.Dequeue());
		else
		{
			// 빈 시퀀스 방어 — 콜백만 즉시 호출
			var cb = sequenceCallback;
			sequenceCallback = null;
			cb?.Invoke();
		}
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
        }
        else if (sequenceLines.Count > 0)
        {
            HandleShowLine(sequenceLines.Dequeue());
        }
        else
        {
            linePanel.SetActive(false);

            var cb = sequenceCallback;
            sequenceCallback = null;
            cb?.Invoke();
        }
    }


	/// <summary>
	/// 선택지 주어지는 대사
	/// </summary>
	/// <param name="options">선택지 문장</param>
	/// <param name="onSelect">클릭 시 콜백 함수 작성 필요</param>
    private void HandleShowChoice(string[] options, Action<int> onSelect)
	{
		choicePanel.SetActive(true);
		pendingChoiceCallback = onSelect;

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
		var callback					= pendingChoiceCallback;
		pendingChoiceCallback	= null;
		callback?.Invoke(index);
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
