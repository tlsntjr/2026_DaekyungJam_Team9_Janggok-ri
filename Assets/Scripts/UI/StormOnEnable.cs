using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오브젝트가 켜지는 순간 팝업 폭풍을 시작하는 트리거 헬퍼. 두 가지 경로로 발동:
///   · Pickup의 Activate On Pickup에 연결 → 아이템 획득(녹음기 등) 순간 폭풍
///   · PhaseObjectToggler 하위에 배치 → 페이즈 시작(갯벌 P4 등) 순간 폭풍, 페이즈 끝나면 자동 정지
///
/// 모드:
///   Dummy — 더미 난사만, StopStorm(또는 이 오브젝트 꺼짐)까지 무한 (갯벌 귀환 러시용)
///   Full  — 더미 난사 → 진짜 팝업 + 제한시간 사망. 진짜 팝업의 모든 버튼은 "누르면 정리"로 자동 연결 (양식장용)
/// </summary>
public class StormOnEnable : MonoBehaviour
{
    public enum StormMode { Dummy, Full }

    [Header("폭풍 모드")]
    [SerializeField] private StormMode mode = StormMode.Dummy;

    [Header("Full 모드: 더미 난사 시간 (그 뒤 진짜 팝업 등장)")]
    [SerializeField] private float fullStormDuration = 4f;

    [Header("Dummy 모드: 글리치가 최대로 차오르는 시간")]
    [SerializeField] private float dummyRampDuration = 6f;

    [Header("이 오브젝트가 꺼질 때 폭풍도 정리 (페이즈 토글러 연동용)")]
    [SerializeField] private bool stopOnDisable = true;

    private void OnEnable() => StartCoroutine(BeginNextFrame());

    // 한 프레임 대기 — 씬 로드 직후 켜지는 경우 PopupStormDirector.Awake보다 먼저일 수 있음
    private IEnumerator BeginNextFrame()
    {
        yield return null;

        if (PopupStormDirector.Instance == null)
        {
            Debug.LogWarning("[StormOnEnable] 씬에 PopupStormDirector가 없어 폭풍을 시작할 수 없습니다.");
            yield break;
        }

        if (mode == StormMode.Dummy)
        {
            PopupStormDirector.Instance.StartDummyStorm(dummyRampDuration);
        }
        else
        {
            PopupStormDirector.Instance.StartStorm(fullStormDuration, popup =>
            {
                // 진짜 팝업의 모든 버튼 = 누르면 전체 정리 (제한시간 내 못 누르면 디렉터가 사망 처리)
                foreach (var button in popup.GetComponentsInChildren<Button>(true))
                    button.onClick.AddListener(() => PopupStormDirector.Instance.StopStorm());
            });
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable && PopupStormDirector.Instance != null)
            PopupStormDirector.Instance.StopStorm();
    }
}
