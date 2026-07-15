using UnityEngine;

public class AudioParamBridge : MonoBehaviour
{
	void OnEnable()
	{
		EventBus.OnContaminationChanged			+= HandleContamination;
		EventBus.OnThreatStateChanged				+= HandleThreatState;
		
	}

	void OnDisable()
	{
		EventBus.OnContaminationChanged			-= HandleContamination;
        EventBus.OnThreatStateChanged				-= HandleThreatState;
	}

	/// <summary>
	/// 오염도(0~1) 심장박동 및 환청 등 효과음에 대한 블렌딩용
	/// </summary>
	void HandleContamination(float value)
		=> SoundManager.Instance.SetGlobalParam("Contamination", value);


	/// <summary>
	/// 위협 상태에 따른 배경음 재생
	/// </summary>
	void HandleThreatState(string huntId, int state)
		=> SoundManager.Instance.SetGlobalParam("ThreatState", state);
}
