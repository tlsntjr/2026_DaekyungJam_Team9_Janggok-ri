using System.Collections;
using UnityEngine;

/// <summary>
/// SCENE_ENDING에 배치. 씬 진입 시 엔딩을 평가하고, 타입별 연출을 보여준 뒤
/// 일정 시간(또는 아무 키 입력) 후 로비 씬으로 돌아간다.
/// 엔딩 컷신 콘텐츠가 아직 없으면 endingVisuals를 비워두면 텍스트로 대체된다.
/// </summary>
public class EndingDirector : MonoBehaviour
{
    [Header("엔딩 평가")]
    [SerializeField] private EndingEvaluator evaluator;

    [Header("타입별 연출 오브젝트 (Bad=0 / Normal=1 / True=2, 비어있으면 텍스트 폴백)")]
    [SerializeField] private GameObject[] endingVisuals;

    [Header("로비 복귀")]
    [SerializeField] private string lobbySceneName = "SCENE_LOBBY";
    [SerializeField] private float autoReturnDelay = 8f;

    private bool returning;

    private void Start()
    {
        EndingType result = evaluator.Evaluate(ObjectiveSystem.Instance);
        GameManager.Instance.SetEnding();
        ShowEnding(result);
        StartCoroutine(ReturnToLobbyRoutine());
    }

    private void ShowEnding(EndingType type)
    {
        int index = (int)type;
        bool hasVisual = endingVisuals != null && index >= 0 && index < endingVisuals.Length && endingVisuals[index] != null;

        if (hasVisual)
        {
            for (int i = 0; i < endingVisuals.Length; i++)
                if (endingVisuals[i] != null)
                    endingVisuals[i].SetActive(i == index);
        }
        else
        {
            DialogueSystem.Instance.Show($"엔딩: {type}");
        }
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        float t = 0f;
        while (t < autoReturnDelay)
        {
            if (Input.anyKeyDown)
                break;
            t += Time.deltaTime;
            yield return null;
        }

        ReturnToLobby();
    }

    private void ReturnToLobby()
    {
        if (returning) return;
        returning = true;
        SceneFlow.Instance.FadeAndLoad(lobbySceneName);
    }
}
