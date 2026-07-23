using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 사망 연출 총괄. 오염도 100%(OnPlayerDeath) 시:
///   1) 플레이어 제어 컴포넌트 전부 정지 (이동·조준·상호작용·애니메이터 등)
///   2) 손전등 서서히 꺼짐
///   3) 사망 모션 + 사망 사운드 재생
///   4) 잠시 후 사망 UI 표시 — [다시 시도] / [메인으로]
/// 씬마다 배치 (플레이어·UI 참조가 씬 소속이므로 DontDestroyOnLoad 아님).
/// </summary>
public class DeathDirector : MonoBehaviour
{
	[Header("사망 시 정지시킬 플레이어 컴포넌트")]
	[SerializeField] private MonoBehaviour[] disableOnDeath;

	[Header("사망 이후 광원 축소")]
	[SerializeField] private Light2D[] lightsToFade;
	[SerializeField] private float lightFadeDuration			= 1.2f;

	[Header("사망 모션")]
	[SerializeField] private Animator playerAnimator;
	[SerializeField] private string deathStateName			= "Death";				// Animator Controller에 같은 이름의 상태 필요

	[Header("사망 사운드")]
	[SerializeField] private EventReference deathSfx;
	[SerializeField] private Transform player;

	[Header("사망 UI")]
	[SerializeField] private GameObject deathPanel;									// 기본 비활성화
	[SerializeField] private Button retryButton;											// 체크포인트(현재 구역 처음)부터 다시
	[SerializeField] private Button mainMenuButton;									// 메인으로
	[SerializeField] private string mainSceneName			= "SCENE_INTRO";
	[SerializeField] private float uiDelay						= 2f;						// 사망 모션을 보여줄 시간

	private bool handled;   // 중복 사망 이벤트 가드
	private float? panelDelayOverride;   // 설정되면 uiDelay 대신 이 값을 사용 (특정 사망 연출 재생시간에 정확히 맞출 때)

	/// <summary>
	/// 사망 UI를 uiDelay 대신 정확히 이 시간(초) 후에 띄움 — 다른 사망 연출(예: 인면어 클로즈업)의
	/// 재생시간에 맞춰 딱 끝나고 바로 UI가 뜨게 할 때 사용. HandleDeath보다 먼저 호출돼야 함.
	/// </summary>
	public void OverridePanelDelay(float seconds) => panelDelayOverride = seconds;

	private void OnEnable()
	{
		EventBus.OnPlayerDeath			+= HandleDeath;
		if (retryButton != null)			retryButton.onClick.AddListener(Retry);
		if (mainMenuButton != null)	mainMenuButton.onClick.AddListener(GoToMain);
	}

	private void OnDisable()
	{
		EventBus.OnPlayerDeath			-= HandleDeath;
		if (retryButton != null)			retryButton.onClick.RemoveListener(Retry);
		if (mainMenuButton != null)	mainMenuButton.onClick.RemoveListener(GoToMain);
	}

	private void HandleDeath()
	{
		if (handled) return;
		handled = true;

		StartCoroutine(DeathSequence());
	}

	private IEnumerator DeathSequence()
	{
		foreach (var comp in disableOnDeath)
		{
			if (comp == null) continue;
			comp.enabled = false;
		}

		if (playerAnimator != null) playerAnimator.Play(deathStateName);

		if (!deathSfx.IsNull && player != null)
			SoundManager.Instance.PlayOneShot(deathSfx, player.position);

		float t = 0f;
		float[] baseIntensity = new float[lightsToFade.Length];

		for (int i = 0; i < lightsToFade.Length; i++)
			if (lightsToFade[i] != null) baseIntensity[i] = lightsToFade[i].intensity;

		while (t < lightFadeDuration)
		{
			t += Time.deltaTime;
			float k = 1f - Mathf.Clamp01(t / lightFadeDuration);

			for (int i = 0; i < lightsToFade.Length; i++)
				if (lightsToFade[i] != null) lightsToFade[i].intensity = baseIntensity[i] * k;

			yield return null;
		}

		float delay = panelDelayOverride ?? uiDelay;
		yield return new WaitForSeconds(Mathf.Max(0f, delay - lightFadeDuration));
		if (deathPanel != null) deathPanel.SetActive(true);
	}

	/// <summary>
	/// 게임 다시 시도
	/// </summary>
	public void Retry()
	{
		ContaminationSystem.Instance.ResetAll();
		SceneFlow.Instance.FadeAndLoad(SceneManager.GetActiveScene().name);
	}

    /// <summary>
    /// 타이틀로 복귀
    /// </summary>
    public void GoToMain()
	{
		ContaminationSystem.Instance.ResetAll();
		SceneFlow.Instance.FadeAndLoad(mainSceneName);
	}
}
