using TMPro;
using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
	[SerializeField] private GameObject promptRoot; 
	[SerializeField] private TMP_Text promptText;
	[SerializeField] private TMP_Text interactKey;
	[SerializeField] private PlayerInteractor interactor;
	[SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);

	private void Update()
	{
		// 씬 전환 후 DDOL 루트에 얹혀 살아남은 잔해 프롬프트 자가 정리 —
		// 자기 씬의 플레이어(interactor)가 파괴됐으면 이 UI는 더 표시될 이유가 없음.
		// (잔해가 위치 갱신을 못 받아 화면 왼쪽 아래에 이중 렌더링되던 문제)
		if (interactor == null)
		{
			if (promptRoot != null) promptRoot.SetActive(false);
			enabled = false;
			return;
		}

		string prompt	= interactor.CurrentPrompt;
		string interact	= interactor.CurrentInteractKey;
		bool show		= !string.IsNullOrEmpty(prompt);
		promptRoot.SetActive(show);

		if (!show) return;

		// 목표가 사라진 프레임(획득 직후 등) 방어 — 위치 갱신 없이 표시만 유지하면 잔상이 남으므로 숨김
		if (interactor.CurrentTargetTransform == null)
		{
			promptRoot.SetActive(false);
			return;
		}

		promptText.text = prompt;
		interactKey.text = interact;
		promptRoot.transform.position =
			interactor.CurrentTargetTransform.position + worldOffset;
	}
}
