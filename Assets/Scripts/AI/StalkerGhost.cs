using UnityEngine;
using System.Collections;

// 한 몸으로 합체! IThreatBehavior를 직접 구현합니다.
public class StalkerGhost : MonoBehaviour, IThreatBehavior
{
    [Header("이동 및 추적")]
    public float speed = 3f;
    private Transform player;
    private Vector3? lureTarget = null;
    private Coroutine lureCoroutine;
    private bool isAtLure = false;
    private bool isActivated = false;

    // 인터페이스 구현
    public bool IsNeutralized { get; private set; } = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        // 활성화되지 않았거나 파훼되었으면 이동하지 않음
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

    // IThreatBehavior 인터페이스 필수 구현
    public void Activate()
    {
        isActivated = true;
        Debug.Log("<color=green>[GhostAI]</color> 갯벌 페이즈 시작! 추적 개시.");
    }

    public void Neutralize()
    {
        IsNeutralized = true;
        Debug.Log("<color=yellow>[GhostAI]</color> 파훼됨! 정지.");
    }

    public void Tick() { }
    public void SetProgress(float t) { }

    // 소음 감지 함수 (외부에서 호출)
    public void OnNoiseDetected(Vector3 noisePosition)
    {
        if (lureCoroutine != null) StopCoroutine(lureCoroutine);
        lureTarget = noisePosition;
        isAtLure = false;
        lureCoroutine = StartCoroutine(LureTimer(7f));
    }

    private IEnumerator LureTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        lureTarget = null;
        isAtLure = false;
    }
}