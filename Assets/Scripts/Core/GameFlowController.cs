using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 전체 진행 배선: 괴담 클리어 → 다음 씬 전환.
/// 인스펙터의 매핑 테이블(huntId → 씬 이름)만 채우면 코드 수정 없이 진행 순서 조정 가능.
/// Managers 오브젝트에 부착 (DontDestroyOnLoad로 씬을 넘어 유지됨).
///
/// 잼 빌드 진행 예시:
///   mudflat  → SCENE_FISH_FARM
///   fishfarm → SCENE_ENDING
/// </summary>
public class GameFlowController : MonoBehaviour
{
    [System.Serializable]
    public class Route
    {
        public string huntId;         // 클리어된 괴담 id (HauntDefinition.huntId)
        public string nextSceneName;  // 클리어 시 이동할 씬 이름 (Build Settings 등록 필수)
    }

    public static GameFlowController Instance { get; private set; }

    [Header("괴담 클리어 → 다음 씬 매핑")]
    [SerializeField] private Route[] routes;

    [Header("클리어 후 전환까지 여유 (클리어 사운드/연출이 들릴 시간)")]
    [SerializeField] private float transitionDelay = 1.5f;

    private bool transitioning;   // 중복 전환 방지

    private void Awake()
    {
        // 중복이면 "이 컴포넌트만" 제거 — 오브젝트째 파괴하면 같은 오브젝트에 탄
        // 씬-로컬 매니저들(대사·오염 등)까지 같이 죽어 씬 기능이 전멸함
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  => EventBus.OnHauntCleared += HandleHauntCleared;
    private void OnDisable() => EventBus.OnHauntCleared -= HandleHauntCleared;

    private void HandleHauntCleared(string huntId)
    {
        if (transitioning) return;

        foreach (var route in routes)
        {
            if (route.huntId != huntId) continue;

            transitioning = true;
            StartCoroutine(TransitionAfterDelay(route.nextSceneName));
            return;
        }

        // 매핑에 없는 괴담 클리어 — 진행에는 영향 없지만 배선 누락일 수 있으니 로그
        Debug.LogWarning($"[GameFlow] '{huntId}' 클리어됐지만 다음 씬 매핑이 없음");
    }

    private IEnumerator TransitionAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneFlow.Instance.FadeAndLoad(sceneName);
        transitioning = false;   // 다음 씬의 괴담 클리어를 받을 수 있게 해제
    }
}
