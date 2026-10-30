using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Conserva la partida; las escenas contienen sus objetos y configuraci?n.</summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int world { get; private set; } = 1;
    public int stage { get; private set; } = 1;
    public int lives { get; private set; } = 3;
    public int coins { get; private set; }
    public bool IsTransitioning { get; private set; }
    public bool IsResetPending { get; private set; }
    private SceneTransition transition;
    private PlayerState pendingState;
    private bool restoreState;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        transition = gameObject.AddComponent<SceneTransition>();
        if (GetComponent<GameAudio>() == null) gameObject.AddComponent<GameAudio>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (Instance != this) return;
        Application.targetFrameRate = 60;
        // Abrir 1-2 desde el editor no debe iniciar de nuevo 1-1.
        ConfigureScene();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ConfigureScene();

    private void ConfigureScene()
    {
        LevelSettings settings = FindFirstObjectByType<LevelSettings>();
        if (settings != null) { world = settings.world; stage = settings.stage; settings.Apply(); }
        Player player = FindFirstObjectByType<Player>();
        if (player != null && restoreState) player.RestoreState(pendingState);
        // sceneLoaded ocurre antes del primer Update del jugador recién creado.
        if (player != null && IsTransitioning) player.SetTransitioning(true);
        restoreState = false;
    }

    public void NewGame()
    {
        if (IsTransitioning) return;
        CancelInvoke(nameof(ResetLevel));
        IsResetPending = false;
        lives = 3;
        coins = 0;
        BeginLoad(1, 1, default);
    }

    // La escena de Game Over se incorporar? en la tercera parte.
    public void GameOver() => NewGame();

    public void LoadLevel(int world, int stage)
    {
        if (IsTransitioning || IsResetPending) return;
        Player player = FindFirstObjectByType<Player>();
        BeginLoad(world, stage, player != null ? player.CaptureState() : default);
    }

    private void BeginLoad(int nextWorld, int nextStage, PlayerState state)
    {
        string sceneName = $"{nextWorld}-{nextStage}";
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena {sceneName} no est? registrada en la compilaci?n.");
            return;
        }
        IsTransitioning = true; // Un segundo contacto no inicia otra carga.
        StartCoroutine(LoadRoutine(sceneName, state));
    }

    private IEnumerator LoadRoutine(string sceneName, PlayerState state)
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null) player.SetTransitioning(true);
        yield return transition.FadeTo(1f);
        GameAudio.Instance?.StopMusic();
        pendingState = state;
        restoreState = true;
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        player = FindFirstObjectByType<Player>();
        if (player != null) player.SetTransitioning(true);
        yield return transition.FadeTo(0f);
        if (player != null) player.SetTransitioning(false);
        IsTransitioning = false;
    }

    public void NextLevel() => LoadLevel(world, stage + 1);

    public void ResetLevel(float delay)
    {
        if (IsResetPending || IsTransitioning) return;
        IsResetPending = true;
        Invoke(nameof(ResetLevel), Mathf.Max(0f, delay));
    }

    public void ResetLevel()
    {
        if (IsTransitioning) return;
        CancelInvoke(nameof(ResetLevel));
        IsResetPending = false;
        lives--;
        if (lives <= 0) GameOver();
        else BeginLoad(world, stage, default);
    }

    public void AddCoin()
    {
        coins++;
        GameAudio.Instance?.Play(SoundEffect.Coin);
        if (coins >= 100) { coins -= 100; AddLife(); }
    }

    public void AddLife()
    {
        lives++;
        GameAudio.Instance?.Play(SoundEffect.ExtraLife);
    }
}
