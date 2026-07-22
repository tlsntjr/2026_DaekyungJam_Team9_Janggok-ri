using UnityEngine;
using FMODUnity;

[System.Serializable]
public class FootstepEventMap
{
    public LayerMask layer;
    public EventReference footstepEvent;
}

[RequireComponent(typeof(Rigidbody2D))]
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] FootstepEventMap[] surfaceMap;
    [SerializeField] LayerMask floorMask;
    [SerializeField] float stepDistance = 0.5f;

    [Header("진단 — 발소리가 침묵하는 이유를 걸음마다 출력 (원인 잡히면 끌 것)")]
    [SerializeField] bool debugLog = false;

    Rigidbody2D rb;
    Vector2 lastPos;
    float distanceAccum;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPos = rb.position;
    }

    void FixedUpdate()
    {
        float moved = Vector2.Distance(rb.position, lastPos);
        lastPos = rb.position;

        distanceAccum += moved; // �����ص� ���� �� ��, �׳� �� �þ ��
        if (distanceAccum >= stepDistance)
        {
            distanceAccum = 0f;
            PlayFootstep(transform.position);
        }
    }

    // �ȱ� �ִϸ��̼� Ŭ���� �� ��� �����ӿ� Animation Event�� �� �Լ����� ���
    public void OnFootstepAnimEvent()
    {
        PlayFootstep(transform.position);
    }

    void PlayFootstep(Vector3 pos)
    {
        // 겹친 바닥 콜라이더를 전부 수집 — 단수 OverlapPoint는 겹침 지역(부두 밑에 바다 콜라이더가
        // 깔린 곳 등)에서 아무거나 하나를 돌려줘, 매칭 없는 레이어가 잡히면 발소리가 침묵하는
        // "부분적으로만 소리 남" 문제가 있었음.
        var hits = Physics2D.OverlapPointAll(pos, floorMask);
        if (hits == null || hits.Length == 0)
        {
            if (debugLog)
                Debug.Log($"<color=grey>[Footstep 진단]</color> 발밑({pos.x:F1}, {pos.y:F1})에 Floor Mask 레이어의 콜라이더가 없음 — " +
                          "바닥 타일맵 콜라이더 누락, 레이어가 Floor Mask에 미포함, 또는 타일 Collider Type이 None/Sprite(빈틈)인 경우");
            return;
        }

        // surfaceMap의 "앞에 있는 항목이 우선" — 부두/통행로를 물·진흙보다 위 칸에 배치할 것
        foreach (var m in surfaceMap)
        {
            foreach (var hit in hits)
            {
                if (((1 << hit.gameObject.layer) & m.layer) == 0) continue;

                if (!m.footstepEvent.IsNull)
                    SoundManager.Instance.PlayOneShot(m.footstepEvent, pos);
                else if (debugLog)
                    Debug.Log($"<color=grey>[Footstep 진단]</color> '{LayerMask.LayerToName(hit.gameObject.layer)}' 매칭됐지만 이벤트가 비어 있음 (의도적 무음이 아니면 Surface Map에 이벤트 연결)");
                return;   // 매칭된 최우선 표면 하나만 재생 (이벤트가 비어 있으면 의도적 무음)
            }
        }

        if (debugLog)
        {
            string layers = "";
            foreach (var hit in hits)
                layers += $"'{hit.gameObject.name}'(레이어 {LayerMask.LayerToName(hit.gameObject.layer)}) ";
            Debug.Log($"<color=grey>[Footstep 진단]</color> 발밑 콜라이더는 있지만 Surface Map에 매칭 항목이 없음 — 감지된 것: {layers}");
        }
    }
}
