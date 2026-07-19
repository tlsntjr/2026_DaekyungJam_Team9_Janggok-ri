using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// 해당 클래스는 목표를 달성 했는지, 하지 않았는지 여부만 체크함
/// 작업 진행에 따라 진척도 시스템이 필요할 경우 key_{index} 형태로 추가 관리하면 될 것 같음
/// </summary>
public class ObjectiveSystem : MonoBehaviour, IObjective
{
    public static ObjectiveSystem Instance { get; private set; }

    readonly HashSet<string> flags = new();
    public event Action<string> OnFlagChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 구역 클리어 시 huntId("mudflat" 등)가 그대로 플래그로 저장됨
    // — EndingCondition의 requiredFlags가 이 값을 참조해 엔딩 판정
    void OnEnable()  => EventBus.OnHauntCleared += SetFlag;
    void OnDisable() => EventBus.OnHauntCleared -= SetFlag;

    public bool HasFlag(string id) => flags.Contains(id);

    public void SetFlag(string id)
    {
        if (!flags.Add(id)) return;
        OnFlagChanged?.Invoke(id);
        EventBus.RaiseObjectiveFlagSet(id);
    }
}
