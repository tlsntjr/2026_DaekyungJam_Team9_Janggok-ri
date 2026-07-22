using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 양식장 "가라앉는 폐품 블럭" — 그리드로 깔아 두는 발판.
/// 밟으면 삐걱거리다(sinkDelay) 바다에 가라앉고(sinkDuration), 잠시 잠겨 있다가(sunkDuration) 다시 떠오른다(riseDuration).
/// 완전히 잠기는 순간 위에 서 있으면 즉사. 잠겨 있는 동안 그 자리(열린 바다)에 들어와도 즉사.
///
/// 셋업: 블럭 프리팹 루트에 이 컴포넌트 + Collider2D(Is Trigger — 발판 영역과 일치시킬 것).
/// 가라앉는 연출(어두워짐·투명화·경고 흔들림·둥둥 떠 있음)은 코드가 SpriteRenderer로 직접 처리 — 별도 애니메이션 불필요.
/// 블럭 "사이"의 맨 바다까지 즉사로 만들려면 바다 전체에 WaterKillZone을 한 장 깔면 됨
/// (블럭 위에 서 있는 동안은 IsAnyBlockSafeAt 판정으로 안전 처리됨).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SinkingBlock : MonoBehaviour
{
    private enum State { Floating, Warning, Sinking, Sunk, Rising }

    [Header("타이밍")]
    [SerializeField] private float sinkDelay = 1.6f;      // 밟은 뒤 가라앉기 시작까지 (경고 흔들림 구간)
    [SerializeField] private float sinkDuration = 0.9f;   // 가라앉는 데 걸리는 시간 — 끝나는 순간이 즉사 판정
    [SerializeField] private float sunkDuration = 3.5f;   // 잠겨 있는 시간
    [SerializeField] private float riseDuration = 1.2f;   // 다시 떠오르는 시간

    [Header("애니메이션 A — Animator 클립 (경고 흔들림이 끝난 뒤 재생. 프레임 배열보다 우선)")]
    [SerializeField] private Animator animator;                   // 파이프의 Animator (비우면 자식에서 자동 탐색, 없으면 스킵). Idle 동안은 꺼두고 일반 이미지 사용
    [SerializeField] private string sinkDownState = "SinkDown";   // 가라앉는 클립의 스테이트 이름 — 재생 속도가 sinkDuration에 자동으로 맞춰짐
    [SerializeField] private string riseUpState = "RiseUp";       // 떠오르는 클립의 스테이트 이름 — riseDuration에 자동으로 맞춰짐

    [Header("애니메이션 B — 스프라이트 프레임 직접 등록 (Animator 없을 때. 역시 duration에 자동 신축)")]
    [SerializeField] private Sprite[] sinkDownFrames;   // 가라앉는 애니메이션. 마지막 프레임이 '잠긴 모습'으로 유지됨 (투명 프레임이면 안 보임)
    [SerializeField] private Sprite[] riseUpFrames;     // 떠오르는 애니메이션. 끝나면 배치해둔 기본(Idle) 스프라이트로 복귀
    // 둘 다 비우면 아래 색·위치 연출(코드 폴백)로 동작

    [Header("연출 (프레임 애니메이션 미등록 시 폴백)")]
    [SerializeField] private Color submergedTint = new Color(0.2f, 0.32f, 0.45f, 0f);  // 완전히 잠겼을 때 색 (알파 0 = 안 보임)
    [SerializeField] private float warningShake = 0.06f;   // 경고 구간 흔들림 진폭 (점점 거세짐)
    [SerializeField] private float sinkVisualDrop = 0.15f; // 가라앉을 때 스프라이트가 아래로 밀리는 거리
    [SerializeField] private float idleBobAmount = 0.03f;  // 평상시 둥둥 떠 있는 미세 움직임 (0 = 끔)
    [SerializeField] private float idleBobSpeed = 1.2f;

    [Header("사운드 (비우면 스킵)")]
    [SerializeField] private EventReference creakSfx;   // 밟았을 때 삐걱
    [SerializeField] private EventReference sinkSfx;    // 잠기는 순간 첨벙
    [SerializeField] private EventReference riseSfx;    // 다시 떠오르는 순간

    private State state = State.Floating;
    private Collider2D blockCollider;
    private Transform visualT;
    private Vector3 visualBaseLocal;
    private SpriteRenderer mainSr;      // 프레임 애니메이션 재생 대상 (첫 번째 SpriteRenderer)
    private Sprite idleSprite;          // 배치해둔 기본 이미지 — 떠오른 뒤 복귀용
    private SpriteRenderer[] srs;
    private Color[] baseColors;
    private bool playerInside;
    private Coroutine cycle;
    private float bobSeed;

    // ── 전 블럭 공용 안전 판정 (WaterKillZone이 사용) ──
    private static readonly List<SinkingBlock> all = new List<SinkingBlock>();

    /// <summary>해당 지점이 "떠 있는(밟을 수 있는)" 블럭 위인가 — 가라앉는 중/떠오르는 중도 안전으로 취급</summary>
    public static bool IsAnyBlockSafeAt(Vector2 pos)
    {
        for (int i = 0; i < all.Count; i++)
        {
            var b = all[i];
            if (b.state != State.Sunk && b.blockCollider != null && b.blockCollider.OverlapPoint(pos))
                return true;
        }
        return false;
    }

    private void Awake()
    {
        blockCollider = GetComponent<Collider2D>();

        var sr = GetComponentInChildren<SpriteRenderer>();
        visualT = sr != null ? sr.transform : transform;
        visualBaseLocal = visualT.localPosition;
        mainSr = sr;
        idleSprite = sr != null ? sr.sprite : null;

        // Animator는 재생 순간에만 켠다 — Idle 동안 켜져 있으면 기본 스테이트가 멋대로 재생되어
        // "평상시엔 일반 이미지" 원칙이 깨짐
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = false;

        srs = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) baseColors[i] = srs[i].color;

        bobSeed = (transform.position.x * 7.31f + transform.position.y * 3.17f) % (Mathf.PI * 2f);   // 블럭마다 물결 위상 어긋나게
    }

    private void OnEnable() => all.Add(this);

    private void OnDisable()
    {
        all.Remove(this);
        // 코루틴은 조용히 죽지만 시각 상태는 남음 — 재활성화 시 반쯤 잠긴 채 시작하는 것 방지
        if (cycle != null) { StopCoroutine(cycle); cycle = null; }
        state = State.Floating;
        playerInside = false;
        ApplySubmergence(0f);
        visualT.localPosition = visualBaseLocal;
        if (animator != null) { animator.speed = 1f; animator.enabled = false; }
        if (mainSr != null && idleSprite != null) mainSr.sprite = idleSprite;   // 애니메이션 도중 꺼졌어도 기본 이미지로
    }

    private void Update()
    {
        // 평상시 둥둥 — "물 위에 떠 있는 물건"이라는 정보 전달 (가라앉을 수 있다는 복선)
        if (state == State.Floating && idleBobAmount > 0f)
            visualT.localPosition = visualBaseLocal
                + Vector3.up * (Mathf.Sin(Time.time * idleBobSpeed + bobSeed) * idleBobAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;

        if (state == State.Floating)
        {
            Trigger();
        }
        else if (state == State.Sunk)
        {
            // 블럭이 잠겨 있는 자리 = 열린 바다 — 걸어 들어오면 빠져 죽음
            Debug.Log("<color=red>[SinkingBlock]</color> 잠긴 블럭 자리(맨 바다)에 진입 — 즉사");
            EventBus.RaisePlayerDeath();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void Trigger()
    {
        if (cycle == null) cycle = StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        // ── 1) 경고 — 삐걱거리며 점점 거세게 흔들림 ("지금 떠나라"는 텔레그래프) ──
        state = State.Warning;
        if (!creakSfx.IsNull) SoundManager.Instance.PlayOneShot(creakSfx, transform.position);

        float t = 0f;
        while (t < sinkDelay)
        {
            t += Time.deltaTime;
            float power = warningShake * Mathf.Lerp(0.3f, 1f, t / sinkDelay);
            visualT.localPosition = visualBaseLocal + (Vector3)(Random.insideUnitCircle * power);
            yield return null;
        }
        visualT.localPosition = visualBaseLocal;

        // ── 2) 가라앉기 ──
        state = State.Sinking;
        if (!sinkSfx.IsNull) SoundManager.Instance.PlayOneShot(sinkSfx, transform.position);

        if (animator != null)
            yield return PlayAnimatorState(sinkDownState, sinkDuration);   // 마지막 포즈가 잠긴 동안 유지됨
        else if (HasFrames(sinkDownFrames))
            yield return PlayFrames(sinkDownFrames, sinkDuration);
        else
            yield return LerpSubmerge(0f, 1f, sinkDuration);

        // ── 3) 완전 잠김 — 이 순간 위에 있으면 같이 가라앉음 ──
        state = State.Sunk;
        if (playerInside)
        {
            Debug.Log("<color=red>[SinkingBlock]</color> 블럭과 함께 가라앉음 — 즉사");
            EventBus.RaisePlayerDeath();
        }

        yield return new WaitForSeconds(sunkDuration);

        // ── 4) 다시 떠오르기 ──
        state = State.Rising;
        if (!riseSfx.IsNull) SoundManager.Instance.PlayOneShot(riseSfx, transform.position);

        if (animator != null)
        {
            yield return PlayAnimatorState(riseUpState, riseDuration);
            animator.enabled = false;                         // Idle은 다시 일반 이미지
            if (mainSr != null) mainSr.sprite = idleSprite;
        }
        else if (HasFrames(riseUpFrames))
        {
            yield return PlayFrames(riseUpFrames, riseDuration);
            if (mainSr != null) mainSr.sprite = idleSprite;   // 평상시(Idle) 이미지로 복귀
        }
        else
        {
            yield return LerpSubmerge(1f, 0f, riseDuration);
        }

        state = State.Floating;
        cycle = null;

        // 떠오르는 자리에서 계속 기다리고 있었다면 "다시 밟은 것"과 같음 — 바로 다음 사이클 (무한 캠핑 방지)
        if (playerInside) Trigger();
    }

    private static bool HasFrames(Sprite[] frames) => frames != null && frames.Length > 0;

    /// <summary>
    /// Animator 스테이트를 duration에 맞춰 재생 — 클립 원래 길이와 무관하게
    /// 배속(클립 길이 ÷ duration)을 걸어 게임플레이 타이밍(즉사 판정 순간)과 정확히 일치시킨다.
    /// 클립은 Loop Time을 꺼둘 것 (켜져 있으면 잠긴 동안 계속 반복됨).
    /// </summary>
    private IEnumerator PlayAnimatorState(string stateName, float duration)
    {
        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(stateName, 0, 0f);
        yield return null;   // Play가 실제 스테이트에 반영되는 프레임까지 대기

        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        animator.speed = (clipLength > 0.01f && duration > 0.01f) ? clipLength / duration : 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        animator.speed = 1f;
    }

    /// <summary>등록된 프레임을 duration에 맞춰 균등 재생 — 프레임 수와 무관하게 항상 시간에 딱 맞음</summary>
    private IEnumerator PlayFrames(Sprite[] frames, float duration)
    {
        if (mainSr == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            int idx = Mathf.Min(frames.Length - 1, (int)(Mathf.Clamp01(t / duration) * frames.Length));
            if (frames[idx] != null) mainSr.sprite = frames[idx];
            yield return null;
        }
        if (frames[frames.Length - 1] != null) mainSr.sprite = frames[frames.Length - 1];
    }

    private IEnumerator LerpSubmerge(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            ApplySubmergence(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        ApplySubmergence(to);
    }

    /// <summary>잠김 정도(0=떠 있음, 1=완전 잠김) 시각 반영 — 색이 물색으로 가라앉고 살짝 아래로 밀림</summary>
    private void ApplySubmergence(float s)
    {
        for (int i = 0; i < srs.Length; i++)
            if (srs[i] != null) srs[i].color = Color.Lerp(baseColors[i], submergedTint, s);

        visualT.localPosition = visualBaseLocal + Vector3.down * (sinkVisualDrop * s);
    }
}
