using System.Collections;
using UnityEngine;

/// <summary>
/// 방 단위 화면 마스킹. 지금 방을 이루는 사각형들(RoomZone의 Collider2D, 최대 4개)만 남기고
/// 나머지 화면을 셰이더로 가린다 — 타일맵은 그대로 하나로 두어도 됨.
///
/// 방 전환은 "이전 방 마스크 ↔ 새 방 마스크"를 크로스페이드하는 방식 (RoomBlend 0→1).
/// 도형 위치를 보간하지 않고 투명도만 섞으므로, 겹치는 영역(대부분의 화면)은 그대로 유지되고
/// 문간(경계) 부분만 부드럽게 넓어져 보임 — 화면 전체가 밝아지는 플래시 없이 자연스러운 전환.
///
/// 씬에 1개 배치. RoomMask 셰이더가 꽂힌 Full Screen Pass Renderer Feature와 짝을 이룸.
/// </summary>
public class RoomRevealDirector : MonoBehaviour
{
    public static RoomRevealDirector Instance { get; private set; }

    [Header("마스크 머티리얼 (Full Screen Pass의 Pass Material과 동일 에셋)")]
    [SerializeField] private Material maskMaterial;

    [Header("참조 (비우면 Camera.main)")]
    [SerializeField] private Camera cam;

    [Header("방 전환 크로스페이드 시간 (0이면 즉시 전환)")]
    [SerializeField] private float transitionDuration = 0.3f;

    [Header("시작 시 보여줄 방 (비우면 마스크 없이 시작 — 배치 전 테스트용)")]
    [SerializeField] private RoomZone startingRoom;

    private static readonly int RoomMinAId  = Shader.PropertyToID("_RoomMinA");
    private static readonly int RoomMaxAId  = Shader.PropertyToID("_RoomMaxA");
    private static readonly int RoomCountAId = Shader.PropertyToID("_RoomCountA");
    private static readonly int RoomMinBId  = Shader.PropertyToID("_RoomMinB");
    private static readonly int RoomMaxBId  = Shader.PropertyToID("_RoomMaxB");
    private static readonly int RoomCountBId = Shader.PropertyToID("_RoomCountB");
    private static readonly int RoomBlendId = Shader.PropertyToID("_RoomBlend");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    private readonly Vector4[] minBufA = new Vector4[RoomZone.MaxAreas];
    private readonly Vector4[] maxBufA = new Vector4[RoomZone.MaxAreas];
    private readonly Vector4[] minBufB = new Vector4[RoomZone.MaxAreas];
    private readonly Vector4[] maxBufB = new Vector4[RoomZone.MaxAreas];

    private RoomZone currentRoom;    // 새(목표) 방
    private RoomZone previousRoom;   // 전환 중인 이전 방 (전환 끝나면 의미 없어짐)
    private float blend = 1f;        // 0 = previousRoom, 1 = currentRoom
    private Coroutine blendCoroutine;
    private bool hasRoom;

    private void Awake()
    {
        // 씬 전환 후 살아남은 옛 인스턴스는 컴포넌트만 제거하고 현재 씬 것이 승계 (DDOL 좀비 방지)
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;

        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        if (startingRoom != null)
            SetRoom(startingRoom, instant: true);
        else if (maskMaterial != null)
            maskMaterial.SetFloat(IntensityId, 0f);   // 방 미배치 상태 — 마스크 없이 맵 전체 노출
    }

    private void OnDisable()
    {
        if (maskMaterial != null)
            maskMaterial.SetFloat(IntensityId, 0f);   // 에디터에서 머티리얼 에셋에 값이 남지 않도록 원복
    }

    /// <summary>지정한 방으로 전환. RoomZone이 진입 시 호출.</summary>
    public void SetRoom(RoomZone room, bool instant = false)
    {
        if (room == null || room == currentRoom) return;

        previousRoom = currentRoom;   // 첫 호출이면 null (Update에서 currentRoom으로 대체됨)
        currentRoom = room;
        hasRoom = true;

        if (instant || transitionDuration <= 0f || previousRoom == null)
        {
            blend = 1f;
            return;
        }

        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        blendCoroutine = StartCoroutine(BlendRoutine());
    }

    private IEnumerator BlendRoutine()
    {
        blend = 0f;
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            blend = Mathf.Clamp01(t / transitionDuration);
            yield return null;
        }
        blend = 1f;
        blendCoroutine = null;
    }

    private void Update()
    {
        if (!hasRoom || maskMaterial == null || cam == null || currentRoom == null) return;

        FillRects(currentRoom, minBufB, maxBufB, out int countB);
        RoomZone fromRoom = previousRoom != null ? previousRoom : currentRoom;
        FillRects(fromRoom, minBufA, maxBufA, out int countA);

        maskMaterial.SetVectorArray(RoomMinAId, minBufA);
        maskMaterial.SetVectorArray(RoomMaxAId, maxBufA);
        maskMaterial.SetFloat(RoomCountAId, countA);

        maskMaterial.SetVectorArray(RoomMinBId, minBufB);
        maskMaterial.SetVectorArray(RoomMaxBId, maxBufB);
        maskMaterial.SetFloat(RoomCountBId, countB);

        maskMaterial.SetFloat(RoomBlendId, blend);
        maskMaterial.SetFloat(IntensityId, 1f);
    }

    private void FillRects(RoomZone room, Vector4[] minBuf, Vector4[] maxBuf, out int count)
    {
        Collider2D[] areas = room.Areas;
        count = Mathf.Min(areas.Length, RoomZone.MaxAreas);

        for (int i = 0; i < count; i++)
        {
            Bounds b = areas[i].bounds;
            Vector3 minVp = cam.WorldToViewportPoint(b.min);
            Vector3 maxVp = cam.WorldToViewportPoint(b.max);

            minBuf[i] = new Vector4(Mathf.Min(minVp.x, maxVp.x), Mathf.Min(minVp.y, maxVp.y), 0f, 0f);
            maxBuf[i] = new Vector4(Mathf.Max(minVp.x, maxVp.x), Mathf.Max(minVp.y, maxVp.y), 0f, 0f);
        }
    }
}
