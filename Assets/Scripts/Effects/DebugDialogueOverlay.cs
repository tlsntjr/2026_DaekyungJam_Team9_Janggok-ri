using UnityEngine;

/// <summary>
/// 테스트 전용: DialogueSystem이 쏘는 대사를 Canvas 없이 OnGUI로 화면 좌하단에 바로 띄워준다.
/// 진짜 DialogueUI(타이핑 효과·선택지 포함)는 UI/DialogueUI.cs를 Canvas에 구성해서 써야 함.
/// </summary>
public class DebugDialogueOverlay : MonoBehaviour
{
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private int fontSize = 48;

    private string currentLine;
    private float hideAt;

    // Start()는 씬의 모든 오브젝트 Awake()가 끝난 뒤 호출이 보장되므로,
    // DialogueSystem.Instance가 아직 세팅되기 전에 구독 시도하는 초기화 순서 문제를 피할 수 있다.
    private void Start()   => DialogueSystem.Instance.OnShowLine += HandleShowLine;
    private void OnDestroy() => DialogueSystem.Instance.OnShowLine -= HandleShowLine;

    private void HandleShowLine(string line)
    {
        currentLine = line;
        hideAt = Time.time + displayDuration;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(currentLine) || Time.time > hideAt) return;

        GUI.skin.label.fontSize = fontSize;
        GUI.skin.box.fontSize = fontSize;

        Rect box = new Rect(20, Screen.height - fontSize * 2, Screen.width - 40, fontSize * 1.5f);
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.Box(box, GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(box, currentLine);
    }
}
