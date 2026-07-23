using UnityEngine;

/// <summary>
/// 갯벌 원혼의 "부름" 트리거 — 유혹 대사 → 선택지 → 결과/페널티 3단 흐름.
/// Trigger Collider2D가 있는 오브젝트에 부착 (StoryTrigger와 같은 배치 방식).
///
/// ICounterCondition 구현 — HauntController의 Counter Conditions 배열에 등록하면
/// "부름을 올바르게 넘긴 것"이 페이즈 클리어 조건이 된다 (EntrySafeZone 등과 조합 가능).
///
/// 흐름: 진입 → (고정 도입 대사) → 선택지 [안전 / 위험] (유혹 문구는 선택지 창의 질문으로 표시)
///       → 안전 선택: 만족(IsSatisfied) + 안전 결과 대사
///       → 위험 선택: 오염도 페널티 + 위험 결과 대사 → 잠시 후 다시 부름 (재시도 — 소프트락 방지)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GhostCallTrigger : MonoBehaviour, ICounterCondition
{
    [Header("도입 대사 (유혹 전에 나올 고정 줄들 — 비우면 생략, 첫 부름에만 표시)")]
    [SerializeField, TextArea(2, 4)] private string[] introLines;

    [Header("유혹 대사 풀 (무작위 1줄이 선택지 창의 질문으로 표시)")]
    [SerializeField, TextArea(2, 4)] private string[] luringPool = {
        "『얘, 우리랑 놀자.』",
        "『숨바꼭질 할 사람, 여기 붙어라.』",
        "『같이 놀아야지.』",
        "『왜 혼자 도망가?』",
    };

    [Header("선택지")]
    [SerializeField] private string safeOption = "...";
    [SerializeField] private string unsafeOption = "거긴 누구야?";
    [SerializeField] private bool shuffleOptions = true;   // 선택지 위치를 매번 랜덤 배치

    [Header("결과 대사")]
    [SerializeField, TextArea(2, 4)] private string[] safeResultLines = { "...대답하면 안 될 것 같다." };
    [SerializeField, TextArea(2, 4)] private string[] unsafeResultLines = { "...!", "숨이... 갑자기 무거워졌다." };

    [Header("위험 선택 페널티 (오염도 증가량, 0이면 대사만)")]
    [SerializeField, Range(0f, 1f)] private float contaminationPenalty = 0.2f;

    [Header("오답 후 다시 부르기까지의 대기 (재시도 간격)")]
    [SerializeField] private float retryDelay = 2.5f;

    [SerializeField] private string playerTag = "Player";

    private bool satisfied;
    private bool playing;
    private bool introShown;
    private float nextAllowedTime;

    /// <summary>올바른 선택으로 부름을 넘겼는가 — HauntController 페이즈 카운터용</summary>
    public bool IsSatisfied => satisfied;

    private void OnTriggerEnter2D(Collider2D collision) => TryBeginCall(collision);

    // 오답 후 존 안에 서 있어도 재시도가 걸리도록 Stay에서도 시도 (retryDelay가 간격을 통제)
    private void OnTriggerStay2D(Collider2D collision) => TryBeginCall(collision);

    private void TryBeginCall(Collider2D collision)
    {
        if (satisfied || playing) return;
        if (Time.time < nextAllowedTime) return;
        if (!collision.CompareTag(playerTag)) return;

        playing = true;

        string lure = luringPool != null && luringPool.Length > 0
            ? luringPool[Random.Range(0, luringPool.Length)]
            : null;

        // 도입 대사는 첫 부름에만 — 재시도 때 반복되면 늘어짐
        if (!introShown && introLines != null && introLines.Length > 0)
        {
            introShown = true;
            DialogueSystem.Instance.ShowSequence(introLines, () => ShowChoiceStep(lure));
        }
        else
        {
            ShowChoiceStep(lure);
        }
    }

    private void ShowChoiceStep(string lure)
    {
        bool swapped = shuffleOptions && Random.value < 0.5f;
        string[] options = swapped
            ? new[] { unsafeOption, safeOption }
            : new[] { safeOption, unsafeOption };

        DialogueSystem.Instance.ShowChoice(lure, options, index =>
        {
            bool choseSafe = (index == 0) != swapped;

            if (choseSafe)
            {
                satisfied = true;   // 페이즈 카운터 만족 — 이후 다시는 부르지 않음
                PlayResult(safeResultLines);
            }
            else
            {
                if (contaminationPenalty > 0f)
                    ContaminationSystem.Instance.Add(contaminationPenalty);

                nextAllowedTime = Time.time + retryDelay;   // 잠시 후 다시 부름
                PlayResult(unsafeResultLines);
            }
        });
    }

    private void PlayResult(string[] lines)
    {
        if (lines != null && lines.Length > 0)
            DialogueSystem.Instance.ShowSequence(lines, () => playing = false);
        else
            playing = false;
    }
}
