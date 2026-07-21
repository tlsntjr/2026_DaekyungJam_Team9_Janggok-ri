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

    // static — 컴포넌트는 씬-로컬이지만 플래그 데이터는 씬 전환을 넘어 유지됨.
    // (집에서 세운 스토리 플래그가 엔딩 씬의 EndingEvaluator까지 도달해야 하므로)
    // 새 게임 시작 시엔 ClearAll()을 호출해 초기화할 것.
    static readonly HashSet<string> flags = new();
    public event Action<string> OnFlagChanged;

    /// <summary>모든 플래그 초기화 — 새 게임 시작(메인 로비 → 시작) 시 호출</summary>
    public static void ClearAll() => flags.Clear();

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
