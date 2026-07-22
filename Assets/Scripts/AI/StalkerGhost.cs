using UnityEngine;
using System.Collections;

// �� ������ ��ü! IThreatBehavior�� ���� �����մϴ�.
public class StalkerGhost : MonoBehaviour, IThreatBehavior
{
    [Header("�̵� �� ����")]
    public float speed = 3f;

    [Header("청각 (소음 유인 — 조개 투척의 대상)")]
    [SerializeField] private float hearRadius = 12f;      // 이 유령이 들을 수 있는 최대 거리
    [SerializeField] private float lureDuration = 7f;     // 유인 지점에 머무는 시간 (stayAtLure가 켜져 있으면 무시)

    [Header("유인되면 그 자리에 눌러앉음 — 추적 복귀 안 함 (조개껍데기를 갖고 노는 원혼 컨셉)")]
    [SerializeField] private bool stayAtLure = false;

    private Transform player;
    private Vector3? lureTarget = null;
    private Coroutine lureCoroutine;
    private bool isAtLure = false;
    private bool isActivated = false;

    // �������̽� ����
    public bool IsNeutralized { get; private set; } = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // 소음 이벤트 구독 — 이게 없으면 OnNoiseDetected를 아무도 호출하지 않아
    // 조개를 던져도 유인되지 않고 플레이어만 영원히 쫓아오는 버그가 됨
    private void OnEnable()  => EventBus.OnNoiseEmitted += HandleNoise;
    private void OnDisable() => EventBus.OnNoiseEmitted -= HandleNoise;

    private void HandleNoise(Vector2 noisePos, float radius)
    {
        if (!isActivated || IsNeutralized) return;

        // 소리의 도달 반경(이벤트)과 유령의 청각 반경 중 좁은 쪽으로 판정 —
        // "조개 소리는 반경 5까지"라는 소음 설계가 유령에게도 그대로 적용되게
        float effectiveRadius = Mathf.Min(radius, hearRadius);
        float distance = Vector2.Distance((Vector2)transform.position, noisePos);

        if (distance <= effectiveRadius)
        {
            Debug.Log($"<color=cyan>[StalkerGhost]</color> 소음에 유인됨! 거리: {distance:F1}");
            OnNoiseDetected(noisePos);
        }
    }

    private void Update()
    {
        // Ȱ��ȭ���� �ʾҰų� ���ѵǾ����� �̵����� ����
        if (!isActivated || IsNeutralized) return;

        Vector3 destination = lureTarget ?? player.position;

        if (lureTarget != null && !isAtLure)
        {
            float dist = Vector3.Distance(transform.position, destination);
            if (dist < 0.5f)
            {
                isAtLure = true;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            }
        }
        else if (lureTarget == null)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }
    }

    // IThreatBehavior �������̽� �ʼ� ����
    public void Activate()
    {
        isActivated = true;
        Debug.Log("<color=green>[GhostAI]</color> ���� ������ ����! ���� ����.");
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        Debug.Log("<color=yellow>[GhostAI]</color> ���ѵ�! ����.");
    }

    public void Tick() { }
    public void SetProgress(float t) { }

    // ���� ���� �Լ� (�ܺο��� ȣ��)
    public void OnNoiseDetected(Vector3 noisePosition)
    {
        if (lureCoroutine != null) StopCoroutine(lureCoroutine);
        lureTarget = noisePosition;
        isAtLure = false;

        // 눌러앉기 모드 — 복귀 타이머를 걸지 않음. 유인 지점에 도착하면 영원히 머묾
        // (새 소음이 나면 그쪽으로 다시 이동 — 조개를 쫓아다니는 그림 자체는 유지)
        if (stayAtLure) return;

        lureCoroutine = StartCoroutine(LureTimer(lureDuration));
    }

    private IEnumerator LureTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        lureTarget = null;
        isAtLure = false;
    }
}