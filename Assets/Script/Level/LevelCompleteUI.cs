using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// COMPLETE FIXED VERSION - Level Complete UI Manager
/// Handles UI display, stars animation, fade transition, sound, dan navigation
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [Header("UI References - DRAG FROM HIERARCHY")]
    [Tooltip("Panel Level Complete GameObject dari hierarchy ContentPanel")]
    public GameObject levelCompletePanel;

    [Tooltip("Button Continue Level dari hierarchy")]
    public Button continueButton;

    [Tooltip("Button Home dari hierarchy")]
    public Button homeButton;

    [Header("Stars Display")]
    [Tooltip("Star GameObjects (3 stars) - Stars (1), Stars (2), Stars (3)")]
    public GameObject[] starObjects;

    [Tooltip("Star Images untuk ganti sprite")]
    public Image[] starImages;

    [Tooltip("Star sprite - filled (kuning)")]
    public Sprite starFilled;

    [Tooltip("Star sprite - empty (abu-abu/outline)")]
    public Sprite starEmpty;

    [Header("Fade Settings")]
    [Tooltip("Image untuk fade overlay (hitam, alpha 0-1) - di root Canvas")]
    public Image fadeImage;

    [Tooltip("Durasi fade out saat transition (detik)")]
    public float fadeOutDuration = 1f;

    [Tooltip("Durasi fade in saat scene start (detik)")]
    public float fadeInDuration = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Delay per star untuk animasi sequence")]
    public float starAnimationDelay = 0.3f;

    [Tooltip("Scale animation untuk stars")]
    public float starScalePunch = 1.3f;

    [Tooltip("Animation duration")]
    public float starAnimationDuration = 0.3f;

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

        // Auto-find star images if not assigned
        if (starImages == null || starImages.Length == 0)
        {
            starImages = new Image[3];
            if (starObjects != null)
            {
                for (int i = 0; i < starObjects.Length && i < 3; i++)
                {
                    if (starObjects[i] != null)
                    {
                        starImages[i] = starObjects[i].GetComponent<Image>();
                    }
                }
            }
        }

        // Subscribe to level complete event
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Subscribed to OnLevelCompleted");
        }
        else
        {
            LogWarning("LevelGameSession.Instance not found! Searching...");
            StartCoroutine(LateSubscribe());
        }

        // Fade in saat scene mulai
        StartCoroutine(FadeIn());
    }

    IEnumerator LateSubscribe()
    {
        yield return new WaitForSeconds(0.5f);

        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Late subscribed to OnLevelCompleted");
        }
        else
        {
            LogWarning("LevelGameSession still not found after delay!");
        }
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
    public void OnLevelComplete()
    {
        if (isTransitioning) return;

        Log("🎉 Level Complete triggered!");

        // ✅ CRITICAL: Stop spawner
        StopAllSpawners();

        // ✅ Play level complete sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLevelComplete();
            Log("✓ Playing level complete sound");
        }

        // Get earned stars
        if (GameplayStarManager.Instance != null)
        {
            earnedStars = GameplayStarManager.Instance.GetCollectedStars();
            Log($"Earned stars: {earnedStars}");
        }
        else
        {
            earnedStars = 0;
            LogWarning("GameplayStarManager not found!");
        }

        // Get current level number
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);
        Log($"Current level: {currentLevelNumber}");

        // Show UI dengan delay
        StartCoroutine(ShowLevelCompleteUI());
    }

    /// <summary>
    /// Stop semua spawner di scene
    /// </summary>
    void StopAllSpawners()
    {
        Log("Stopping all spawners...");

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

        // Method 3: Stop all active spawner coroutines
        var allSpawners = FindObjectsByType<FixedGameplaySpawner>(FindObjectsSortMode.None);
        foreach (var s in allSpawners)
        {
            if (s != null)
            {
                s.StopAllCoroutines();
                s.enabled = false;
            }
        }

        Log("✓ All spawners stopped");
    }

    /// <summary>
    /// Show level complete UI dengan animasi
    /// </summary>
    IEnumerator ShowLevelCompleteUI()
    {
        // Wait sedikit untuk efek dramatis
        yield return new WaitForSeconds(0.5f);

        // Show panel
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            Log("✓ Level complete panel shown");
        }
        else
        {
            LogWarning("levelCompletePanel is NULL!");
            yield break;
        }

        // Reset all stars to empty first
        ResetStars();

        // Animate stars dengan sequence
        yield return StartCoroutine(AnimateStars());

        Log("✓ Level complete UI fully shown");
    }

    /// <summary>
    /// Reset semua stars ke empty sprite
    /// </summary>
    void ResetStars()
    {
        if (starImages == null || starEmpty == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = starEmpty;
                starImages[i].transform.localScale = Vector3.one;

                if (starObjects != null && i < starObjects.Length && starObjects[i] != null)
                {
                    starObjects[i].SetActive(true);
                }
            }
        }

        Log("✓ Stars reset to empty");
    }

    /// <summary>
    /// Animate stars dengan sequence (satu per satu)
    /// </summary>
    IEnumerator AnimateStars()
    {
        if (starImages == null || starFilled == null)
        {
            LogWarning("Star images or starFilled sprite not assigned!");
            yield break;
        }

        for (int i = 0; i < earnedStars && i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;

            // Wait before showing next star
            yield return new WaitForSeconds(starAnimationDelay);

            // Change sprite to filled (kuning)
            starImages[i].sprite = starFilled;

            // Punch scale animation
            StartCoroutine(PunchScale(starImages[i].transform));

            // Play star pickup sound (reuse existing sound)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStarPickup();
            }

            Log($"✓ Star {i + 1} animated");
        }

        Log($"✓ All {earnedStars} stars animated");
    }

    /// <summary>
    /// Punch scale animation untuk star
    /// </summary>
    IEnumerator PunchScale(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float elapsed = 0f;
        float halfDuration = starAnimationDuration / 2f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale, originalScale * starScalePunch, t);
            yield return null;
        }

        elapsed = 0f;

        // Scale down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale * starScalePunch, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    /// <summary>
    /// Called ketika button Continue diklik
    /// </summary>
    public void OnContinueClicked()
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
    public void OnHomeClicked()
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

    [ContextMenu("Test: Animate Stars (3 stars)")]
    public void TestAnimateStars()
    {
        earnedStars = 3;
        ResetStars();
        StartCoroutine(AnimateStars());
    }
}