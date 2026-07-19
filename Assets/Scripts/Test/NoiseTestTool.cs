using UnityEngine;
using System.Collections;

/// <summary>
/// G 키를 눌러 특정 위치에 강제로 소음 이벤트를 발생시키는 디버그 도구입니다.
/// 귀신 AI의 소음 감지 로직을 테스트할 때 사용하세요.
/// </summary>
public class NoiseTestTool : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private float noiseRadius = 5f;
    [SerializeField] private Color debugColor = Color.yellow;

    private void Update()
    {
        // G 키를 누르면 마우스 커서 위치에 강제 소음 발생
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (Camera.main == null)
            {
                Debug.LogError("[NoiseTestTool] 메인 카메라를 찾을 수 없습니다.");
                return;
            }

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            Debug.Log($"<color=yellow>[NoiseTest]</color> 강제 소음 발생! 위치: {mousePos}, 반경: {noiseRadius}");

            // 프로젝트의 전역 이벤트 버스를 통해 소음 방송
            EventBus.RaiseNoiseEmitted(mousePos, noiseRadius);
        }
    }

    // 소음 범위를 씬 뷰에서 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        Gizmos.color = debugColor;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // 마우스 위치에 소음 반경을 원으로 표시
        Gizmos.DrawWireSphere(mousePos, noiseRadius);
    }
}