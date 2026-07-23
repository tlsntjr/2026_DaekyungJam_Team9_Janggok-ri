using FMODUnity;
using UnityEngine;
using System.Collections;

public class AudioHallucination : MonoBehaviour
{
	[Header("FMOD Sounds")]
	[SerializeField] private EventReference[] hallucinationSfx;

	[Header("플레이어 세팅")]
	[SerializeField] private Transform player;
	[SerializeField] private float minInterval		= 8f;
	[SerializeField] private float maxInterval		= 25f;
	[SerializeField] private float randomRadius	= 3f; 
	[SerializeField] private int minStage			= 2;

	private Coroutine loopCoroutine;

	private void OnEnable() => EventBus.OnContaminationStageChanged += HandleStageChanged;
	private void OnDisable()
	{
		EventBus.OnContaminationStageChanged -= HandleStageChanged;
		if (loopCoroutine != null) StopCoroutine(loopCoroutine);
	}

	/// <summary>
	/// 스테이지 변화시, 2단계 이상부터 환청
	/// </summary>
	/// <param name="stage"></param>
	private void HandleStageChanged(int stage)
	{
		if (stage >= minStage && loopCoroutine == null)
			loopCoroutine = StartCoroutine(HallucinationLoop());
		else if (stage < minStage && loopCoroutine != null)
		{
			StopCoroutine(loopCoroutine);
			loopCoroutine = null;
		}
	}

	/// <summary>
	/// 환청 루프, 2단계 이상부터 계속 반복
	/// </summary>
	/// <returns></returns>
	private IEnumerator HallucinationLoop()
	{
		while (true)
		{
			yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
			if (hallucinationSfx.Length == 0) continue;

			var sfx			= hallucinationSfx[Random.Range(0, hallucinationSfx.Length)];
			Vector3 pos		= player.position + (Vector3)(Random.insideUnitCircle * randomRadius);

			SoundManager.Instance.PlayOneShot(sfx, pos);
		}
	}
}