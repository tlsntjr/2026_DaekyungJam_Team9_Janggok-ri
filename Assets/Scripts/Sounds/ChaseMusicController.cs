using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// 추격 BGM 컨트롤러.
/// 어떤 위협이든 추격 상태(ThreatState=2)에 들어가면 추격 음악을 재생하고,
/// 모든 위협이 추격을 포기하면(숨기 성공 등) 페이드아웃으로 멈춘다.
/// 여러 마리가 동시에 쫓아와도 음악은 하나만 유지 (huntId 집합으로 관리).
/// </summary>
public class ChaseMusicController : MonoBehaviour
{
	[Header("추격 BGM")]
	[SerializeField] private EventReference chaseMusic;

	[Header("심장박동")]

	// 현재 추격 중인 위협들의 huntId. 하나라도 있으면 음악 유지
	private readonly HashSet<string> chasing = new();
	private EventInstance instance;
	private bool playing;

	private void OnEnable()
	{
		EventBus.OnThreatStateChanged	+= HandleThreatState;
		EventBus.OnPlayerDeath				+= HandleDeath;
	}

	private void OnDisable()
	{
		EventBus.OnThreatStateChanged	-= HandleThreatState;
		EventBus.OnPlayerDeath				-= HandleDeath;
	}

	private void HandleThreatState(string huntId, int state)
	{
		if (state >= 2)	chasing.Add(huntId);
		else				chasing.Remove(huntId);

		if (chasing.Count > 0 && !playing)
			StartMusic();
		else if (chasing.Count == 0 && playing)
			StopMusic(immediate: false);   // 페이드아웃 (길이는 FMOD 이벤트의 AHDSR Release로 조절)
	}

	// 사망 시엔 여운 없이 즉시 컷 (사망 연출/스팅에게 자리를 비켜줌)
	private void HandleDeath()
	{
		chasing.Clear();
		if (playing) StopMusic(immediate: true);
	}

	private void StartMusic()
	{
		if (chaseMusic.IsNull) return;
		instance		= SoundManager.Instance.PlayLoop(chaseMusic, transform);
		playing		= true;
	}

	private void StopMusic(bool immediate)
	{
		SoundManager.Instance.StopLoop(instance, immediate);
		playing = false;
	}
}
