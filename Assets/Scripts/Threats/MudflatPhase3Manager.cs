using UnityEngine;
using System.Collections;

public class MudflatPhase3Manager : MonoBehaviour, IThreatBehavior
{
    [SerializeField] private DialogueSystem dialogueSystem; // 에디터에서 할당
    [SerializeField] private HauntController hauntController; // 에디터에서 할당

    private bool isNeutralized = false;
    public bool IsNeutralized => isNeutralized;

    public void Activate()
    {
        isNeutralized = false;
        StartCoroutine(RunRapidPrompts());
    }

    public void Tick() { /* 타이머 및 밀물 오염도는 HauntController가 담당 */ }

    public void Neutralize()
    {
        isNeutralized = true;
        StopAllCoroutines(); // 페이즈 종료 시 질문창 루틴 강제 종료
    }

    public void SetProgress(float t) { }

    private IEnumerator RunRapidPrompts()
    {
        for (int i = 0; i < 3; i++)
        {
            // 질문창 시작 단계 확인
            Debug.Log($"<color=blue>[3페이즈]</color> {i + 1}번째 질문창 시작!");

            bool isWaiting = true;
            bool success = false;

            dialogueSystem.ShowRapidPrompt(3.0f,
                onSurvive: () => {
                    Debug.Log($"<color=green>[3페이즈]</color> {i + 1}번째 질문창 성공!");
                    success = true;
                    isWaiting = false;
                },
                onFail: () => {
                    Debug.Log($"<color=red>[3페이즈]</color> {i + 1}번째 질문창 실패!");
                    hauntController.Fail(FailReason.Caught);
                    isWaiting = false;
                }
            );

            yield return new WaitUntil(() => !isWaiting);

            if (!success) yield break;
        }
        Debug.Log("<color=purple>[3페이즈]</color> 모든 질문창 통과 완료!");
    }
}