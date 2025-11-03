using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// COMPLETE FIXED VERSION - Level Complete UI Manager
/// Handles UI display, fade transition, sound, dan navigation
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [Header("UI References - DRAG FROM HIERARCHY")]
    [Tooltip("Panel Level Complete GameObject dari hierarchy Anda")]
    public GameObject levelCompletePanel;

    [Tooltip("Button Continue Level dari hierarchy")]
    public Button continueButton;

    [Tooltip("Button Home dari hierarchy")]
    public Button homeButton;

    [Tooltip("Text untuk display stars (opsional)")]
    public TMP_Text starsText;

    [Tooltip("Star icons untuk visual (3 stars)")]
    public GameObject[] starIcons;

    [Header("Fade Settings")]
    [Tooltip("Image untuk fade overlay (hitam, alpha 0-1)")]
    public Image fadeImage;

    [Tooltip("Durasi fade out saat transition (detik)")]
    public float fadeOutDuration = 1f;

    [Tooltip("Durasi fade in saat scene start (detik)")]
    public float fadeInDuration = 0.5f;

    [Header("Scene Settings")]
    [Tooltip("Nama scene Main Menu")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Nama scene Gameplay")]
    public string gameplaySceneName = "Gameplay";

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private bool isTransitioning = false;
    private int earnedStars = 0;
    private int currentLevelNumber = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Hide panel initially
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        // Setup fade image (transparent initially)
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }

        // Setup buttons
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(OnHomeClicked);
        }

        // Subscribe to level complete event
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Subscribed to OnLevelCompleted");
        }

        // Fade in saat scene mulai
        StartCoroutine(FadeIn());
    }

    void OnDestroy()
    {
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.RemoveListener(OnLevelComplete);
        }
    }

    /// <summary>
    /// Called ketika level complete (via LevelGameSession event)
    /// </summary>
    void OnLevelComplete()
    {
        if (isTransitioning) return;

        Log("Level Complete triggered!");

        // ✅ CRITICAL: Stop spawner
        StopAllSpawners();

        // ✅ Play level complete sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLevelComplete();
        }

        // Get earned stars
        if (GameplayStarManager.Instance != null)
        {
            earnedStars = GameplayStarManager.Instance.GetCollectedStars();
        }

        // Get current level number
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        // Show UI dengan delay
        StartCoroutine(ShowLevelCompleteUI());
    }

    /// <summary>
    /// Stop semua spawner di scene
    /// </summary>
    void StopAllSpawners()
    {
        // Method 1: Via SpawnerController
        if (SpawnerController.Instance != null)
        {
            SpawnerController.Instance.StopSpawner();
            Log("✓ Stopped spawner via SpawnerController");
        }

        // Method 2: Direct disable FixedGameplaySpawner
        var spawner = FindFirstObjectByType<FixedGameplaySpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
            Log("✓ Disabled FixedGameplaySpawner");
        }

        // Method 3: Stop all coroutines (safety measure)
        StopAllCoroutines();
    }

    /// <summary>
    /// Show level complete UI dengan delay
    /// </summary>
    IEnumerator ShowLevelCompleteUI()
    {
        // Wait sedikit untuk efek dramatis
        yield return new WaitForSeconds(0.5f);

        // Show panel
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        // Update stars display
        UpdateStarsDisplay();

        Log("✓ Level complete UI shown");
    }

    /// <summary>
    /// Update display bintang
    /// </summary>
    void UpdateStarsDisplay()
    {
        // Update text
        if (starsText != null)
        {
            starsText.text = $"{earnedStars}/3";
        }

        // Update star icons
        if (starIcons != null)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].SetActive(i < earnedStars);
                }
            }
        }

        Log($"Stars displayed: {earnedStars}");
    }

    /// <summary>
    /// Called ketika button Continue diklik
    /// </summary>
    void OnContinueClicked()
    {
        if (isTransitioning) return;

        Log("Continue button clicked");

        // Play click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        StartCoroutine(TransitionToNextLevel());
    }

    /// <summary>
    /// Called ketika button Home diklik
    /// </summary>
    void OnHomeClicked()
    {
        if (isTransitioning) return;

        Log("Home button clicked");

        // Play click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        StartCoroutine(TransitionToMainMenu());
    }

    /// <summary>
    /// Transition ke level berikutnya dengan fade
    /// </summary>
    IEnumerator TransitionToNextLevel()
    {
        isTransitioning = true;

        // Resume time jika di-pause
        Time.timeScale = 1f;

        // Fade out
        yield return StartCoroutine(FadeOut());

        int nextLevel = currentLevelNumber + 1;

        // ✅ Check if next level exists (max 100 levels)
        if (nextLevel <= 100)
        {
            // Load next level
            Log($"Loading next level: {nextLevel}");

            // Set selected level
            PlayerPrefs.SetString("SelectedLevelId", $"level_{nextLevel}");
            PlayerPrefs.SetInt("SelectedLevelNumber", nextLevel);
            PlayerPrefs.Save();

            // Reload gameplay scene
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            // No more levels, back to main menu
            Log("No more levels (reached level 100), returning to main menu");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// Transition ke main menu dengan fade
    /// </summary>
    IEnumerator TransitionToMainMenu()
    {
        isTransitioning = true;

        // Resume time jika di-pause
        Time.timeScale = 1f;

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Load main menu
        Log("Loading main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Fade out (hitam)
    /// </summary>
    IEnumerator FadeOut()
    {
        if (fadeImage == null)
        {
            LogWarning("Fade image not assigned! Skipping fade out.");
            yield break;
        }

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        Color startColor = fadeImage.color;
        startColor.a = 0f;
        Color endColor = startColor;
        endColor.a = 1f;

        fadeImage.color = startColor;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
        Log("✓ Fade out complete");
    }

    /// <summary>
    /// Fade in (transparan) - called on scene start
    /// </summary>
    IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            LogWarning("Fade image not assigned! Skipping fade in.");
            yield break;
        }

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        Color startColor = fadeImage.color;
        startColor.a = 1f;
        Color endColor = startColor;
        endColor.a = 0f;

        fadeImage.color = startColor;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
        fadeImage.gameObject.SetActive(false);
        Log("✓ Fade in complete");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[LevelCompleteUI] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[LevelCompleteUI] {message}");
    }

    // ========================================
    // PUBLIC API - FOR TESTING
    // ========================================

    /// <summary>
    /// Manual trigger level complete (untuk testing)
    /// </summary>
    [ContextMenu("Test: Trigger Level Complete")]
    public void TestLevelComplete()
    {
        OnLevelComplete();
    }

    [ContextMenu("Test: Fade Out")]
    public void TestFadeOut()
    {
        StartCoroutine(FadeOut());
    }

    [ContextMenu("Test: Fade In")]
    public void TestFadeIn()
    {
        StartCoroutine(FadeIn());
    }
}