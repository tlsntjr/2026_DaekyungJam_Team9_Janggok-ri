using System.Collections;
using UnityEngine;

/// <summary>
/// 몬스터 실루엣이 플레이어 옆을 스쳐 지나가듯 화면을 가로질러 이동했다가 사라진다.
/// 2단계: 적당한 속도로 한 번씩. 3단계: 더 자주, 더 크게, 더 가까이 스쳐 지나간다.
/// 실루엣 스프라이트는 아트가 없으면 단색 placeholder 스프라이트를 써도 된다.
/// </summary>
public class SilhouetteScareDirector : MonoBehaviour
{
    [Header("연출 대상 (없으면 스킵)")]
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer silhouetteRenderer;

    [Header("지나가는 궤적")]
    [SerializeField] private float passDistance = 12f;   // 지나가는 궤적 전체 길이
    [SerializeField] private float sideOffsetMin = 1.5f; // 플레이어 기준 옆으로 스치는 최소 거리
    [SerializeField] private float sideOffsetMax = 4f;

    [Header("2단계")]
    [SerializeField] private float stage2MinInterval = 5f;
    [SerializeField] private float stage2MaxInterval = 12f;
    [SerializeField] private float stage2PassDuration = 0.4f;
    [SerializeField] private float stage2Scale = 1f;

    [Header("3단계")]
    [SerializeField] private float stage3MinInterval = 2.5f;
    [SerializeField] private float stage3MaxInterval = 6f;
    [SerializeField] private float stage3PassDuration = 0.25f;
    [SerializeField] private float stage3Scale = 1.8f;
    [SerializeField] private float stage3SideOffsetMax = 2f; // 3단계엔 더 가깝게 스칠 수 있도록 좁힘

    private Coroutine loop;
    private Coroutine passRoutine;
    private int currentStage;

    private void OnEnable()  => EventBus.OnContaminationStageChanged += HandleStageChanged;
    private void OnDisable() => EventBus.OnContaminationStageChanged -= HandleStageChanged;

    private void HandleStageChanged(int stage)
    {
        currentStage = stage;

        if (stage >= 2 && loop == null)
            loop = StartCoroutine(ScareLoop());
        else if (stage < 2 && loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator ScareLoop()
    {
        while (true)
        {
            bool isStage3 = currentStage >= 3;
            float interval = isStage3
                ? Random.Range(stage3MinInterval, stage3MaxInterval)
                : Random.Range(stage2MinInterval, stage2MaxInterval);

            yield return new WaitForSeconds(interval);
            TriggerPassBy(isStage3);
        }
    }

    private void TriggerPassBy(bool isStage3)
    {
        if (silhouetteRenderer == null || player == null) return;

        float travelAngleDeg = Random.Range(0f, 360f);
        float perpAngleDeg = travelAngleDeg + 90f;

        float maxSideOffset = isStage3 ? stage3SideOffsetMax : sideOffsetMax;
        float sideOffset = Random.Range(sideOffsetMin, maxSideOffset) * (Random.value < 0.5f ? 1f : -1f);

        Vector2 travelDir = AngleToDir(travelAngleDeg);
        Vector2 perpDir = AngleToDir(perpAngleDeg);
        Vector2 lineCenter = (Vector2)player.position + perpDir * sideOffset;

        float half = passDistance * 0.5f;
        Vector2 start = lineCenter - travelDir * half;
        Vector2 end = lineCenter + travelDir * half;

        silhouetteRenderer.transform.localScale = Vector3.one * (isStage3 ? stage3Scale : stage2Scale);

        Color c = silhouetteRenderer.color;
        c.a = 1f;
        silhouetteRenderer.color = c;
        silhouetteRenderer.enabled = true;

        if (passRoutine != null) StopCoroutine(passRoutine);
        passRoutine = StartCoroutine(PassRoutine(start, end, isStage3 ? stage3PassDuration : stage2PassDuration));
    }

    private static Vector2 AngleToDir(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private IEnumerator PassRoutine(Vector2 start, Vector2 end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            silhouetteRenderer.transform.position = Vector2.Lerp(start, end, t / duration);
            yield return null;
        }

        silhouetteRenderer.enabled = false;
    }
}
