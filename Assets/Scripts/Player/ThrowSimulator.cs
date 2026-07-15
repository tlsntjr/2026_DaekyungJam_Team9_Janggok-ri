using UnityEngine;

public class ThrowSimulator : MonoBehaviour
{
    [Header("테스트용 플레이어 설정")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float noiseRadius = 5f; // 조개껍데기 소음 반경

    void Update()
    {
        // 키보드 G키를 누르면 조개껍데기를 던진 소음을 시뮬레이션합니다.
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (playerTransform == null)
            {
                // 플레이어를 못 찾았다면 본인 위치 기준
                playerTransform = this.transform;
            }

            Vector2 noisePosition = playerTransform.position;

            Debug.Log($"<color=yellow>[ThrowSimulator]</color> <b>G 키 입력!</b> 플레이어 위치({noisePosition})에서 소음 반경 {noiseRadius}m짜리 조개껍데기 투척 소음을 방송합니다.");

            // 핵심: 전역 이벤트 버스에 소음 발생 방송을 쏩니다!
            EventBus.RaiseNoiseEmitted(noisePosition, noiseRadius);
        }
    }
}