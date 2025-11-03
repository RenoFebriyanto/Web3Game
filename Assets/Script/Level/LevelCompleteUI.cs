using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manager untuk UI Level Complete dengan animasi fade dan sound
/// Attach ke Canvas di Gameplay scene
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel Level Complete (aktifkan saat level selesai)")]
    public GameObject levelCompletePanel;

    [Tooltip("Button Continue")]
    public Button continueButton;

    [Tooltip("Text untuk display stars (opsional)")]
    public TMP_Text starsText;

    [Tooltip("Star icons untuk visual")]
    public GameObject[] starIcons;

    [Header("Fade Overlay")]
    [Tooltip("Image untuk fade (warna hitam, alpha 0-1)")]
    public Image fadeImage;

    [Tooltip("Durasi fade out (detik)")]
    public float fadeOutDuration = 1f;

    [Tooltip("Durasi fade in (detik)")]
    public float fadeInDuration = 0.5f;

    [Header("Scene Settings")]
    [Tooltip("Nama scene untuk kembali ke main menu")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private bool isTransitioning = false;
    private int earnedStars = 0;

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

        // Setup button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
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
    /// Called ketika level complete
    /// </summary>
    void OnLevelComplete()
    {
        if (isTransitioning) return;

        Log("Level Complete triggered!");

        // Stop spawner
        StopAllSpawners();

        // Play level complete sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLevelComplete();
        }

        // Get earned stars
        if (GameplayStarManager.Instance != null)
        {
            earnedStars = GameplayStarManager.Instance.GetCollectedStars();
        }

        // Show UI
        StartCoroutine(ShowLevelCompleteUI());
    }

    /// <summary>
    /// Stop semua spawner di scene
    /// </summary>
    void StopAllSpawners()
    {
        // Stop FixedGameplaySpawner
        var spawner = FindFirstObjectByType<FixedGameplaySpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
            Log("✓ Stopped spawner");
        }

        // Stop semua coroutine yang berhubungan dengan spawn
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

        // Pause game (optional)
        // Time.timeScale = 0f; // Jangan pause jika ada animasi
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
    /// Transition ke level berikutnya dengan fade
    /// </summary>
    IEnumerator TransitionToNextLevel()
    {
        isTransitioning = true;

        // Resume time jika di-pause
        Time.timeScale = 1f;

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Get current level number
        int currentLevel = PlayerPrefs.GetInt("SelectedLevelNumber", 1);
        int nextLevel = currentLevel + 1;

        // Check if next level exists
        LevelConfig nextLevelConfig = null;
        if (FindFirstObjectByType<LevelPopulator>() != null)
        {
            var database = FindFirstObjectByType<LevelPopulator>().levelDatabase;
            if (database != null)
            {
                nextLevelConfig = database.GetByNumber(nextLevel);
            }
        }

        if (nextLevelConfig != null)
        {
            // Load next level
            Log($"Loading next level: {nextLevel}");
            PlayerPrefs.SetString("SelectedLevelId", nextLevelConfig.id);
            PlayerPrefs.SetInt("SelectedLevelNumber", nextLevelConfig.number);
            PlayerPrefs.Save();

            // Reload gameplay scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // No more levels, back to main menu
            Log("No more levels, returning to main menu");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// Fade out (hitam)
    /// </summary>
    IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        Color startColor = fadeImage.color;
        startColor.a = 0f;
        Color endColor = startColor;
        endColor.a = 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    /// <summary>
    /// Fade in (transparan)
    /// </summary>
    IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

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
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[LevelCompleteUI] {message}");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Manual trigger level complete (untuk testing)
    /// </summary>
    [ContextMenu("Test: Trigger Level Complete")]
    public void TestLevelComplete()
    {
        OnLevelComplete();
    }
}