using TMPro;
using UnityEngine;

/// <summary>
/// 아이템 개수 HUD — 아이콘 + 개수만 간단히 표시 (갯벌 조개껍데기 등).
///
/// 셋업: HUD 캔버스 우측 하단에 이 컴포넌트를 붙인 오브젝트를 두고,
///       그 "자식"으로 묶음 오브젝트(아이콘 Image + 개수 TMP_Text) 하나 생성 →
///       묶음을 Root에, TMP를 Count Text에 연결. 아이콘 스프라이트는 Image에 직접 지정.
///       ※ Root는 반드시 자식이어야 함 — 자기 자신을 숨기면 인벤토리 이벤트를 다시 못 받아 영영 안 나타남.
///
/// 인벤토리 변경 이벤트로만 갱신 (Update 폴링 없음). 씬마다 배치, 다른 아이템도 id만 바꿔 재사용.
/// </summary>
public class ItemCountHUD : MonoBehaviour
{
    [Header("표시할 아이템 id (인벤토리 카탈로그와 일치)")]
    [SerializeField] private string itemId = "shell";

    [Header("참조")]
    [SerializeField] private GameObject root;      // 아이콘+텍스트 묶음 (자식 오브젝트 — 자기 자신 금지)
    [SerializeField] private TMP_Text countText;

    [Header("표시 옵션")]
    [SerializeField] private bool hideWhenEmpty = true;   // 0개면 통째로 숨김
    [SerializeField] private string format = "x{0}";      // {0} = 개수

    private bool subscribed;

    // 씬 로드 순서에 따라 InventorySystem이 늦게 깨어날 수 있어 양쪽에서 구독 시도 (DialogueUI와 같은 방어)
    private void OnEnable() { TrySubscribe(); Refresh(); }
    private void Start()    { TrySubscribe(); Refresh(); }

    private void TrySubscribe()
    {
        if (subscribed || InventorySystem.Instance == null) return;
        subscribed = true;
        InventorySystem.Instance.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        if (subscribed && InventorySystem.Instance != null)
            InventorySystem.Instance.OnChanged -= Refresh;
        subscribed = false;
    }

    private void Refresh()
    {
        int count = InventorySystem.Instance != null ? InventorySystem.Instance.GetCount(itemId) : 0;

        if (countText != null)
            countText.text = string.Format(format, count);

        if (root == null || root == gameObject) return;   // 자기 자신 숨김 금지 — 이벤트 수신이 끊김
        root.SetActive(!hideWhenEmpty || count > 0);
    }
}
