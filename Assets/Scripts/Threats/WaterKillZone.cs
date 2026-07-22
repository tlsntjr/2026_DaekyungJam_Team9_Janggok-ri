using FMODUnity;
using UnityEngine;

/// <summary>
/// 맨 바다 즉사 존 — 바다 전체를 덮는 큰 Collider2D(Is Trigger) 하나에 붙인다.
/// 플레이어가 "떠 있는 SinkingBlock 위"가 아닌 채로 존 안에 있으면 짧은 유예(graceTime) 후 즉사.
/// 유예는 블럭과 블럭 사이 경계를 스치며 건너는 한 프레임의 오판을 막기 위한 것.
/// SinkingBlock 없이 순수 즉사 물웅덩이로도 사용 가능 (블럭이 하나도 없으면 항상 위험 판정).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaterKillZone : MonoBehaviour
{
    [Header("판정")]
    [SerializeField] private float graceTime = 0.12f;   // 블럭 경계를 넘는 순간의 오판 방지 유예

    [Header("안전 지대 — 이 콜라이더 위에 있는 동안은 바다 위여도 안 죽음")]
    [SerializeField] private Collider2D[] safeAreas;    // 부두/다리 등 통행 타일맵의 TilemapCollider2D(Is Trigger 권장) 등록

    [Header("사운드 (비우면 스킵)")]
    [SerializeField] private EventReference splashSfx;  // 빠지는 순간 첨벙

    private float unsafeTimer;
    private bool killed;   // 사망 이벤트 중복 발화 방지 — 안전 지대 복귀 시 해제

    private void OnEnable()
    {
        unsafeTimer = 0f;
        killed = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector2 feet = other.bounds.center;
        if (SinkingBlock.IsAnyBlockSafeAt(feet) || IsInSafeArea(feet))
        {
            unsafeTimer = 0f;
            killed = false;   // 리스폰 후 다시 빠질 수 있게 래치 해제
            return;
        }

        if (killed) return;

        unsafeTimer += Time.deltaTime;
        if (unsafeTimer < graceTime) return;

        killed = true;
        if (!splashSfx.IsNull) SoundManager.Instance.PlayOneShot(splashSfx, other.transform.position);
        Debug.Log("<color=red>[WaterKillZone]</color> 맨 바다에 빠짐 — 즉사");
        EventBus.RaisePlayerDeath();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        unsafeTimer = 0f;
        killed = false;
    }

    /// <summary>부두/다리 등 등록된 안전 지대 콜라이더 위인가 — 타일맵 콜라이더도 OverlapPoint로 판정됨</summary>
    private bool IsInSafeArea(Vector2 pos)
    {
        if (safeAreas == null) return false;
        for (int i = 0; i < safeAreas.Length; i++)
            if (safeAreas[i] != null && safeAreas[i].OverlapPoint(pos)) return true;
        return false;
    }
}
