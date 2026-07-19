using UnityEngine;

/// <summary>
/// 안개 구역 트리거. Trigger Collider2D가 있는 오브젝트에 부착 (Concealment와 같은 방식).
/// 플레이어가 들어오면 FogDirector에 이 구역의 안개 농도를 등록하고, 나가면 해제.
/// 실제 화면 반영(러프·다른 입력과의 우선순위)은 전부 FogDirector가 처리하므로
/// 이 컴포넌트는 "여기 안개 얼마나 짙은지"만 갖고 있음. 구역끼리 겹쳐도 안전.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FogZone : MonoBehaviour
{
    [Header("이 구역의 안개 농도 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float density = 0.7f;

    [SerializeField] private string playerTag = "Player";

    public float Density => density;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        if (FogDirector.Instance == null)
        {
            Debug.LogWarning("[FogZone] 씬에 FogDirector가 없어 안개 구역이 동작하지 않습니다.");
            return;
        }

        FogDirector.Instance.EnterZone(this);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag)) return;
        if (FogDirector.Instance == null) return;

        FogDirector.Instance.ExitZone(this);
    }

    // 플레이어가 안에 있는 채로 구역이 꺼지는 경우(페이즈 토글 등) 등록이 남지 않게 정리
    private void OnDisable()
    {
        if (FogDirector.Instance != null)
            FogDirector.Instance.ExitZone(this);
    }
}
