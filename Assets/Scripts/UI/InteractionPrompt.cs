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
		string prompt	= interactor.CurrentPrompt;
		string interact	= interactor.CurrentInteractKey;
		bool show		= !string.IsNullOrEmpty(prompt);
		promptRoot.SetActive(show);

		if (!show) return;

		promptText.text = prompt;
		interactKey.text = interact;
		promptRoot.transform.position =
			interactor.CurrentTargetTransform.position + worldOffset;
	}
}
