using UnityEngine;

public class ExitDoorCounter : MonoBehaviour, IInteractable, ICounterCondition
{
    [Header("철문 설정")]
    [SerializeField] private string promptMessage = "철문을 열고 탈출";

    private bool isDoorOpened = false;
    public bool IsSatisfied => isDoorOpened;

    public string Prompt => isDoorOpened ? "이미 열린 문" : promptMessage;
    public string InteractKey => "E";

    public void Interact()
    {
        if (isDoorOpened) return;

        isDoorOpened = true;

        Debug.Log("<color=cyan>[3단계 완수]</color> 플레이어가 출구 철문 상호작용 성공! IsSatisfied가 true로 전송됩니다.");

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.Show("철문이 열렸다! 양식장을 탈출하는 데 성공했습니다!");
        }

        gameObject.SetActive(false);
    }
}