using FMODUnity;
using UnityEngine;

/// <summary>
/// 몬스터가 플레이어를 발견하고 추격을 시작하는 순간 괴성을 지른다.
/// 몬스터 오브젝트에 부착. huntId가 일치하는 ThreatState 이벤트만 반응.
/// (GhostAI가 EventBus.RaiseThreatStateChanged를 쏘면 자동 연동 — AI 없이도 Debug로 테스트 가능)
/// </summary>
public class MonsterVocalizer : MonoBehaviour
{
    [Header("이 몬스터의 괴담 id (HauntDefinition.huntId와 일치시킬 것)")]
    [SerializeField] private string huntId;

    [Header("발견 괴성")]
    [SerializeField] private EventReference screamSfx;
    [SerializeField] private float screamCooldown = 5f;   // 추격이 짧게 끊겼다 재개될 때 괴성 연발 방지

    private int lastState;
    private float lastScreamTime = -999f;

    private void OnEnable()  => EventBus.OnThreatStateChanged += HandleThreatState;
    private void OnDisable() => EventBus.OnThreatStateChanged -= HandleThreatState;

    private void HandleThreatState(string id, int state)
    {
        if (id != huntId) return;

        // "추격이 아니었다가 → 추격이 된" 전환 순간에만 발화
        bool startedChasing = state >= 2 && lastState < 2;
        lastState = state;

        if (!startedChasing) return;
        if (Time.time - lastScreamTime < screamCooldown) return;
        if (screamSfx.IsNull) return;

        lastScreamTime = Time.time;
        SoundManager.Instance.PlayOneShot(screamSfx, transform.position);   // 3D 위치 — 어느 방향에서 울부짖는지 들림
    }
}
