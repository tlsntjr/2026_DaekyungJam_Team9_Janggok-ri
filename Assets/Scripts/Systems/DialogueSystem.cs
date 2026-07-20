using System;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
	public static DialogueSystem Instance { get; private set; }

	// ===== Dialogue UI가 구독하는 요청 이벤트들 =====
	public event Action<string> OnShowLine;									// 대사 렌더링 (string -> 대사)
	public event Action<string[], Action> OnShowSequence;					// 연속 대사 (클릭으로 다음 줄, 끝나면 콜백)
	public event Action<string[], Action<int>> OnShowChoice;			// 선택지 렌더링 (string -> 선택지, Action<int> -> 번호 선택에 따른 콜백)
	public event Action<float, Action, Action> OnShowRapidPrompt;		// 연속 팝업

	void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
	}

	/// <summary>
	/// 단순 대사 출력
	/// </summary>
	/// <param name="line">대사</param>
	public void Show(string line) => OnShowLine?.Invoke(line);

	/// <summary>
	/// 연속 대사 출력 — 스토리용. 클릭할 때마다 다음 줄로 넘어가고, 마지막 줄이 닫히면 콜백 호출.
	/// </summary>
	/// <param name="lines">대사 줄들 (순서대로 출력)</param>
	/// <param name="onComplete">전체 시퀀스 종료 후 콜백 (다음 이벤트 체이닝용, 생략 가능)</param>
	public void ShowSequence(string[] lines, Action onComplete = null)
		=> OnShowSequence?.Invoke(lines, onComplete);

	/// <summary>
	/// 선택지 있는 것
	/// </summary>
	/// <param name="options">우선 선택지 대사 렌더링</param>
	/// <param name="onSelect">선택 발생 이후 콜백 함수, int -> 선택지 index</param>
	public void ShowChoice(string[] options, Action<int> onSelect)
		=> OnShowChoice?.Invoke(options, onSelect);

	/// <summary>
	/// 연속 팝업
	/// </summary>
	/// <param name="timeout">시간 제한</param>
	/// <param name="onSurvive">성공 시 콜백</param>
	/// <param name="onFail">실패 시 콜백</param>
	public void ShowRapidPrompt(float timeout, Action onSurvive, Action onFail)
		=> OnShowRapidPrompt?.Invoke(timeout, onSurvive, onFail);
}
