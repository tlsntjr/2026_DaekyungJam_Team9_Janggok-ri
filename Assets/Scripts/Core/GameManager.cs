using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, Dead, Ending }

/// <summary>
/// ���� ���� ������ �����ϴ� �ּ� �Ŵ���
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
	/// �� ���� ���� �÷��� ������Ʈ ����
	/// </summary>
	/// <param name="scene"></param>
	/// <param name="mode"></param>
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		CurrentState = GameState.Playing;
		Time.timeScale = 1f;
	}

	/// <summary>
	/// ���� �Ͻ� ����
	/// </summary>
    public void Pause()
	{
		CurrentState = GameState.Paused;
		Time.timeScale = 0f;
	}


	/// <summary>
	/// �Ͻ����� ���� �� ���� �簳
	/// </summary>
	public void Resume()
	{
		CurrentState = GameState.Playing;
		Time.timeScale = 1f;
	}

	/// <summary>
	/// 엔딩 연출 진입 시 상태 전환
	/// </summary>
	public void SetEnding()
	{
		CurrentState = GameState.Ending;
	}
}

