using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 녹음기 획득 후 오염도 100%로 사망하면 얼굴 클로즈업(Fish_front_attack_5)이
/// 화면을 꽉 채우며 튀어나왔다가, 화면보다 더 커지며 사라지는 전용 점프스케어
/// (트리거: ContaminationSystem.Add에서 오염도 최대 도달 + 녹음기 보유 시 EventBus.RaiseKilledByFish).
/// 녹음기 획득 전에 오염도로 죽으면(다른 사망 경로) 이 연출은 스킵되고 DeathDirector만 진행됨.
/// 연출이 끝나는 순간 DeathDirector의 사망 UI가 바로 뜨도록 재생시간을 정확히 넘겨줌.
/// 씬마다 배치 (UI/인벤토리 참조가 씬 소속).
/// </summary>
public class RecorderFishDeathScare : MonoBehaviour
{
    [Header("클로즈업 이미지 (Fish_front_attack_5, 앵커를 화면 전체로 늘려둘 것 — 기본 비활성화)")]
    [SerializeField] private Image faceImage;

    [SerializeField] private string recorderItemId = "recorder";

    [Header("1단계 — 튀어나오며 화면 꽉 채우기 (스케일 1)")]
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float startScale = 0.15f;

    [Header("2단계 — 화면보다 더 커지며 사라짐")]
    [SerializeField] private float growOutDuration = 0.4f;
    [SerializeField] private float growOutScale = 2.5f;

    [Header("사운드 (비우면 스킵)")]
    [SerializeField] private EventReference roarSfx;   // HF-Fish-Small (hf-fish-roar-small-1 등 변형 랜덤 재생)

    [Header("카메라 흔들림")]
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float shakeMagnitude = 0.3f;

    [Header("사망 UI 타이밍 — 이 연출이 끝나는 순간 DeathDirector 사망 UI가 뜨게 함")]
    [SerializeField] private DeathDirector deathDirector;

    private void OnEnable()  => EventBus.OnKilledByFish += HandleKilledByFish;
    private void OnDisable() => EventBus.OnKilledByFish -= HandleKilledByFish;

    private void HandleKilledByFish()
    {
        if (faceImage == null) return;
        if (InventorySystem.Instance == null || !InventorySystem.Instance.Has(recorderItemId)) return;

        // DeathDirector.HandleDeath보다 먼저 실행됨 (RaiseKilledByFish가 RaisePlayerDeath보다 먼저 발화되므로)
        if (deathDirector != null)
            deathDirector.OverridePanelDelay(popDuration + growOutDuration);

        if (!roarSfx.IsNull)
            SoundManager.Instance.PlayOneShot(roarSfx, transform.position);

        CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);

        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        faceImage.gameObject.SetActive(true);

        // 1단계 — 작게 시작해 화면 꽉 채울 때까지(스케일 1) 커짐, 동시에 페이드인
        faceImage.rectTransform.localScale = Vector3.one * startScale;
        Color c = faceImage.color;
        c.a = 0f;
        faceImage.color = c;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);

            faceImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, p);
            c.a = Mathf.Lerp(0f, 1f, p);
            faceImage.color = c;

            yield return null;
        }

        // 2단계 — 화면보다 더 커지며 페이드아웃, 끝나는 순간 DeathDirector가 사망 UI를 띄움
        t = 0f;
        while (t < growOutDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / growOutDuration);

            faceImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, growOutScale, p);
            c.a = Mathf.Lerp(1f, 0f, p);
            faceImage.color = c;

            yield return null;
        }

        faceImage.gameObject.SetActive(false);
    }
}
