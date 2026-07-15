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

        distanceAccum += moved; // 정지해도 리셋 안 함, 그냥 안 늘어날 뿐
        if (distanceAccum >= stepDistance)
        {
            distanceAccum = 0f;
            PlayFootstep(transform.position);
        }
    }

    // 걷기 애니메이션 클립의 발 닿는 프레임에 Animation Event로 이 함수명을 등록
    public void OnFootstepAnimEvent()
    {
        PlayFootstep(transform.position);
    }

    void PlayFootstep(Vector3 pos)
    {
        var hit = Physics2D.OverlapPoint(pos, floorMask);
        if (!hit) return;

        foreach (var m in surfaceMap)
        {
            if (((1 << hit.gameObject.layer) & m.layer) != 0)
            {
                SoundManager.Instance.PlayOneShot(m.footstepEvent, pos);
                return;
            }
        }
    }
}
