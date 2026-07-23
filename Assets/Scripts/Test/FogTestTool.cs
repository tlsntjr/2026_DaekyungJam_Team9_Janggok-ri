using UnityEngine;

/// <summary>
/// 안개 연출 테스트용 도구. ScreamTestTool과 같은 방식.
///   N 키 : 전역 안개 +0.1 (오염도 상승 흉내)
///   M 키 : 전역 안개 -0.1
///   B 키 : 구역 진입/이탈 토글 — simulatedZone에 씬의 FogZone을 연결해두면
///          걸어 들어가지 않고도 구역 등록 경로(전역과의 max 우선순위 포함)를 확인 가능
/// 구역 트리거 자체(콜라이더 감지)는 실제로 걸어 들어가서 확인할 것.
/// 테스트 끝나면 씬에서 제거할 것.
/// </summary>
public class FogTestTool : MonoBehaviour
{
    [Header("B 키로 진입/이탈을 흉내낼 FogZone (선택)")]
    [SerializeField] private FogZone simulatedZone;

    private float ambientLevel;
    private bool zoneOn;

    private void Update()
    {
        if (FogDirector.Instance == null)
        {
            if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.B))
                Debug.LogError("[FogTest] 씬에 FogDirector가 없습니다.");
            return;
        }

        // N/M 키: 전역(오염도) 안개 레벨 조절
        if (Input.GetKeyDown(KeyCode.N)) AdjustAmbient(+0.1f);
        if (Input.GetKeyDown(KeyCode.M)) AdjustAmbient(-0.1f);

        // B 키: 구역 진입/이탈 흉내
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (simulatedZone == null)
            {
                Debug.LogError("[FogTest] simulatedZone이 비어 있습니다. 씬의 FogZone을 연결해주세요.");
                return;
            }

            zoneOn = !zoneOn;
            if (zoneOn) FogDirector.Instance.EnterZone(simulatedZone);
            else        FogDirector.Instance.ExitZone(simulatedZone);

            Debug.Log($"<color=cyan>[FogTest]</color> 구역 {(zoneOn ? $"진입 (농도 {simulatedZone.Density})" : "이탈")} — 전역 {ambientLevel:F1}과 큰 쪽이 반영되어야 정상");
        }
    }

    private void AdjustAmbient(float delta)
    {
        ambientLevel = Mathf.Clamp01(ambientLevel + delta);
        FogDirector.Instance.SetAmbientLevel(ambientLevel);
        Debug.Log($"<color=cyan>[FogTest]</color> 전역 안개 레벨: {ambientLevel:F1}");
    }
}
