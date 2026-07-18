using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngameHUD : MonoBehaviour
{
	[Header("오염 게이지")]
	[SerializeField] private Image contaminationFill;

	[Header("상호작용 프롬프트")]
	[SerializeField] private GameObject promptPanel;
	[SerializeField] private TMP_Text promptText;
	[SerializeField] private TMP_Text interactKey;
	[SerializeField] private PlayerInteractor interactor;

	private void OnEnable()		=> EventBus.OnContaminationChanged		+= HandleContaminationChanged;
	private void OnDisable()		=> EventBus.OnContaminationChanged		-= HandleContaminationChanged;

	private void HandleContaminationChanged(float value)
	{
		contaminationFill.fillAmount = value;
	}

	private void Update()
	{
		string prompt = interactor.CurrentPrompt;
		string interactKey = interactor.CurrentInteractKey;

		promptPanel.SetActive(!string.IsNullOrEmpty(prompt));
		if (!string.IsNullOrEmpty(prompt))			promptText.text			= prompt;
		if (!string.IsNullOrEmpty(interactKey))		this.interactKey.text		= interactKey;
	}
}
