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
	}

	/// <summary>
	/// Scene 로드
	/// </summary>
	/// <param name="sceneName">씬 이름</param>
	public void LoadScene(string sceneName)			=> SceneManager.LoadScene(sceneName);
	/// <summary>
	/// Scene 로드, 로드 이전 Fade 호출
	/// </summary>
	/// <param name="sceneName">씬 이름</param>
	public void FadeAndLoad(string sceneName)		=> StartCoroutine(FadeRoutine(sceneName));

    private IEnumerator FadeRoutine(string sceneName)
	{
		yield return Fade(0f, 1f);
		SceneManager.LoadScene(sceneName);
		yield return Fade(1f, 0f);
	}

    private IEnumerator Fade(float from, float to)
	{
		float t = 0f;
		while (t < fadeDuration)
		{
			t += Time.unscaledDeltaTime;	// 일시정지 중에도 페이드 인/아웃 동작 되도록 unscaled 사용
			fadeOverlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
			yield return null;
		}
		fadeOverlay.alpha = to;
	}
}