using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 패널 — 창모드 토글 + 마스터 볼륨.
/// PlayerPrefs에 저장하고, 씬을 새로 열 때마다(타이틀 재방문 등) 저장된 값을 다시 적용한다.
/// 볼륨은 SoundManager.SetMasterVolume을 통해서만 건드림 (오디오는 SoundManager 경유 규칙).
/// </summary>
public class SettingsMenu : MonoBehaviour
{
	const string FullscreenKey	= "Settings_Fullscreen";
	const string VolumeKey		= "Settings_Volume";

	[Header("UI 참조 (비워도 되지만, 채우면 패널 열 때 저장된 값으로 자동 동기화됨)")]
	[SerializeField] private Toggle fullscreenToggle;
	[SerializeField] private Slider volumeSlider;

	[Header("옵션 패널 오브젝트 (열기/닫기 버튼용, 비우면 스킵)")]
	[SerializeField] private GameObject panel;

	private void Start()
	{
		bool isFullscreen	= PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
		float volume			= PlayerPrefs.GetFloat(VolumeKey, 1f);

		ApplyFullscreen(isFullscreen);
		ApplyVolume(volume);

		if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
		if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(volume);
	}

	/// <summary>창모드 토글 UI의 OnValueChanged에 연결</summary>
	public void OnFullscreenChanged(bool isFullscreen)
	{
		ApplyFullscreen(isFullscreen);
		PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
	}

	/// <summary>볼륨 슬라이더의 OnValueChanged에 연결</summary>
	public void OnVolumeChanged(float volume)
	{
		ApplyVolume(volume);
		PlayerPrefs.SetFloat(VolumeKey, volume);
	}

	private void ApplyFullscreen(bool isFullscreen)
		=> Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

	private void ApplyVolume(float volume)
		=> SoundManager.Instance.SetMasterVolume(volume);

	/// <summary>옵션 패널 열기/닫기 버튼용 (비우고 안 써도 무방)</summary>
	public void TogglePanel()
	{
		if (panel != null) panel.SetActive(!panel.activeSelf);
	}
}
