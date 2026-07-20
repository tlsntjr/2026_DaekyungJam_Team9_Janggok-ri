using UnityEngine;

/// <summary>
/// 스토리 대사 트리거 — 플레이어가 밟으면 연속 대사(시퀀스)를 출력한다.
/// Trigger Collider2D가 있는 오브젝트에 부착 (EntryTrigger와 같은 배치 방식).
/// 대사는 인스펙터에서 줄 단위로 작성 — 클릭할 때마다 다음 줄로 넘어감.
/// 시퀀스가 끝나면 선택적으로 목표 플래그를 세울 수 있어 "이 대사를 본 것"을 진행 조건으로 쓸 수 있다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StoryTrigger : MonoBehaviour
{
    [Header("대사 (한 칸 = 한 줄, 클릭으로 다음 줄)")]
    [SerializeField, TextArea(2, 4)] private string[] lines;

    [Header("옵션")]
    [SerializeField] private bool once = true;                 // 1회성 (false면 밟을 때마다 재생)
    [SerializeField] private string objectiveFlagId;           // 시퀀스 종료 시 세울 목표 플래그 (비우면 스킵)
    [SerializeField] private string playerTag = "Player";

    private bool fired;
    private bool playing;   // 시퀀스 재생 중 재진입 방지

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        if (playing || (once && fired)) return;
        if (lines == null || lines.Length == 0) return;

        fired = true;
        playing = true;

        DialogueSystem.Instance.ShowSequence(lines, () =>
        {
            playing = false;

            if (!string.IsNullOrEmpty(objectiveFlagId))
                ObjectiveSystem.Instance.SetFlag(objectiveFlagId);
        });
    }
}
