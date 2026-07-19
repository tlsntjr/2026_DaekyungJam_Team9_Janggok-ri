using System.Collections;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 몬스터 괴성 연출 총괄. EventBus.OnMonsterScreamed(발원지) 수신 시:
///   1) 괴성 사운드 재생 (발원지 3D 위치 — 어느 방향에서 우는지 들림)
///   2) 플레이어와의 거리로 강도 감쇠 계산 (maxDistance 밖이면 통째로 무시)
///   3) 풀스크린 충격파 셰이더(_Center/_Radius/_Intensity) 구동 — 파동이 화면을 훑고 지나감
///   4) 근거리일수록 강한 카메라 셰이크
/// 씬마다 1개 배치. shockwaveMaterial은 Renderer 2D의 Full Screen Pass에 꽂힌 머티리얼 에셋과 같은 것이어야 함.
/// ※ 발화 측(FishMovement 등)은 EventBus.RaiseMonsterScreamed(transform.position) 한 줄만 호출하면 됨.
/// ※ 괴성 사운드를 MonsterVocalizer가 아니라 여기서 재생하는 이유:
///    OnMonsterScreamed엔 huntId가 없어서 몬스터별 Vocalizer가 구독하면 전원이 중복 재생됨.
///    씬에 하나뿐인 이 디렉터가 발원지 위치로 1회만 재생하는 게 안전.
/// </summary>
public class ScreamShockwaveDirector : MonoBehaviour
{
    [Header("충격파 머티리얼 (Full Screen Pass의 Pass Material과 동일 에셋)")]
    [SerializeField] private Material shockwaveMaterial;

    [Header("참조 (cam 비우면 Camera.main)")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform player;

    [Header("거리 감쇠")]
    [SerializeField] private float maxDistance = 15f;          // 이보다 멀면 연출 자체를 스킵
    [SerializeField] private float fullPowerDistance = 4f;     // 이보다 가까우면 최대 강도

    [Header("파동 연출")]
    [SerializeField] private float expandDuration = 0.7f;      // 링이 화면을 훑는 시간
    [SerializeField] private float maxRadius = 1.5f;           // 셰이더 _Radius 최종값 (1.5면 화면 밖까지 빠져나감)

    [Header("괴성 사운드 (비우면 스킵)")]
    [SerializeField] private EventReference screamSfx;

    [Header("카메라 셰이크")]
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float maxShakeMagnitude = 0.25f;  // 최대 강도(근거리)일 때의 흔들림 거리

    // 매 프레임 문자열 해싱을 피하기 위한 프로퍼티 ID 캐싱
    private static readonly int CenterId    = Shader.PropertyToID("_Center");
    private static readonly int RadiusId    = Shader.PropertyToID("_Radius");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private Coroutine waveCoroutine;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        ResetMaterial();   // 에디터에서 머티리얼 에셋에 남은 이전 실행 값 청소
    }

    private void OnEnable()  => EventBus.OnMonsterScreamed += HandleScream;

    private void OnDisable()
    {
        EventBus.OnMonsterScreamed -= HandleScream;
        ResetMaterial();   // SetFloat 값은 에디터에서 머티리얼 에셋에 영구 저장되므로 반드시 원복
    }

    private void HandleScream(Vector2 worldPos)
    {
        if (player == null) return;

        float distance = Vector2.Distance((Vector2)player.position, worldPos);
        if (distance > maxDistance) return;

        // fullPowerDistance 이내 = 1, maxDistance = 0 (InverseLerp가 알아서 0~1로 클램프)
        float power = Mathf.InverseLerp(maxDistance, fullPowerDistance, distance);

        if (!screamSfx.IsNull)
            SoundManager.Instance.PlayOneShot(screamSfx, worldPos);

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, maxShakeMagnitude * power);

        if (shockwaveMaterial != null && cam != null)
        {
            if (waveCoroutine != null) StopCoroutine(waveCoroutine);
            waveCoroutine = StartCoroutine(WaveRoutine(worldPos, power));
        }
    }

    private IEnumerator WaveRoutine(Vector2 worldPos, float power)
    {
        float t = 0f;
        while (t < expandDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / expandDuration);

            // 카메라가 플레이어를 따라 움직이므로 발원지의 뷰포트 좌표는 매 프레임 갱신
            Vector3 viewport = cam.WorldToViewportPoint(worldPos);
            shockwaveMaterial.SetVector(CenterId, new Vector4(viewport.x, viewport.y, 0f, 0f));

            // ease-out: 초반에 확 퍼지고 끝에서 잦아드는 실제 파동 느낌
            float radius = maxRadius * (1f - (1f - k) * (1f - k));
            shockwaveMaterial.SetFloat(RadiusId, radius);
            shockwaveMaterial.SetFloat(IntensityId, power * (1f - k));

            yield return null;
        }

        ResetMaterial();
        waveCoroutine = null;
    }

    private void ResetMaterial()
    {
        if (shockwaveMaterial == null) return;
        shockwaveMaterial.SetFloat(RadiusId, 0f);
        shockwaveMaterial.SetFloat(IntensityId, 0f);
    }
}
