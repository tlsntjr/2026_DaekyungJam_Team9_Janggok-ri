using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    readonly List<EventInstance> activeLoops = new();
    readonly Dictionary<string, EventInstance> activeSnapshots = new();

    void Awake()
    {
        // 중복이면 "이 컴포넌트만" 제거 — 같은 오브젝트의 씬-로컬 매니저들을 같이 죽이지 않게
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- ����: �߼Ҹ�, ������, �ݱ�, �������ɾ� ���� �� ----
    public void PlayOneShot(EventReference evt, Vector3 worldPos,
                             string localParam = null, float paramValue = 0f)
    {
        EventInstance instance = RuntimeManager.CreateInstance(evt);
        if (localParam != null)
            instance.setParameterByName(localParam, paramValue); // ex) "Surface"
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPos));
        instance.start();
        instance.release(); // ��� ������ �˾Ƽ� ������
    }

    // ---- ����: �ں��, ���� �׸��� �� ������ ----
    public EventInstance PlayLoop(EventReference evt, Transform followTarget)
    {
        EventInstance instance = RuntimeManager.CreateInstance(evt);
        RuntimeManager.AttachInstanceToGameObject(instance, followTarget); // ��ġ �ڵ� ����
        instance.start();
        activeLoops.Add(instance);
        return instance;
    }

    public void StopLoop(EventInstance instance, bool immediate = false)
    {
        // 부착된 GameObject가 파괴(씬 전환 등)되어도 페이드아웃이 끝까지 재생되도록
        // 정지 전에 분리 — 안 하면 런타임이 부착 정리 과정에서 페이드를 잘라버릴 수 있음
        RuntimeManager.DetachInstanceFromGameObject(instance);

        instance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
        activeLoops.Remove(instance);
    }

    // ---- ���� �Ķ����: Contamination �� Region �� ThreatState �� RoomSize ----
    public void SetGlobalParam(string name, float value)
        => RuntimeManager.StudioSystem.setParameterByName(name, value);

    // ---- �ν��Ͻ� �Ķ����: Occlusion (�������� ������ ��) ----
    public void SetInstanceParam(EventInstance instance, string name, float value)
        => instance.setParameterByName(name, value);

    // ---- ������: ���� ������, ContaminationHaze, Death �� ----
    public void SetSnapshot(EventReference snapshotEvt, bool on)
    {
        string key = snapshotEvt.Guid.ToString();
        if (on)
        {
            if (activeSnapshots.ContainsKey(key)) return;
            var inst = RuntimeManager.CreateInstance(snapshotEvt);
            inst.start();
            activeSnapshots[key] = inst;
        }
        else if (activeSnapshots.TryGetValue(key, out var inst))
        {
            inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            inst.release();
            activeSnapshots.Remove(key);
        }
    }

    void OnDestroy()
    {
        foreach (var i in activeLoops) { i.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); i.release(); }
        foreach (var i in activeSnapshots.Values) { i.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); i.release(); }
    }
}
