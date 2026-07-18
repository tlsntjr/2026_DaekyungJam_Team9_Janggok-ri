using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlow : MonoBehaviour
{
	public static SceneFlow Instance { get; private set; }

	[Header("UI - Screen for Fade In/Out")]
	[SerializeField] private CanvasGroup fadeOverlay;

	[Header("Fade Duration")]
	[SerializeField] private float fadeDuration = 0.8f;

	private void Awake()
	{
		if (Instance != null)   { Destroy(gameObject); return; }
		Instance = this;
		DontDestroyOnLoad(gameObject);

		// 씬에 저장된 초기 alpha와 무관하게 CanvasGroup 기본값(blocksRaycasts=true)이 남아있으면
		// 투명해도 클릭을 계속 먹어버림 — 시작 시점에 실제 alpha 기준으로 정합성 맞춤
		if (fadeOverlay != null)
			SetOverlayBlocking(fadeOverlay.alpha > 0f);
	}

	/// <summary>
	/// Scene �ε�
	/// </summary>
	/// <param name="sceneName">�� �̸�</param>
	public void LoadScene(string sceneName)			=> SceneManager.LoadScene(sceneName);
	/// <summary>
	/// Scene �ε�, �ε� ���� Fade ȣ��
	/// </summary>
	/// <param name="sceneName">�� �̸�</param>
	public void FadeAndLoad(string sceneName)		=> StartCoroutine(FadeRoutine(sceneName));

    private IEnumerator FadeRoutine(string sceneName)
	{
		yield return Fade(0f, 1f);
		SceneManager.LoadScene(sceneName);
		yield return Fade(1f, 0f);
	}

    private IEnumerator Fade(float from, float to)
	{
		// 페이드 시작 시점부터 목표가 "투명해지는" 쪽이면 즉시 클릭 통과 허용
		// (어두워지는 중엔 화면을 실제로 가리므로 계속 막아야 함)
		if (to <= 0f) SetOverlayBlocking(false);

		float t = 0f;
		while (t < fadeDuration)
		{
			t += Time.unscaledDeltaTime;	// �Ͻ����� �߿��� ���̵� ��/�ƿ� ���� �ǵ��� unscaled ���
			fadeOverlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
			yield return null;
		}
		fadeOverlay.alpha = to;

		if (to > 0f) SetOverlayBlocking(true);
	}

	/// <summary>
	/// 페이드 오버레이가 투명할 때 뒤쪽 UI 클릭을 가로채지 않도록 raycast 차단 여부를 동기화
	/// </summary>
	private void SetOverlayBlocking(bool blocking)
	{
		fadeOverlay.blocksRaycasts = blocking;
		fadeOverlay.interactable = blocking;
	}
}