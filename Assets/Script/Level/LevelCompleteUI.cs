using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UPDATED: Level Complete UI Manager dengan:
/// - Character Lino (Happy/Sad based on stars)
/// - Random Rewards (Coin/Energy)
/// - LEVEL COMPLETE popup animation
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

    [Header("🎭 Character Lino")]
    [Tooltip("GameObject/Image untuk Lino Happy (ditampilkan saat dapat 1-3 bintang)")]
    public GameObject linoHappy;

    [Tooltip("GameObject/Image untuk Lino Sad (ditampilkan saat dapat 0 bintang)")]
    public GameObject linoSad;

    [Header("Stars Display")]
    [Tooltip("Star GameObjects (3 stars) - Stars (1), Stars (2), Stars (3)")]
    public GameObject[] starObjects;

    [Tooltip("Star Images untuk ganti sprite")]
    public Image[] starImages;

    [Tooltip("Star sprite - filled (kuning)")]
    public Sprite starFilled;

    [Tooltip("Star sprite - empty (abu-abu/outline)")]
    public Sprite starEmpty;

    [Header("🎉 LEVEL COMPLETE Popup")]
    [Tooltip("GameObject popup 'LEVEL COMPLETE' (parent of background + text)")]
    public GameObject lvlPopup;

    [Tooltip("RectTransform background yang akan scale horizontal")]
    public RectTransform lvlPopupBackground;

    [Tooltip("Text 'LEVEL COMPLETE'")]
    public TMP_Text lvlPopupText;

    [Tooltip("Durasi animasi background expand (detik)")]
    public float lvlPopupExpandDuration = 0.4f;

    [Tooltip("Durasi popup ditampilkan sebelum hilang (detik)")]
    public float lvlPopupDisplayDuration = 0.8f;

    [Header("🎁 Random Rewards")]
    [Tooltip("Container untuk display rewards (parent of reward items)")]
    public GameObject rewardsContainer;

    [Tooltip("Prefab untuk reward item (harus ada Image icon + TMP_Text amount)")]
    public GameObject rewardItemPrefab;

    [Tooltip("Sprite icon Coin")]
    public Sprite coinIcon;

    [Tooltip("Sprite icon Energy")]
    public Sprite energyIcon;

    [Header("Reward Settings")]
    [Tooltip("Minimum rewards yang bisa didapat (1 atau 2)")]
    public int minRewards = 1;

    [Tooltip("Maximum rewards yang bisa didapat (1 atau 2)")]
    public int maxRewards = 2;

    [Tooltip("Range coin reward (min-max)")]
    public Vector2Int coinRewardRange = new Vector2Int(100, 10000);

    [Tooltip("Range energy reward (min-max)")]
    public Vector2Int energyRewardRange = new Vector2Int(10, 100);

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
    private List<RewardData> generatedRewards = new List<RewardData>();

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

        // Hide LVLPOPUP initially
        if (lvlPopup != null)
        {
            lvlPopup.SetActive(false);
        }

        // Hide rewards container initially
        if (rewardsContainer != null)
        {
            rewardsContainer.SetActive(false);
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
            continueButton.interactable = false; // Disable until animations complete
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(OnHomeClicked);
            homeButton.interactable = false; // Disable until animations complete
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

        // Stop spawner
        StopAllSpawners();

        // Play level complete sound
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

        // Generate random rewards
        GenerateRandomRewards();

        // Show UI dengan delay
        StartCoroutine(ShowLevelCompleteUI());
    }

    /// <summary>
    /// Stop semua spawner di scene
    /// </summary>
    void StopAllSpawners()
    {
        Log("Stopping all spawners...");

        if (SpawnerController.Instance != null)
        {
            SpawnerController.Instance.StopSpawner();
            Log("✓ Stopped spawner via SpawnerController");
        }

        var spawner = FindFirstObjectByType<FixedGameplaySpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
            Log("✓ Disabled FixedGameplaySpawner");
        }

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
    /// 🎁 Generate random rewards (1-2 rewards dengan random amount)
    /// </summary>
    void GenerateRandomRewards()
    {
        generatedRewards.Clear();

        // Random number of rewards (1 or 2)
        int rewardCount = Random.Range(minRewards, maxRewards + 1);
        Log($"Generating {rewardCount} reward(s)...");

        // Available reward types
        List<RewardType> availableTypes = new List<RewardType> { RewardType.Coin, RewardType.Energy };

        for (int i = 0; i < rewardCount; i++)
        {
            if (availableTypes.Count == 0) break;

            // Pick random reward type
            int typeIndex = Random.Range(0, availableTypes.Count);
            RewardType type = availableTypes[typeIndex];

            // Remove from available (no duplicate types)
            availableTypes.RemoveAt(typeIndex);

            // Generate random amount based on type
            int amount = 0;
            switch (type)
            {
                case RewardType.Coin:
                    amount = Random.Range(coinRewardRange.x, coinRewardRange.y + 1);
                    break;
                case RewardType.Energy:
                    amount = Random.Range(energyRewardRange.x, energyRewardRange.y + 1);
                    break;
            }

            // Add to rewards list
            RewardData reward = new RewardData
            {
                type = type,
                amount = amount
            };
            generatedRewards.Add(reward);

            Log($"✓ Generated reward: {type} x{amount}");
        }
    }

    /// <summary>
    /// Apply rewards to PlayerEconomy
    /// </summary>
    void ApplyRewards()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogWarning("PlayerEconomy not found! Cannot apply rewards.");
            return;
        }

        foreach (var reward in generatedRewards)
        {
            switch (reward.type)
            {
                case RewardType.Coin:
                    PlayerEconomy.Instance.AddCoins(reward.amount);
                    Log($"✓ Added {reward.amount} coins");
                    break;

                case RewardType.Energy:
                    PlayerEconomy.Instance.AddEnergy(reward.amount);
                    Log($"✓ Added {reward.amount} energy");
                    break;
            }
        }
    }

    /// <summary>
    /// Show level complete UI dengan animasi lengkap
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

        // ✅ NEW: Animate LEVEL COMPLETE popup FIRST
        yield return StartCoroutine(AnimateLevelCompletePopup());

        // ✅ NEW: Set Lino character based on stars
        SetLinoCharacter();

        // ✅ Existing: Animate stars dengan sequence
        yield return StartCoroutine(AnimateStars());

        // ✅ NEW: Show rewards
        yield return StartCoroutine(ShowRewards());

        // ✅ Enable buttons after all animations
        EnableButtons();

        Log("✓ Level complete UI fully shown");
    }

    /// <summary>
    /// 🎉 Animate LEVEL COMPLETE popup (background expand + text)
    /// </summary>
    IEnumerator AnimateLevelCompletePopup()
    {
        if (lvlPopup == null)
        {
            LogWarning("lvlPopup not assigned! Skipping popup animation.");
            yield break;
        }

        Log("Animating LEVEL COMPLETE popup...");

        // Show popup
        lvlPopup.SetActive(true);

        // Setup initial state
        if (lvlPopupBackground != null)
        {
            // Start with zero width
            Vector2 originalSize = lvlPopupBackground.sizeDelta;
            lvlPopupBackground.sizeDelta = new Vector2(0, originalSize.y);

            // Animate expand (horizontal scale)
            float elapsed = 0f;
            while (elapsed < lvlPopupExpandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lvlPopupExpandDuration;
                float easedT = EaseOutBack(t); // Smooth easing

                lvlPopupBackground.sizeDelta = new Vector2(
                    Mathf.Lerp(0, originalSize.x, easedT),
                    originalSize.y
                );

                yield return null;
            }

            lvlPopupBackground.sizeDelta = originalSize;
        }

        // Display for duration
        yield return new WaitForSeconds(lvlPopupDisplayDuration);

        // Hide popup
        lvlPopup.SetActive(false);

        Log("✓ LEVEL COMPLETE popup animation complete");
    }

    /// <summary>
    /// Easing function for smooth animation
    /// </summary>
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>
    /// 🎭 Set Lino character based on earned stars
    /// </summary>
    void SetLinoCharacter()
    {
        if (linoHappy == null || linoSad == null)
        {
            LogWarning("Lino character GameObjects not assigned!");
            return;
        }

        if (earnedStars == 0)
        {
            // Sad Lino (no stars)
            linoHappy.SetActive(false);
            linoSad.SetActive(true);
            Log("✓ Showing Sad Lino (0 stars)");
        }
        else
        {
            // Happy Lino (1-3 stars)
            linoHappy.SetActive(true);
            linoSad.SetActive(false);
            Log($"✓ Showing Happy Lino ({earnedStars} stars)");
        }
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
    /// Animate stars dengan sequence (satu per satu) - EXISTING LOGIC
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

            // Play star pickup sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStarPickup();
            }

            Log($"✓ Star {i + 1} animated");
        }

        Log($"✓ All {earnedStars} stars animated");
    }

    /// <summary>
    /// Punch scale animation untuk star - EXISTING LOGIC
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
    /// 🎁 Show rewards dengan animasi
    /// </summary>
    IEnumerator ShowRewards()
    {
        if (rewardsContainer == null || rewardItemPrefab == null)
        {
            LogWarning("Rewards container or prefab not assigned!");
            yield break;
        }

        if (generatedRewards.Count == 0)
        {
            LogWarning("No rewards to show!");
            yield break;
        }

        Log($"Showing {generatedRewards.Count} reward(s)...");

        // Clear existing reward items
        foreach (Transform child in rewardsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Show container
        rewardsContainer.SetActive(true);

        // Apply rewards to economy
        ApplyRewards();

        // Wait sedikit sebelum spawn reward items
        yield return new WaitForSeconds(0.3f);

        // Spawn reward items dengan animasi
        foreach (var reward in generatedRewards)
        {
            // Instantiate reward item
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardsContainer.transform);

            // Setup reward item (icon + amount text)
            SetupRewardItem(rewardItem, reward);

            // Animate reward item (scale punch)
            StartCoroutine(PunchScale(rewardItem.transform));

            // Play sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCoinPickup(); // Reuse coin sound
            }

            yield return new WaitForSeconds(0.2f);
        }

        Log("✓ All rewards shown");
    }

    /// <summary>
    /// Setup reward item UI (icon + amount)
    /// </summary>
    void SetupRewardItem(GameObject item, RewardData reward)
    {
        // Find Image component for icon
        Image iconImage = item.GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            switch (reward.type)
            {
                case RewardType.Coin:
                    if (coinIcon != null) iconImage.sprite = coinIcon;
                    break;
                case RewardType.Energy:
                    if (energyIcon != null) iconImage.sprite = energyIcon;
                    break;
            }
        }

        // Find TMP_Text component for amount
        TMP_Text amountText = item.GetComponentInChildren<TMP_Text>();
        if (amountText != null)
        {
            amountText.text = $"+{reward.amount}";
        }

        Log($"✓ Setup reward item: {reward.type} x{reward.amount}");
    }

    /// <summary>
    /// Enable buttons setelah semua animasi selesai
    /// </summary>
    void EnableButtons()
    {
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        if (homeButton != null)
        {
            homeButton.interactable = true;
        }

        Log("✓ Buttons enabled");
    }

    /// <summary>
    /// Called ketika button Continue diklik
    /// </summary>
    public void OnContinueClicked()
    {
        if (isTransitioning) return;

        Log("Continue button clicked");

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

        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

        int nextLevel = currentLevelNumber + 1;

        if (nextLevel <= 100)
        {
            Log($"Loading next level: {nextLevel}");

            PlayerPrefs.SetString("SelectedLevelId", $"level_{nextLevel}");
            PlayerPrefs.SetInt("SelectedLevelNumber", nextLevel);
            PlayerPrefs.Save();

            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
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

        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

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
    // TESTING METHODS
    // ========================================

    [ContextMenu("Test: Trigger Level Complete (0 stars)")]
    public void TestLevelComplete0Stars()
    {
        earnedStars = 0;
        currentLevelNumber = 1;
        GenerateRandomRewards();
        StartCoroutine(ShowLevelCompleteUI());
    }

    [ContextMenu("Test: Trigger Level Complete (3 stars)")]
    public void TestLevelComplete3Stars()
    {
        earnedStars = 3;
        currentLevelNumber = 1;
        GenerateRandomRewards();
        StartCoroutine(ShowLevelCompleteUI());
    }

    [ContextMenu("Test: Animate LEVEL COMPLETE Popup")]
    public void TestPopupAnimation()
    {
        StartCoroutine(AnimateLevelCompletePopup());
    }

    [ContextMenu("Test: Show Rewards")]
    public void TestShowRewards()
    {
        GenerateRandomRewards();
        StartCoroutine(ShowRewards());
    }
}

// ========================================
// HELPER CLASSES
// ========================================

/// <summary>
/// Reward type enum
/// </summary>
public enum RewardType
{
    Coin,
    Energy
}

/// <summary>
/// Reward data structure
/// </summary>
[System.Serializable]
public class RewardData
{
    public RewardType type;
    public int amount;
}