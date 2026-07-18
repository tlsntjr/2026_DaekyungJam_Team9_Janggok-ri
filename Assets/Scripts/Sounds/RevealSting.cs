using FMODUnity;
using UnityEngine;

/// <summary>
/// "손전등을 돌렸더니 갑자기 눈앞에 있는" 순간의 스팅(효과음) 재생.
/// LitVisibility가 몬스터 스프라이트 알파를 페이드하는 것을 감시해서,
/// 완전히 안 보이던 상태 → 드러난 상태로 전환되는 순간을 판정한다.
/// 몬스터 오브젝트에 부착 (LitVisibility와 같은 대상).
/// </summary>
public class RevealSting : MonoBehaviour
{
    [Header("감시 대상 (LitVisibility가 페이드하는 몬스터 본체 렌더러)")]
    [SerializeField] private SpriteRenderer body;

    [Header("판정")]
    [SerializeField] private Transform player;
    [SerializeField] private float maxDistance = 5f;       // 이 거리 안에서 드러날 때만 스팅 (먼 데서 걸리는 건 무섭지 않음)
    [SerializeField] private float hiddenThreshold = 0.1f; // 이 미만이면 "완전히 안 보임"
    [SerializeField] private float revealThreshold = 0.6f; // 이 이상이면 "드러남"
    [SerializeField] private float cooldown = 8f;          // 빛 경계에서 들락날락할 때 스팅 연발 방지

    [Header("스팅")]
    [SerializeField] private EventReference stingSfx;

    private bool wasHidden = true;
    private float lastStingTime = -999f;

    private void Update()
    {
        if (body == null) return;

        float alpha = body.color.a;

        // 완전히 사라진 상태를 거쳐야만 다음 "드러남"이 성립 (경계에서 파닥거림 방지)
        if (alpha < hiddenThreshold)
        {
            wasHidden = true;
            return;
        }

        if (!wasHidden || alpha < revealThreshold) return;
        wasHidden = false;

        if (player != null && Vector2.Distance(player.position, transform.position) > maxDistance) return;
        if (Time.time - lastStingTime < cooldown) return;
        if (stingSfx.IsNull) return;

        lastStingTime = Time.time;
        SoundManager.Instance.PlayOneShot(stingSfx, transform.position);
    }
}
