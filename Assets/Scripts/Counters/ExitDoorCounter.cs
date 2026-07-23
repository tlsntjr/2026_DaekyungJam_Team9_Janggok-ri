using UnityEngine;

public class ExitDoorCounter : MonoBehaviour, IInteractable, ICounterCondition
{
    [Header("철거 문구")]
    [SerializeField] private string promptMessage = "철거된 문을 통해 탈출";

    [Header("상호작용 시 대사 (한 칸 = 한 줄, 비우면 스킵)")]
    [SerializeField, TextArea(2, 4)] private string[] resultLines = { "...끝났다. 이제 정말 끝났나." };

    [Header("완료 시 괴담 종료 처리 (State가 ObjectiveReady일 때만 실제로 씬 전환됨)")]
    [SerializeField] private HauntController haunt;

    private bool isDoorOpened = false;
    public bool IsSatisfied => isDoorOpened;

    public string Prompt => isDoorOpened ? "이미 열린 문" : promptMessage;
    public string InteractKey => "E";

    public void Interact()
    {
        if (isDoorOpened) return;

        isDoorOpened = true;

        Debug.Log("<color=cyan>[탈출 완료]</color> 플레이어가 출구 철문 상호작용 완료! IsSatisfied가 true로 전환됩니다.");

        if (resultLines != null && resultLines.Length > 0 && DialogueSystem.Instance != null)
            DialogueSystem.Instance.ShowSequence(resultLines, () => haunt?.CompleteHaunt());
        else
            haunt?.CompleteHaunt();

        gameObject.SetActive(false);
    }
}