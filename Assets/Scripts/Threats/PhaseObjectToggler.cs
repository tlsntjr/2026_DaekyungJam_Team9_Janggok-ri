using UnityEngine;

// IThreatBehavior를 구현하여 HauntController의 '위협' 배열에 꽂을 수 있게 만듭니다.
public class PhaseObjectToggler : MonoBehaviour, IThreatBehavior
{
    public bool IsNeutralized { get; private set; }

    // 페이즈 시작 시 HauntController가 호출
    public void Activate()
    {
        gameObject.SetActive(true); // 오브젝트 켜기!
        IsNeutralized = false;
        Debug.Log($"<color=cyan>[PhaseObjectToggler]</color> {gameObject.name} 등장!");
    }

    // 페이즈 종료 시 HauntController가 호출
    public void Neutralize()
    {
        gameObject.SetActive(false); // 오브젝트 끄기!
        IsNeutralized = true;
        Debug.Log($"<color=gray>[PhaseObjectToggler]</color> {gameObject.name} 퇴장.");
    }

    public void Tick() { }
    public void SetProgress(float t) { }
}