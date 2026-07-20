using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 옛날 윈도우 오류창 스타일 팝업 폭풍 연출. 캔버스 아래 배치 (씬 싱글톤).
///
/// 흐름: StartStorm(시간, 콜백)
///       → 랜덤 앵커 지점에서 더미 오류창이 "계단식으로 주르르륵" 쌓임 (회전 없음, 클릭 통과)
///       → 클러스터가 차면 다른 앵커 지점으로 점프해서 또 주르르륵
///       → 그동안 패닉 글리치가 펄린 노이즈로 출렁이며, 팝업이 쌓일수록 점점 강해짐
///       → 시간이 끝나면 "진짜 팝업" 하나가 화면 중앙 최상단에 출력 → 콜백으로 인스턴스 전달
///       → 처리 후 호출자가 StopStorm()으로 전부 정리.
///
/// 사용 예:
///   PopupStormDirector.Instance.StartStorm(4f, popup => {
///       // popup 안의 버튼에 원하는 동작 연결. 닫을 땐 PopupStormDirector.Instance.StopStorm();
///   });
/// </summary>
public class PopupStormDirector : MonoBehaviour
{
    public static PopupStormDirector Instance { get; private set; }

    [Header("더미 팝업 프리팹 (장식용 오류창 — 여러 종류를 넣으면 랜덤 선택)")]
    [SerializeField] private GameObject[] dummyPopupPrefabs;

    [Header("진짜 팝업 프리팹 (폭풍 끝에 하나 출력 — 버튼 동작은 StartStorm 콜백에서 연결)")]
    [SerializeField] private GameObject finalPopupPrefab;

    [SerializeField] private RectTransform spawnArea;    // 비우면 이 오브젝트의 RectTransform 기준

    [Header("난사 리듬")]
    [SerializeField] private float spawnIntervalMin = 0.08f;
    [SerializeField] private float spawnIntervalMax = 0.22f;
    [SerializeField] private int maxAliveDummies = 30;   // 초과 시 가장 오래된 것부터 제거 (성능 가드)

    [Header("계단식 캐스케이드")]
    [SerializeField] private Vector2 cascadeStep = new Vector2(34f, -26f);   // 한 장마다 밀리는 오프셋 (우하단 계단)
    [SerializeField] private int cascadeCountMin = 3;    // 한 지점에서 연달아 나오는 장수
    [SerializeField] private int cascadeCountMax = 6;
    [SerializeField] private Vector2 areaPadding = new Vector2(100f, 80f);   // 앵커 지점의 화면 가장자리 여유

    [Header("사운드 (FMOD 이벤트는 2D로 제작 — 비우면 스킵)")]
    [SerializeField] private EventReference popupSfx;        // 더미 오류창 하나 뜰 때마다 (윈도우 '띵!' — 멀티트랙 랜덤 추천)
    [SerializeField] private EventReference finalPopupSfx;   // 진짜 팝업 등장 순간 (더 무겁게)

    [Header("추격 BGM 연동 — 폭풍 동안 추격 음악·심장이 깔림 (비우면 스킵)")]
    [SerializeField] private string stormHuntId = "popup_storm";   // 가상의 위협 id — 실제 몬스터 huntId와 겹치지만 않으면 됨

    [Header("진짜 팝업 제한시간 — 시간 내 처리(StopStorm) 못 하면 오염도 최대 → 사망 (0이면 무제한)")]
    [SerializeField] private float finalPopupTimeLimit = 5f;

    [Header("패닉 글리치 — 출렁이며 점점 강해짐 (PanicGlitchDirector 없으면 스킵)")]
    [SerializeField, Range(0f, 1f)] private float glitchWobbleMin = 0.08f;   // 요동 하한 (초반)
    [SerializeField, Range(0f, 1f)] private float glitchWobbleMax = 0.45f;   // 요동 상한 (막판)
    [SerializeField] private float glitchWobbleSpeed = 2.5f;                 // 출렁임 속도
    [SerializeField, Range(0f, 1f)] private float spawnGlitchPulse = 0.25f;  // 더미 하나 뜰 때마다 찌직
    [SerializeField, Range(0f, 1f)] private float finalGlitchPulse = 0.7f;   // 진짜 팝업 등장 순간 강타

    private readonly List<GameObject> aliveDummies = new();
    private GameObject finalPopup;
    private Coroutine stormCoroutine;
    private Coroutine wobbleCoroutine;
    private Coroutine finalTimerCoroutine;
    private float stormProgress;   // 0→1, 글리치 요동의 세기 램프

    /// <summary>폭풍 진행 중 여부 (더미 난사 중 or 진짜 팝업 표시 중) — 투척 등 게임플레이 입력 차단 판정용</summary>
    public bool IsActive => stormCoroutine != null || finalPopup != null || aliveDummies.Count > 0;

    // 캐스케이드 상태
    private Vector2 cascadeAnchor;
    private int cascadeIndex;
    private int cascadeTarget;
    private float cascadeDirX;     // 클러스터마다 좌/우 계단 방향 랜덤

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable() => Cleanup();

    /// <summary>
    /// 팝업 폭풍 시작. duration 동안 더미를 난사한 뒤 진짜 팝업을 출력하고 onFinalPopup(인스턴스)을 호출.
    /// 진짜 팝업 처리 후엔 호출자가 StopStorm()으로 정리할 것.
    /// </summary>
    public void StartStorm(float duration, Action<GameObject> onFinalPopup = null)
    {
        StopAllStormCoroutines();
        Cleanup();   // 이전 폭풍 잔여물 정리 후 새로 시작

        cascadeIndex = 0;
        cascadeTarget = 0;   // 첫 스폰에서 새 앵커를 잡게 함

        // 폭풍 = 가상의 위협이 추격 중 — ChaseMusicController가 받아서 추격 BGM 시작
        // (심장박동 등 ThreatState 파라미터 연출도 같이 걸림)
        if (!string.IsNullOrEmpty(stormHuntId))
            EventBus.RaiseThreatStateChanged(stormHuntId, 2);

        stormCoroutine = StartCoroutine(StormRoutine(duration, onFinalPopup));
        wobbleCoroutine = StartCoroutine(GlitchWobbleRoutine());
    }

    /// <summary>폭풍 종료 — 더미·진짜 팝업 전부 정리 + 글리치 해제.</summary>
    public void StopStorm()
    {
        StopAllStormCoroutines();
        Cleanup();
    }

    private void StopAllStormCoroutines()
    {
        if (stormCoroutine != null) { StopCoroutine(stormCoroutine); stormCoroutine = null; }
        if (wobbleCoroutine != null) { StopCoroutine(wobbleCoroutine); wobbleCoroutine = null; }
        if (finalTimerCoroutine != null) { StopCoroutine(finalTimerCoroutine); finalTimerCoroutine = null; }
    }

    private IEnumerator StormRoutine(float duration, Action<GameObject> onFinalPopup)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            stormProgress = Mathf.Clamp01(elapsed / duration);   // 글리치 요동 세기 램프
            SpawnDummy();

            float wait = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            elapsed += wait;
            yield return new WaitForSeconds(wait);
        }

        stormProgress = 1f;
        stormCoroutine = null;
        SpawnFinalPopup(onFinalPopup);
    }

    /// <summary>
    /// 글리치가 펄린 노이즈로 부드럽게 출렁임 — stormProgress에 비례해 요동 폭이 커짐.
    /// 진짜 팝업이 떠 있는 동안에도 유지되다가 StopStorm에서 해제.
    /// </summary>
    private IEnumerator GlitchWobbleRoutine()
    {
        float seed = UnityEngine.Random.value * 100f;
        while (true)
        {
            if (PanicGlitchDirector.Instance != null)
            {
                float wobble = Mathf.PerlinNoise(Time.time * glitchWobbleSpeed, seed);   // 0~1 부드러운 요동
                float level = Mathf.Lerp(glitchWobbleMin, glitchWobbleMax, wobble) * Mathf.Lerp(0.4f, 1f, stormProgress);
                PanicGlitchDirector.Instance.SetBaseLevel(level);
            }
            yield return null;
        }
    }

    /// <summary>
    /// 장식용 더미 팝업 — 현재 앵커에서 계단식으로 밀려나며 출현, 클러스터가 차면 새 앵커로 점프.
    /// 버튼이 있어도 동작하지 않고 클릭도 통과 (진짜 팝업 조작을 방해하지 않게).
    /// </summary>
    private void SpawnDummy()
    {
        if (dummyPopupPrefabs == null || dummyPopupPrefabs.Length == 0) return;

        RectTransform parent = spawnArea != null ? spawnArea : transform as RectTransform;
        if (parent == null) return;

        // 클러스터 소진 → 새 앵커 지점 선정 + 계단 방향 랜덤 (좌하단/우하단)
        if (cascadeIndex >= cascadeTarget)
        {
            float rangeX = Mathf.Max(0f, parent.rect.width  * 0.5f - areaPadding.x);
            float rangeY = Mathf.Max(0f, parent.rect.height * 0.5f - areaPadding.y);
            cascadeAnchor = new Vector2(UnityEngine.Random.Range(-rangeX, rangeX),
                                        UnityEngine.Random.Range(-rangeY * 0.4f, rangeY));   // 아래로 흐를 공간 확보차 약간 위쪽 편향
            cascadeDirX = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            cascadeIndex = 0;
            cascadeTarget = UnityEngine.Random.Range(cascadeCountMin, cascadeCountMax + 1);
        }

        GameObject prefab = dummyPopupPrefabs[UnityEngine.Random.Range(0, dummyPopupPrefabs.Length)];
        GameObject dummy = Instantiate(prefab, parent);
        dummy.SetActive(true);

        if (dummy.transform is RectTransform rt)
        {
            Vector2 pos = cascadeAnchor + new Vector2(cascadeStep.x * cascadeDirX, cascadeStep.y) * cascadeIndex;

            // 화면 밖으로 계단이 새어나가지 않게 클램프
            float limX = Mathf.Max(0f, (parent.rect.width  - rt.rect.width)  * 0.5f - 8f);
            float limY = Mathf.Max(0f, (parent.rect.height - rt.rect.height) * 0.5f - 8f);
            pos.x = Mathf.Clamp(pos.x, -limX, limX);
            pos.y = Mathf.Clamp(pos.y, -limY, limY);

            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.identity;   // 회전 없음
        }

        cascadeIndex++;

        // 입력 통과 — 더미의 버튼·이미지가 클릭을 먹으면 진짜 팝업을 못 누름
        var group = dummy.GetComponent<CanvasGroup>();
        if (group == null) group = dummy.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        aliveDummies.Add(dummy);

        while (aliveDummies.Count > maxAliveDummies)
        {
            if (aliveDummies[0] != null) Destroy(aliveDummies[0]);
            aliveDummies.RemoveAt(0);
        }

        if (!popupSfx.IsNull)
            SoundManager.Instance.PlayOneShot(popupSfx,
                Camera.main != null ? Camera.main.transform.position : Vector3.zero);

        if (PanicGlitchDirector.Instance != null)
            PanicGlitchDirector.Instance.Pulse(spawnGlitchPulse);
    }

    /// <summary>
    /// 진짜 팝업 — 화면 정중앙 최상단. 버튼 연결은 호출자 콜백 몫.
    /// </summary>
    private void SpawnFinalPopup(Action<GameObject> onFinalPopup)
    {
        if (finalPopupPrefab == null)
        {
            Debug.LogWarning("[PopupStormDirector] finalPopupPrefab이 비어 있어 진짜 팝업 없이 종료합니다.");
            StopStorm();
            return;
        }

        RectTransform parent = spawnArea != null ? spawnArea : transform as RectTransform;
        if (parent == null) return;

        finalPopup = Instantiate(finalPopupPrefab, parent);
        finalPopup.SetActive(true);

        if (finalPopup.transform is RectTransform rt)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling();   // 더미들 위(최상단)에 표시
        }

        if (!finalPopupSfx.IsNull)
            SoundManager.Instance.PlayOneShot(finalPopupSfx,
                Camera.main != null ? Camera.main.transform.position : Vector3.zero);

        if (PanicGlitchDirector.Instance != null)
            PanicGlitchDirector.Instance.Pulse(finalGlitchPulse);   // 등장 순간 화면 강타

        onFinalPopup?.Invoke(finalPopup);

        // 제한시간 시작 — 호출자가 시간 내에 StopStorm()을 부르면 타이머도 함께 정지됨
        if (finalPopupTimeLimit > 0f)
            finalTimerCoroutine = StartCoroutine(FinalTimeoutRoutine());
    }

    /// <summary>
    /// 진짜 팝업 제한시간 초과 — 오염도를 최대로 밀어 사망 체인 발동.
    /// (ContaminationSystem이 100% 도달 시 OnPlayerDeath를 쏘고, DeathDirector가 사망 모션·사망 타이틀을 처리)
    /// </summary>
    private IEnumerator FinalTimeoutRoutine()
    {
        yield return new WaitForSeconds(finalPopupTimeLimit);

        Debug.Log("<color=red>[PopupStormDirector]</color> 진짜 팝업 제한시간 초과 — 오염도 최대, 사망 처리");

        Cleanup();   // 화면의 팝업·글리치·BGM부터 정리 (사망 연출에 자리를 비켜줌)
        finalTimerCoroutine = null;

        ContaminationSystem.Instance.Add(1f);   // 오염도 강제 최대 → OnPlayerDeath → DeathDirector 사망 연출
    }

    private void Cleanup()
    {
        stormProgress = 0f;

        foreach (var dummy in aliveDummies)
            if (dummy != null) Destroy(dummy);
        aliveDummies.Clear();

        if (finalPopup != null) { Destroy(finalPopup); finalPopup = null; }

        if (PanicGlitchDirector.Instance != null)
            PanicGlitchDirector.Instance.SetBaseLevel(0f);

        // 가상 위협 해제 — 추격 BGM 페이드아웃 (다른 실제 위협이 추격 중이면 컨트롤러가 알아서 유지)
        if (!string.IsNullOrEmpty(stormHuntId))
            EventBus.RaiseThreatStateChanged(stormHuntId, 0);
    }
}
