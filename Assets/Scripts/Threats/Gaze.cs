using UnityEngine;
using System.Collections;

public class Gaze : MonoBehaviour, IThreatBehavior
{
    [Header("연결 필드")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private FishMovement movement;

    [Header("시야(Gaze) 설정")]
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private LayerMask obstacleMask;

    private bool isActive = false;
    private bool isDistracted = false;
    private Coroutine distractionCoroutine;

    public bool IsNeutralized { get; private set; }

    private void OnEnable()
    {
        EventBus.OnNoiseEmitted += HandleNoiseBridge;
    }

    private void OnDisable()
    {
        EventBus.OnNoiseEmitted -= HandleNoiseBridge;
    }

    public void Activate()
    {
        IsNeutralized = false;
        isActive = true;
        isDistracted = false;
        if (movement != null) movement.ActivateMovement();
    }

    public void Tick()
    {
        if (!isActive || IsNeutralized) return;

        if (!isDistracted)
        {
            PerformGazeCheck();
        }
    }

    private void PerformGazeCheck()
    {
        if (playerTransform == null || movement == null) return;

        if (Concealment.IsPlayerConcealed)
        {
            LostPlayer();
            return;
        }

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= viewDistance)
        {
            float angle = Vector2.Angle(transform.up, directionToPlayer);
            if (angle <= viewAngle / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask);
                if (hit.collider == null)
                {
                    movement.SetState(FishMovement.BehaviorState.Chase);

                    ContaminationSystem.Instance.Add(0.15f * Time.deltaTime);
                    Debug.DrawLine(transform.position, playerTransform.position, Color.red);
                    return;
                }
            }
        }

        LostPlayer();
    }

    private void LostPlayer()
    {
        if (movement.CurrentState == FishMovement.BehaviorState.Chase)
        {
            movement.SetState(FishMovement.BehaviorState.Patrol);
        }
    }

    private void HandleNoiseBridge(Vector2 position, float radius)
    {
        if (!isActive || IsNeutralized) return;

        float distanceToNoise = Vector2.Distance((Vector2)transform.position, position);
        if (distanceToNoise <= radius)
        {
            if (movement != null) movement.HandleNoise(position, radius);

            if (distractionCoroutine != null) StopCoroutine(distractionCoroutine);
            distractionCoroutine = StartCoroutine(NoiseDistractionRoutine());
        }
    }

    private IEnumerator NoiseDistractionRoutine()
    {
        isDistracted = true;
        yield return new WaitForSeconds(10f);
        isDistracted = false;
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        isActive = false;
        if (movement != null) movement.StopMovement();
        if (distractionCoroutine != null) StopCoroutine(distractionCoroutine);
    }

    public void SetProgress(float t) { }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.orange;
        Vector3 forward = transform.up;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, viewAngle / 2f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -viewAngle / 2f) * forward;

        Gizmos.DrawRay(transform.position, forward * viewDistance);
        Gizmos.DrawRay(transform.position, leftBoundary * viewDistance);
        Gizmos.DrawRay(transform.position, rightBoundary * viewDistance);
    }
}