using UnityEngine;

public class RoomsizeEstimator : MonoBehaviour
{
    [SerializeField] LayerMask wallMask;
    [SerializeField] float openDistance = 10f;   // 이 이상이면 완전 개방(0)
    [SerializeField] float tightDistance = 1.5f; // 이 이하면 완전 밀폐(1)
    [SerializeField] int rayCount = 8;
    [SerializeField] float sampleInterval = 0.2f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < sampleInterval) return;
        timer = 0f;

        float total = 0f;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (360f / rayCount) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var hit = Physics2D.Raycast(transform.position, dir, openDistance, wallMask);
            total += hit.collider ? hit.distance : openDistance;
        }

        float avg = total / rayCount;
        float roomSize = Mathf.InverseLerp(openDistance, tightDistance, avg); // 0~1
        
        SoundManager.Instance.SetGlobalParam("RoomSize", roomSize);
    }
}
