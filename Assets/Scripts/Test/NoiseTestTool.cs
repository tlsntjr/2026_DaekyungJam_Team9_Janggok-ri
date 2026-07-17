using UnityEngine;

public class NoiseTestTool : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private float noiseRadius = 5f;
    [SerializeField] private Color debugColor = Color.yellow;

    void Update()
    {
        // G 키를 누르면 마우스 위치에 소음 발생
        if (Input.GetKeyDown(KeyCode.G))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            Debug.Log($"<color=yellow>[NoiseTest]</color> 강제 소음 발생! 위치: {mousePos}, 반경: {noiseRadius}");

            // 핵심: 프로젝트의 EventBus를 통해 소음 이벤트 발생
            EventBus.RaiseNoiseEmitted(mousePos, noiseRadius);
        }
    }

    // 소음 범위를 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = debugColor;
        Vector3 mousePos = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : transform.position;
        mousePos.z = 0f;
        Gizmos.DrawWireSphere(mousePos, noiseRadius);
    }
}