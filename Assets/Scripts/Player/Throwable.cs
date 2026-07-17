using System.Collections;
using UnityEngine;
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
    [SerializeField] private float throwDuration = 0.35f;
    [SerializeField] private float arcHeight = 1f;
    [SerializeField] private float visualLifetime = 1f;

    [Header("사운드")]
    [SerializeField] private EventReference throwSfx;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
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
        float t = 0f;
        while (t < throwDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / throwDuration);
            Vector3 pos = Vector3.Lerp(from, to, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * arcHeight; // 3/4 아이소메트릭 시점용 포물선 궤적(화면상 Y축으로만 표현)
            visual.transform.position = pos;
            yield return null;
        }

        visual.transform.position = to;
        Land(to);
        Destroy(visual, visualLifetime);
    }

    private void Land(Vector3 pos)
    {
        if (!throwSfx.IsNull)
            SoundManager.Instance.PlayOneShot(throwSfx, pos);

        EventBus.RaiseNoiseEmitted(pos, noiseRadius);
    }
}
