using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, Dead, Ending }

/// <summary>
/// 전역 상태 정도만 관리하는 최소 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
	public static GameManager Instance	{ get; private set; }
	public GameState CurrentState			{ get; private set; } = GameState.Playing;

    private void Awake()
	{
		if (Instance != null)	{ Destroy(gameObject); return; }

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}


	private void OnEnable()
	{
		EventBus.OnPlayerDeath			+= HandleDeath;
		SceneManager.sceneLoaded	+= HandleSceneLoaded;

    }

	private void OnDisable()
	{
		EventBus.OnPlayerDeath			-= HandleDeath;
        SceneManager.sceneLoaded	-= HandleSceneLoaded;
    }
    private void HandleDeath()	=> CurrentState = GameState.Dead;

	/// <summary>
	/// 씬 복원 이후 플레잉 스테이트 유지
	/// </summary>
	/// <param name="scene"></param>
	/// <param name="mode"></param>
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		CurrentState = GameState.Playing;
		Time.timeScale = 1f;
	}

	/// <summary>
	/// 게임 일시 정지
	/// </summary>
    public void Pause()
	{
		CurrentState = GameState.Paused;
		Time.timeScale = 0f;
	}


	/// <summary>
	/// 일시정지 해제 및 게임 재개
	/// </summary>
	public void Resume()
	{
		CurrentState = GameState.Playing;
		Time.timeScale = 1f;
	}

	/// <summary>
	/// </summary>
	public void SetEnding()
	{
		CurrentState = GameState.Ending;
	}
}

