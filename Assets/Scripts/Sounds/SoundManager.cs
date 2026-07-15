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
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- 원샷: 발소리, 던지기, 줍기, 점프스케어 스팅 등 ----
    public void PlayOneShot(EventReference evt, Vector3 worldPos,
                             string localParam = null, float paramValue = 0f)
    {
        EventInstance instance = RuntimeManager.CreateInstance(evt);
        if (localParam != null)
            instance.setParameterByName(localParam, paramValue); // ex) "Surface"
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPos));
        instance.start();
        instance.release(); // 재생 끝나면 알아서 정리됨
    }

    // ---- 루프: 앰비언스, 괴물 그르렁 등 지속음 ----
    public EventInstance PlayLoop(EventReference evt, Transform followTarget)
    {
        EventInstance instance = RuntimeManager.CreateInstance(evt);
        RuntimeManager.AttachInstanceToGameObject(instance, followTarget); // 위치 자동 추적
        instance.start();
        activeLoops.Add(instance);
        return instance;
    }

    public void StopLoop(EventInstance instance, bool immediate = false)
    {
        instance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
        activeLoops.Remove(instance);
    }

    // ---- 전역 파라미터: Contamination · Region · ThreatState · RoomSize ----
    public void SetGlobalParam(string name, float value)
        => RuntimeManager.StudioSystem.setParameterByName(name, value);

    // ---- 인스턴스 파라미터: Occlusion (루프별로 독립된 값) ----
    public void SetInstanceParam(EventInstance instance, string name, float value)
        => instance.setParameterByName(name, value);

    // ---- 스냅샷: 구역 리버브, ContaminationHaze, Death 등 ----
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
