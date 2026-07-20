using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

/// <summary>
/// 마우스 왼쪽 클릭으로 조개껍데기를 소모해 플레이어 위치에서 마우스 위치까지 던진다.
/// 착지하는 순간 소음을 방송한다. 누가 소음에 반응하는지는 몰라도 된다 (몬스터가 알아서 감지).
/// </summary>
public class Throwable : MonoBehaviour
{
    [Header("소모 아이템")]
    [SerializeField] private string shellItemId = "shell";

    [Header("소음")]
    [SerializeField] private float noiseRadius = 5f;

    [Header("연출")]
    [SerializeField] private GameObject throwVisualPrefab;
    [SerializeField] private float throwSpeed = 9f;          // 거리 ÷ 이 값 = 체공 시간 (멀리 던질수록 오래 난다)
    [SerializeField] private float minDuration = 0.25f;
    [SerializeField] private float maxDuration = 0.8f;
    [SerializeField] private float arcHeightRatio = 0.35f;   // 거리 비례 포물선 높이 (멀리 던질수록 높이 뜸)
    [SerializeField] private float arcHeight = 2.2f;         // 포물선 최대 높이 상한
    [SerializeField] private float spinSpeedMax = 540f;      // 비행 중 회전 (도/초, 방향·세기 랜덤)
    [SerializeField] private float scalePeak = 1.25f;        // 정점에서 살짝 커짐 — 카메라에 가까워지는 높이감
    [SerializeField] private int bounceCount = 2;            // 착지 후 잔 바운스 횟수
    [SerializeField] private float visualLifetime = 1f;

    [Header("사운드")]
    [SerializeField] private EventReference throwSfx;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // UI(대화창·버튼 등) 위를 클릭한 경우 — UI 조작이지 투척이 아님
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 팝업 폭풍 중엔 투척 잠금 — 더미 팝업은 클릭을 통과시키는 구조라 위 검사만으론 새어 들어옴
        if (PopupStormDirector.Instance != null && PopupStormDirector.Instance.IsActive) return;

        TryThrow();
    }

    private void TryThrow()
    {
        if (!InventorySystem.Instance.Has(shellItemId))
        {
            DialogueSystem.Instance.Show("던질 것이 없다.");
            return;
        }

        InventorySystem.Instance.Remove(shellItemId);

        Vector3 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetPos.z = 0f;

        if (throwVisualPrefab != null)
        {
            GameObject visual = Instantiate(throwVisualPrefab, transform.position, Quaternion.identity);
            StartCoroutine(ThrowRoutine(visual, transform.position, targetPos));
        }
        else
        {
            Land(targetPos);
        }
    }

    private IEnumerator ThrowRoutine(GameObject visual, Vector3 from, Vector3 to)
    {
        // 거리 비례 체공 시간·포물선 높이 — 짧게 던지면 낮고 빠르게 톡, 멀리 던지면 높고 길게 붕
        float dist = Vector2.Distance(from, to);
        float duration = Mathf.Clamp(dist / throwSpeed, minDuration, maxDuration);
        float arc = Mathf.Min(arcHeight, dist * arcHeightRatio + 0.3f);

        // 바닥 그림자 — 그림자는 직선 경로(지면)를 따라가고 껍데기는 그 위로 뜬다.
        // 이 "분리"가 탑다운 시점에서 높이를 인식시키는 핵심 트릭
        Transform shadow = CreateShadow(visual);
        Vector3 shadowBaseScale = shadow != null ? shadow.localScale : Vector3.one;

        float spin = Random.Range(0.6f, 1f) * spinSpeedMax * (Random.value < 0.5f ? -1f : 1f);
        Vector3 baseScale = visual.transform.localScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float h = Mathf.Sin(p * Mathf.PI);   // 0→1→0 높이 곡선

            Vector3 ground = Vector3.Lerp(from, to, p);
            Vector3 pos = ground;
            pos.y += h * arc;                     // 3/4 아이소메트릭 시점용 포물선(화면 Y축으로 표현)

            visual.transform.position = pos;
            visual.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            visual.transform.localScale = baseScale * (1f + (scalePeak - 1f) * h);   // 정점에서 살짝 커짐

            if (shadow != null)
            {
                shadow.position = ground;
                shadow.localScale = shadowBaseScale * Mathf.Lerp(1f, 0.55f, h);      // 높이 오르면 그림자 축소
            }

            yield return null;
        }

        visual.transform.position = to;
        visual.transform.localScale = baseScale;
        Land(to);   // 소음·착지음은 첫 착지 시점 — 잔 바운스는 순수 연출

        // 잔 바운스 — 점점 낮게 두어 번 튀고 멈춰야 "떨어졌다"가 완성됨
        float bounceHeight = arc * 0.25f;
        for (int i = 0; i < bounceCount && bounceHeight > 0.05f; i++)
        {
            float bt = 0f;
            const float bounceDuration = 0.18f;
            while (bt < bounceDuration)
            {
                bt += Time.deltaTime;
                float bp = Mathf.Clamp01(bt / bounceDuration);
                Vector3 pos = to;
                pos.y += Mathf.Sin(bp * Mathf.PI) * bounceHeight;
                visual.transform.position = pos;
                visual.transform.Rotate(0f, 0f, spin * 0.4f * Time.deltaTime);
                yield return null;
            }
            bounceHeight *= 0.4f;
        }
        visual.transform.position = to;

        if (shadow != null) Destroy(shadow.gameObject);
        Destroy(visual, visualLifetime);
    }

    /// <summary>
    /// 비주얼의 스프라이트를 복제해 검게 눌러 만든 즉석 그림자 — 별도 에셋 불필요.
    /// 프리팹에 SpriteRenderer가 없으면 그림자 없이 진행.
    /// </summary>
    private Transform CreateShadow(GameObject visual)
    {
        var sr = visual.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return null;

        var go = new GameObject("ThrowShadow");
        var shadowSr = go.AddComponent<SpriteRenderer>();
        shadowSr.sprite = sr.sprite;
        shadowSr.color = new Color(0f, 0f, 0f, 0.35f);
        shadowSr.sortingLayerID = sr.sortingLayerID;
        shadowSr.sortingOrder = sr.sortingOrder - 1;   // 껍데기 바로 아래에 깔리게

        Vector3 s = visual.transform.localScale * 0.9f;
        s.y *= 0.6f;   // 납작하게 눌러 타원 그림자 느낌
        go.transform.localScale = s;
        return go.transform;
    }

    private void Land(Vector3 pos)
    {
        if (!throwSfx.IsNull)
            SoundManager.Instance.PlayOneShot(throwSfx, pos);

        EventBus.RaiseNoiseEmitted(pos, noiseRadius);
    }
}
