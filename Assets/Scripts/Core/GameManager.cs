using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, Dead, Ending }

/// <summary>
/// ���� ���� ������ �����ϴ� �ּ� �Ŵ���
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    [Header("일시정지 화면")]
    [SerializeField] private GameObject pauseScreen;
    private bool isPaused = false;
    
    

    private void Awake()
    {
        // 중복이면 "이 컴포넌트만" 제거 — 같은 오브젝트의 씬-로컬 매니저들을 같이 죽이지 않게
        if (Instance != null && Instance != this) { Destroy(this); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void OnEnable()
    {
        EventBus.OnPlayerDeath += HandleDeath;
        SceneManager.sceneLoaded += HandleSceneLoaded;

    }

    private void OnDisable()
    {
        EventBus.OnPlayerDeath -= HandleDeath;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }


    private void Update()
    {
        // Null 에러 방지 + 업데이트 무한 호출 방지
        if (pauseScreen != null && !isPaused && Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = true;
            Pause();
        }
    }

    private void HandleDeath() => CurrentState = GameState.Dead;

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
        if (pauseScreen == null) return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        pauseScreen.SetActive(true);
    }


    /// <summary>
    /// �Ͻ����� ���� �� ���� �簳
    /// </summary>
    public void Resume()
    {
        if (pauseScreen == null) return;

        isPaused = false;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        pauseScreen.SetActive(false);
    }

    /// <summary>
    /// 엔딩 연출 진입 시 상태 전환
    /// </summary>
    public void SetEnding()
    {
        CurrentState = GameState.Ending;
    }
}
