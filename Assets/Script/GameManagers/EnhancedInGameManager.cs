// Assets/Script/Game/EnhancedInGameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnhancedInGameManager : MonoBehaviour
{
    public static EnhancedInGameManager Instance;

    [Header("Game State")]
    public bool isGameActive = true;
    public bool isGamePaused = false;

    [Header("Progress Tracking")]
    public int sessionCoins = 0;
    public int sessionCrystals = 0;
    public float sessionDistance = 0f;
    public int sessionPlanetsAvoided = 0;
    public int sessionStarsCollected = 0;

    [Header("UI References")]
    public GameObject levelCompleteUI;
    public GameObject gameOverUI;
    public GameObject pauseUI;

    [Header("Distance Tracking")]
    public Transform playerTransform;
    private Vector3 lastPlayerPosition;
    private float totalDistanceTraveled = 0f;

    // References to other systems
    private MissionManager missionManager;
    private PlayerHealth playerHealth;

    // Events
    public System.Action OnGameStart;
    public System.Action OnGameEnd;
    public System.Action OnGamePause;
    public System.Action OnGameResume;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeGame();

        // Find references
        missionManager = MissionManager.Instance;
        playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerTransform != null)
            lastPlayerPosition = playerTransform.position;

        // Subscribe to mission events
        if (missionManager != null)
        {
            missionManager.OnMissionComplete += OnMissionCompleted;
        }

        // Subscribe to player health events
        if (playerHealth != null)
        {
            // Player health should call OnPlayerDeath when player dies
        }

        OnGameStart?.Invoke();
    }

    void Update()
    {
        if (!isGameActive || isGamePaused) return;

        // Track distance traveled
        TrackDistance();

        // Update mission manager with distance
        if (missionManager != null)
        {
            missionManager.OnDistanceTraveled(Time.deltaTime * 5f); // Approximate distance per frame
        }
    }

    void OnDestroy()
    {
        if (missionManager != null)
        {
            missionManager.OnMissionComplete -= OnMissionCompleted;
        }
    }

    private void InitializeGame()
    {
        isGameActive = true;
        isGamePaused = false;
        sessionCoins = 0;
        sessionCrystals = 0;
        sessionDistance = 0f;
        sessionPlanetsAvoided = 0;
        sessionStarsCollected = 0;
        totalDistanceTraveled = 0f;

        // Hide UI panels
        if (levelCompleteUI != null) levelCompleteUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (pauseUI != null) pauseUI.SetActive(false);

        Time.timeScale = 1f;
    }

    private void TrackDistance()
    {
        if (playerTransform != null)
        {
            float distanceThisFrame = Vector3.Distance(playerTransform.position, lastPlayerPosition);
            sessionDistance += distanceThisFrame;
            totalDistanceTraveled += distanceThisFrame;
            lastPlayerPosition = playerTransform.position;
        }
    }

    // Called by collectible systems
    public void AddCoins(int amount)
    {
        sessionCoins += amount;

        // Notify mission manager
        if (missionManager != null)
        {
            missionManager.OnCoinCollected(amount);
        }

        // Update global economy
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddCoins(amount);
        }

        Debug.Log($"[InGameManager] Coins collected: {sessionCoins}");
    }

    public void OnCrystalCollected(int amount)
    {
        sessionCrystals += amount;

        // Notify mission manager
        if (missionManager != null)
        {
            missionManager.OnCrystalCollected(amount);
        }

        Debug.Log($"[InGameManager] Crystals collected: {sessionCrystals}");
    }

    public void OnStarCollected(int amount)
    {
        sessionStarsCollected += amount;

        // Notify mission manager
        if (missionManager != null)
        {
            missionManager.OnStarCollected(amount);
        }

        Debug.Log($"[InGameManager] Stars collected: {sessionStarsCollected}");
    }

    public void OnPlanetAvoided()
    {
        sessionPlanetsAvoided++;

        // Notify mission manager
        if (missionManager != null)
        {
            missionManager.OnPlanetAvoided(1);
        }

        Debug.Log($"[InGameManager] Planets avoided: {sessionPlanetsAvoided}");
    }

    // Called by PlayerHealth or other death systems
    public void OnPlayerDeath()
    {
        if (!isGameActive) return;

        Debug.Log("[InGameManager] Player died!");
        EndGame(false);
    }

    // Called by MissionManager when mission is completed
    private void OnMissionCompleted()
    {
        Debug.Log("[InGameManager] Mission completed!");
        EndGame(true);
    }

    private void EndGame(bool missionCompleted)
    {
        if (!isGameActive) return;

        isGameActive = false;

        // Stop time or reduce speed for dramatic effect
        StartCoroutine(SlowTimeAndShowResults(missionCompleted));

        OnGameEnd?.Invoke();
    }

    private IEnumerator SlowTimeAndShowResults(bool missionCompleted)
    {
        // Slow time for dramatic effect
        float originalTimeScale = Time.timeScale;
        float targetTimeScale = 0.1f;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Time.timeScale = Mathf.Lerp(originalTimeScale, targetTimeScale, t);
            yield return null;
        }

        Time.timeScale = targetTimeScale;
        yield return new WaitForSecondsRealtime(1f);

        // Show appropriate UI
        if (missionCompleted)
        {
            ShowLevelCompleteUI();
        }
        else
        {
            ShowGameOverUI();
        }

        Time.timeScale = 0f; // Pause completely
    }

    private void ShowLevelCompleteUI()
    {
        if (levelCompleteUI != null)
        {
            levelCompleteUI.SetActive(true);

            // Setup level complete UI with results
            var levelCompleteScreen = levelCompleteUI.GetComponent<LevelCompleteScreen>();
            if (levelCompleteScreen != null)
            {
                levelCompleteScreen.ShowResults(
                    missionManager?.GetStarsEarned() ?? 0,
                    sessionCoins,
                    sessionCrystals,
                    Mathf.RoundToInt(sessionDistance)
                );
            }
        }
    }

    private void ShowGameOverUI()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);

            // You can setup game over UI with session stats here
        }
    }

    // UI Button Methods
    public void PauseGame()
    {
        if (!isGameActive) return;

        isGamePaused = true;
        Time.timeScale = 0f;

        if (pauseUI != null)
            pauseUI.SetActive(true);

        OnGamePause?.Invoke();
    }

    public void ResumeGame()
    {
        if (!isGameActive) return;

        isGamePaused = false;
        Time.timeScale = 1f;

        if (pauseUI != null)
            pauseUI.SetActive(false);

        OnGameResume?.Invoke();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Adjust scene name as needed
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;

        // Load next level
        string currentLevelId = LevelLoader.CurrentLevelId;
        int currentLevelNumber = LevelLoader.CurrentLevelNumber;

        // Simple next level logic - you can make this more sophisticated
        string nextLevelId = $"level_{currentLevelNumber + 1}";
        int nextLevelNumber = currentLevelNumber + 1;

        // Check if next level exists and is unlocked
        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null && levelManager.IsUnlocked(nextLevelId))
        {
            LevelLoader.LoadLevel(nextLevelId, nextLevelNumber);
        }
        else
        {
            // Go back to level selection
            GoToMainMenu();
        }
    }

    // Public getters for UI
    public int GetSessionCoins() => sessionCoins;
    public int GetSessionCrystals() => sessionCrystals;
    public float GetSessionDistance() => sessionDistance;
    public int GetSessionPlanetsAvoided() => sessionPlanetsAvoided;
    public int GetSessionStarsCollected() => sessionStarsCollected;
    public bool IsGameActive() => isGameActive;
    public bool IsGamePaused() => isGamePaused;
}