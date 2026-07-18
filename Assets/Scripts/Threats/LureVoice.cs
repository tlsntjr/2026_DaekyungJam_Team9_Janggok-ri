using UnityEngine;
using System;

public class LureVoice : MonoBehaviour, IThreatBehavior
{
    [Header("대사 연출")]
    [SerializeField] private string[] promptLines;
    [SerializeField] private bool useRapidPrompt;
    [SerializeField] private float rapidTimeout = 3.5f;

    [Header("연결")]
    [SerializeField] private HauntController controller;
    [SerializeField] private RuleWatcher rule;

    public bool IsNeutralized { get; private set; }
    private int currentLineIndex = 0;

    public void Activate()
    {
        IsNeutralized = false;
        currentLineIndex = 0;
        Debug.Log($"<color=gray>[Test]</color> {gameObject.name} LureVoice Activated!");

        if (DialogueSystem.Instance == null)
        {
            if (rule != null) rule.MarkSatisfied();
            return;
        }

        if (useRapidPrompt)
        {
            // 3페이즈 모드일 때는 루프 출력만 수행
            foreach (var line in promptLines)
            {
                Debug.Log($"<color=cyan>[LureVoice 모드]</color> 대기 중: {line}");
            }
            // 필요하다면 여기서 RunRapidPrompts() 같은 코루틴을 시작하세요.
        }
        else
        {
            // 일반 모드일 때만 첫 대사를 출력
            Debug.Log($"<color=cyan>[LureVoice 대사 {currentLineIndex + 1}/{promptLines.Length}]</color> <b>{promptLines[currentLineIndex]}</b>");
            ShowNextChoice(); // 다음 선택지로 넘어가는 로직
        }
    }

    private void ShowNextChoice()
    {
        // 배열의 모든 대사를 통과했을 때 성공 처리
        if (currentLineIndex >= promptLines.Length)
        {
            Debug.Log($"<color=green>[LureVoice]</color> 모든 질문 통과 완료! (Rule 만족)");

            // 에러 방지: 인스펙터에 RuleWatcher가 안 꽂혀있으면 에러 로그를 띄웁니다.
            if (rule != null) rule.MarkSatisfied();
            else Debug.LogError($"<color=red>[LureVoice]</color> {gameObject.name}의 Rule 필드가 인스펙터에 연결되지 않았습니다! 연결해 주세요.");

            return;
        }

        Debug.Log($"<color=cyan>[LureVoice 대사 {currentLineIndex + 1}/{promptLines.Length}]</color> <b>{promptLines[currentLineIndex]}</b>");
        Debug.Log("<color=yellow>[선택지]</color> 0: [대답하지 않는다] / 1: 대답한다");

        // 콜백 함수 미리 정의
        Action<int> onSelectAction = i =>
        {
            if (i == 0)
            {
                currentLineIndex++;
                ShowNextChoice();
            }
            else
            {
                if (controller != null) controller.Fail(FailReason.Caught);
            }
        };

        DialogueSystem.Instance.ShowChoice(new[] { "[대답하지 않는다]", "대답한다" }, onSelect: onSelectAction);

        // UI 임시 시뮬레이터 (자동 진행)
        Debug.Log("<color=green>[UI 임시 시뮬레이터]</color> 자동으로 0번(대답하지 않는다)을 선택합니다.");
        onSelectAction.Invoke(0);
    }

    public void Tick() { }
    public void Neutralize() => IsNeutralized = true;
    public void SetProgress(float t) { }
}