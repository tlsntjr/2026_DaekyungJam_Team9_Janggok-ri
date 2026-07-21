using FMODUnity;
using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [Header("Item id")]
    [SerializeField] private string itemId;

    [Header("획득 사운드 (비우면 스킵 — FMOD 이벤트는 2D 권장)")]
    [SerializeField] private EventReference pickupSfx;

    [Header("획득 직후 대사 (한 칸 = 한 줄, 클릭으로 다음 줄. 비우면 스킵 — 일지/편지류 아이템용)")]
    [SerializeField, TextArea(2, 4)] private string[] flavorLines;

    [Header("특정 줄 연출 (줄 번호에 맞춰 셰이크·글리치·효과음 — 비우면 스킵)")]
    [SerializeField] private DialogueLineEffect[] lineEffects;

    [Header("획득 시 활성화할 오브젝트 (손전등 시야 콘 등 해금형 — 비우면 스킵)")]
    [SerializeField] private GameObject[] activateOnPickup;

    [Header("획득 후 오브젝트 제거 여부 — 끄면 가구처럼 남고 재상호작용만 막힘 (신발장·책상용)")]
    [SerializeField] private bool hideAfterPickup = true;
    [SerializeField] private string collectedPrompt = "";   // 남겨둘 때 이후 프롬프트 (비우면 프롬프트 자체가 안 뜸)

    private bool collected;

    [Header("�ٽ� �������� ���")]
    [SerializeField] private string objectiveFlagId;

    [Header("������ �������� �����ϱ� ���� �������� ��� ����")]
    [SerializeField] private HauntController haunt;   // �� �������� ���� ������ ��ǥ���� ���� ����

    public string Prompt => collected ? collectedPrompt : "줍기";
    public string InteractKey => "E";
    /// <summary>
    /// ������ ��ȣ�ۿ�
    /// </summary>
    public void Interact()
    {
        if (collected) return;   // 유지형(hideAfterPickup=false)일 때 중복 획득 방지
        collected = true;

        if (!pickupSfx.IsNull)
            SoundManager.Instance.PlayOneShot(pickupSfx, transform.position);

        // 매니저가 없는 씬에서도 대사·해금 등 나머지 동작은 진행되도록 방어
        if (!string.IsNullOrEmpty(itemId))
        {
            if (InventorySystem.Instance != null) InventorySystem.Instance.Add(itemId);
            else Debug.LogWarning($"[Pickup] InventorySystem이 씬에 없어 '{itemId}' 추가를 건너뜀");
        }

        if (!string.IsNullOrEmpty(objectiveFlagId))
        {
            if (ObjectiveSystem.Instance != null) ObjectiveSystem.Instance.SetFlag(objectiveFlagId);
            else Debug.LogWarning($"[Pickup] ObjectiveSystem이 씬에 없어 '{objectiveFlagId}' 플래그를 건너뜀");
        }

        foreach (var target in activateOnPickup)
            if (target != null) target.SetActive(true);

        // 오브젝트 제거(→ItemTriggerCounter 만족 = 페이즈 전환)와 괴담 완료는 대사가 끝난 뒤에 —
        // 대사 도중에 다음 페이즈(추격 등)가 시작돼 읽기와 위협이 겹치는 것 방지
        if (flavorLines != null && flavorLines.Length > 0)
            DialogueSystem.Instance.ShowSequence(flavorLines,
                lineIndex => DialogueLineEffect.ApplyAll(lineEffects, lineIndex, transform.position),
                FinishPickup);
        else
            FinishPickup();
    }

    /// <summary>
    /// 획득 마무리 — 대사 시퀀스 종료 후(대사가 없으면 즉시) 실행.
    /// 여기서 오브젝트가 꺼지며 ItemTriggerCounter가 만족되고, haunt가 연결돼 있으면 괴담 완료.
    /// </summary>
    private void FinishPickup()
    {
        if (haunt != null)
            haunt.CompleteHaunt();

        if (hideAfterPickup)
            gameObject.SetActive(false);
    }
}