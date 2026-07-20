using UnityEngine;

public class AudioParamBridge : MonoBehaviour
{
	void OnEnable()
	{
		EventBus.OnContaminationChanged			+= HandleContamination;
		EventBus.OnThreatStateChanged				+= HandleThreatState;
		EventBus.OnPlayerDeath						+= HandleDeath;
	}

	void OnDisable()
	{
		EventBus.OnContaminationChanged			-= HandleContamination;
        EventBus.OnThreatStateChanged				-= HandleThreatState;
		EventBus.OnPlayerDeath						-= HandleDeath;
	}

	/// <summary>
	/// 사망 시 위협 파라미터 원복 — 안 하면 씬 리로드 후에도 ThreatState=2가 남아
	/// 재시작 직후부터 심장박동/긴장 믹스가 켜진 채 시작됨
	/// </summary>
	void HandleDeath()
		=> SoundManager.Instance.SetGlobalParam("ThreatState", 0);

	/// <summary>
	/// ������(0~1) ����ڵ� �� ȯû �� ȿ������ ���� ��������
	/// </summary>
	void HandleContamination(float value)
		=> SoundManager.Instance.SetGlobalParam("Contamination", value);


	/// <summary>
	/// ���� ���¿� ���� ����� ���
	/// </summary>
	void HandleThreatState(string huntId, int state)
		=> SoundManager.Instance.SetGlobalParam("ThreatState", state);
}
