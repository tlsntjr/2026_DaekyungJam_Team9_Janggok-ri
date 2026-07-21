using System;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
	public static DialogueSystem Instance { get; private set; }

	// ===== Dialogue UI가 구독하는 요청 이벤트들 =====
	public event Action<string> OnShowLine;									// 대사 렌더링 (string -> 대사)
	public event Action<string[], Action<int>, Action> OnShowSequence;		// 연속 대사 (Action<int> -> 각 줄 표시 순간 콜백(줄 번호), Action -> 종료 콜백)
	public event Action<string, string[], Action<int>> OnShowChoice;	// 선택지 렌더링 (string -> 질문/직전 대사, string[] -> 선택지, Action<int> -> 선택 콜백)
	public event Action<float, Action, Action> OnShowRapidPrompt;		// 연속 팝업

	void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
	}

	/// <summary>
	/// 스토리 시퀀스(연속 대사)가 진행 중인가 — 기믹류 단발 안내(정적 힌트 등)가
	/// 시퀀스 위에 끼어들지 않도록 발화 전에 확인하는 용도. DialogueUI가 갱신.
	/// </summary>
	public bool IsSequenceActive { get; private set; }
	public void ReportSequenceActive(bool active) => IsSequenceActive = active;   // DialogueUI 전용

	/// <summary>
	/// 단순 대사 출력
	/// </summary>
	/// <param name="line">대사</param>
	public void Show(string line) => OnShowLine?.Invoke(line);

	/// <summary>
	/// 여러 후보 중 하나를 무작위로 골라 단순 대사로 출력 — 같은 상황이 반복돼도 매번 문구가 달라지게.
	/// (예: 갯벌 원혼이 매 페이즈 걸어오는 유혹 대사 풀)
	/// </summary>
	/// <param name="pool">후보 대사들</param>
	public void ShowRandom(string[] pool)
	{
		if (pool == null || pool.Length == 0) return;
		Show(pool[UnityEngine.Random.Range(0, pool.Length)]);
	}

	/// <summary>
	/// 연속 대사 출력 — 스토리용. 클릭/자동으로 다음 줄로 넘어가고, 마지막 줄이 닫히면 콜백 호출.
	/// </summary>
	/// <param name="lines">대사 줄들 (순서대로 출력)</param>
	/// <param name="onComplete">전체 시퀀스 종료 후 콜백 (다음 이벤트 체이닝용, 생략 가능)</param>
	public void ShowSequence(string[] lines, Action onComplete = null)
		=> OnShowSequence?.Invoke(lines, null, onComplete);

	/// <summary>
	/// 연속 대사 출력 + 각 줄이 표시되는 순간의 콜백 — 특정 줄에서 셰이크·글리치 등 연출을 걸 때 사용.
	/// </summary>
	/// <param name="lines">대사 줄들</param>
	/// <param name="onLineShown">각 줄이 화면에 뜨는 순간 호출 (int = 줄 번호, 0부터)</param>
	/// <param name="onComplete">전체 종료 콜백</param>
	public void ShowSequence(string[] lines, Action<int> onLineShown, Action onComplete = null)
		=> OnShowSequence?.Invoke(lines, onLineShown, onComplete);

	/// <summary>
	/// 선택지 있는 것 (질문 텍스트 없이 — 선택지 패널의 질문 칸은 숨겨짐)
	/// </summary>
	/// <param name="options">우선 선택지 대사 렌더링</param>
	/// <param name="onSelect">선택 발생 이후 콜백 함수, int -> 선택지 index</param>
	public void ShowChoice(string[] options, Action<int> onSelect)
		=> OnShowChoice?.Invoke(null, options, onSelect);

	/// <summary>
	/// 질문 텍스트와 함께 선택지 표시 — 직전 대사(유혹 문구 등)가 선택지 창에 그대로 남는다.
	/// </summary>
	/// <param name="question">선택지 위에 표시할 질문/대사</param>
	/// <param name="options">선택지들</param>
	/// <param name="onSelect">선택 콜백, int -> 선택지 index</param>
	public void ShowChoice(string question, string[] options, Action<int> onSelect)
		=> OnShowChoice?.Invoke(question, options, onSelect);

	/// <summary>
	/// 연속 팝업
	/// </summary>
	/// <param name="timeout">시간 제한</param>
	/// <param name="onSurvive">성공 시 콜백</param>
	/// <param name="onFail">실패 시 콜백</param>
	public void ShowRapidPrompt(float timeout, Action onSurvive, Action onFail)
		=> OnShowRapidPrompt?.Invoke(timeout, onSurvive, onFail);
}
