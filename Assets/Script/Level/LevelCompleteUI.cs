using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UPDATED: Level Complete UI dengan reward 1x per level & improved reward system
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject levelCompletePanel;
    public Button continueButton;
    public Button homeButton;

    [Header("🎭 Character Lino")]
    public GameObject linoHappy;
    public GameObject linoSad;

    [Header("Stars Display")]
    public GameObject[] starObjects;
    public Image[] starImages;
    public Sprite starFilled;
    public Sprite starEmpty;

    [Header("🎉 LEVEL COMPLETE Popup")]
    public GameObject lvlPopup;
    public RectTransform lvlPopupBackground;
    public TMP_Text lvlPopupText;
    public float lvlPopupExpandDuration = 0.4f;
    public float lvlPopupDisplayDuration = 0.8f;

    [Header("🎁 Random Rewards")]
    public GameObject rewardsContainer;
    public GameObject rewardItemPrefab;
    public Sprite coinIcon;
    public Sprite energyIcon;

    [Header("✨ NEW: Already Completed Text")]
    [Tooltip("Text yang akan muncul jika level sudah pernah diselesaikan")]
    public TMP_Text alreadyCompletedText;
    [Tooltip("Text yang akan ditampilkan (default: 'Level Already Completed')")]
    public string alreadyCompletedMessage = "Level Already Completed";

    [Header("⚙️ NEW: Improved Reward Settings")]
    [Tooltip("Chance untuk mendapat reward (1 reward, biasanya Coin)")]
    [Range(0, 100)]
    public float singleRewardChance = 80f;

    [Tooltip("Chance untuk mendapat 2 rewards (Coin + Energy) - RARE")]
    [Range(0, 100)]
    public float doubleRewardChance = 20f;

    [Tooltip("Range coin reward")]
    public Vector2Int coinRewardRange = new Vector2Int(500, 3000);

    [Tooltip("Range energy reward (dikecilkan)")]
    public Vector2Int energyRewardRange = new Vector2Int(5, 20);

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeOutDuration = 1f;
    public float fadeInDuration = 0.5f;

    [Header("Animation Settings")]
    public float starAnimationDelay = 0.3f;
    public float starScalePunch = 1.3f;
    public float starAnimationDuration = 0.3f;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "Gameplay";

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Save key untuk track level yang sudah dapat reward
    private const string PREF_REWARDED_LEVELS = "Kulino_RewardedLevels_v1";

    private bool isTransitioning = false;
    private int earnedStars = 0;
    private int currentLevelNumber = 0;
    private string currentLevelId = "";
    private List<RewardData> generatedRewards = new List<RewardData>();
    private bool isFirstCompletion = false; // ✅ Track first completion

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
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (lvlPopup != null)
            lvlPopup.SetActive(false);

        if (rewardsContainer != null)
            rewardsContainer.SetActive(false);

        // ✅ Hide already completed text initially
        if (alreadyCompletedText != null)
            alreadyCompletedText.gameObject.SetActive(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.interactable = false;
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(OnHomeClicked);
            homeButton.interactable = false;
        }

        if (starImages == null || starImages.Length == 0)
        {
            starImages = new Image[3];
            if (starObjects != null)
            {
                for (int i = 0; i < starObjects.Length && i < 3; i++)
                {
                    if (starObjects[i] != null)
                        starImages[i] = starObjects[i].GetComponent<Image>();
                }
            }
        }

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

    public void OnLevelComplete()
    {
        if (isTransitioning) return;

        Log("🎉 Level Complete triggered!");

        StopAllSpawners();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLevelComplete();
            Log("✓ Playing level complete sound");
        }

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

        currentLevelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);
        Log($"Current level: {currentLevelId} (#{currentLevelNumber})");

        // ✅ Check if this is first completion
        isFirstCompletion = !HasReceivedReward(currentLevelId);

        // ✅ Generate rewards ONLY if first completion
        if (isFirstCompletion)
        {
            GenerateImprovedRewards();
            Log("✓ First completion - rewards generated");
        }
        else
        {
            generatedRewards.Clear();
            Log("⚠️ Level already completed - no rewards");
        }

        StartCoroutine(ShowLevelCompleteUI());
    }

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
    /// ✅ IMPROVED: Better reward generation system
    /// - Higher chance untuk single reward (Coin only)
    /// - Lower chance untuk double reward (Coin + Energy)
    /// - Energy amount dikecilkan
    /// </summary>
    void GenerateImprovedRewards()
    {
        generatedRewards.Clear();

        float roll = Random.Range(0f, 100f);

        // ✅ 80% chance: Single reward (Coin only)
        if (roll < singleRewardChance)
        {
            int coinAmount = Random.Range(coinRewardRange.x, coinRewardRange.y + 1);
            generatedRewards.Add(new RewardData
            {
                type = RewardType.Coin,
                amount = coinAmount
            });
            Log($"✓ Generated single reward: Coin x{coinAmount}");
        }
        // ✅ 20% chance: Double reward (Coin + Energy)
        else if (roll < singleRewardChance + doubleRewardChance)
        {
            // Coin
            int coinAmount = Random.Range(coinRewardRange.x, coinRewardRange.y + 1);
            generatedRewards.Add(new RewardData
            {
                type = RewardType.Coin,
                amount = coinAmount
            });

            // Energy (smaller amount)
            int energyAmount = Random.Range(energyRewardRange.x, energyRewardRange.y + 1);
            generatedRewards.Add(new RewardData
            {
                type = RewardType.Energy,
                amount = energyAmount
            });

            Log($"✓ Generated double reward: Coin x{coinAmount} + Energy x{energyAmount}");
        }
        else
        {
            // Fallback: single coin (shouldn't happen with 80+20=100%)
            int coinAmount = Random.Range(coinRewardRange.x, coinRewardRange.y + 1);
            generatedRewards.Add(new RewardData
            {
                type = RewardType.Coin,
                amount = coinAmount
            });
            Log($"✓ Generated fallback reward: Coin x{coinAmount}");
        }
    }

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

        // ✅ Mark level as rewarded (first completion)
        if (isFirstCompletion)
        {
            MarkLevelRewarded(currentLevelId);
            Log($"✓ Marked {currentLevelId} as rewarded");
        }
    }

    IEnumerator ShowLevelCompleteUI()
    {
        yield return new WaitForSeconds(0.5f);

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

        ResetStars();

        yield return StartCoroutine(AnimateLevelCompletePopup());

        SetLinoCharacter();

        yield return StartCoroutine(AnimateStars());

        // ✅ Show rewards OR "already completed" message
        if (isFirstCompletion && generatedRewards.Count > 0)
        {
            yield return StartCoroutine(ShowRewards());
        }
        else
        {
            ShowAlreadyCompletedMessage();
        }

        EnableButtons();

        Log("✓ Level complete UI fully shown");
    }

    /// <summary>
    /// ✅ NEW: Show "Level Already Completed" message
    /// </summary>
    void ShowAlreadyCompletedMessage()
    {
        if (alreadyCompletedText != null)
        {
            alreadyCompletedText.text = alreadyCompletedMessage;
            alreadyCompletedText.gameObject.SetActive(true);
            Log("✓ Showing already completed message");
        }
        else
        {
            LogWarning("alreadyCompletedText not assigned!");
        }

        // Hide rewards container
        if (rewardsContainer != null)
        {
            rewardsContainer.SetActive(false);
        }
    }

    IEnumerator AnimateLevelCompletePopup()
    {
        if (lvlPopup == null)
        {
            LogWarning("lvlPopup not assigned! Skipping popup animation.");
            yield break;
        }

        Log("Animating LEVEL COMPLETE popup...");

        lvlPopup.SetActive(true);

        if (lvlPopupBackground != null)
        {
            Vector2 originalSize = lvlPopupBackground.sizeDelta;
            lvlPopupBackground.sizeDelta = new Vector2(0, originalSize.y);

            float elapsed = 0f;
            while (elapsed < lvlPopupExpandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lvlPopupExpandDuration;
                float easedT = EaseOutBack(t);

                lvlPopupBackground.sizeDelta = new Vector2(
                    Mathf.Lerp(0, originalSize.x, easedT),
                    originalSize.y
                );

                yield return null;
            }

            lvlPopupBackground.sizeDelta = originalSize;
        }

        yield return new WaitForSeconds(lvlPopupDisplayDuration);

        lvlPopup.SetActive(false);

        Log("✓ LEVEL COMPLETE popup animation complete");
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    void SetLinoCharacter()
    {
        if (linoHappy == null || linoSad == null)
        {
            LogWarning("Lino character GameObjects not assigned!");
            return;
        }

        if (earnedStars == 0)
        {
            linoHappy.SetActive(false);
            linoSad.SetActive(true);
            Log("✓ Showing Sad Lino (0 stars)");
        }
        else
        {
            linoHappy.SetActive(true);
            linoSad.SetActive(false);
            Log($"✓ Showing Happy Lino ({earnedStars} stars)");
        }
    }

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

            yield return new WaitForSeconds(starAnimationDelay);

            starImages[i].sprite = starFilled;

            StartCoroutine(PunchScale(starImages[i].transform));

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStarPickup();
            }

            Log($"✓ Star {i + 1} animated");
        }

        Log($"✓ All {earnedStars} stars animated");
    }

    IEnumerator PunchScale(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float elapsed = 0f;
        float halfDuration = starAnimationDuration / 2f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale, originalScale * starScalePunch, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale * starScalePunch, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

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

        foreach (Transform child in rewardsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        rewardsContainer.SetActive(true);

        // ✅ Hide already completed text
        if (alreadyCompletedText != null)
        {
            alreadyCompletedText.gameObject.SetActive(false);
        }

        ApplyRewards();

        yield return new WaitForSeconds(0.3f);

        foreach (var reward in generatedRewards)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardsContainer.transform);

            SetupRewardItem(rewardItem, reward);

            StartCoroutine(PunchScale(rewardItem.transform));

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCoinPickup();
            }

            yield return new WaitForSeconds(0.2f);
        }

        Log("✓ All rewards shown");
    }

    void SetupRewardItem(GameObject item, RewardData reward)
    {
        Transform iconTransform = item.transform.Find("IconItemsRW");

        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();

            if (iconImage != null)
            {
                switch (reward.type)
                {
                    case RewardType.Coin:
                        if (coinIcon != null)
                        {
                            iconImage.sprite = coinIcon;
                            Log($"✓ Set coin icon on {iconTransform.name}");
                        }
                        break;
                    case RewardType.Energy:
                        if (energyIcon != null)
                        {
                            iconImage.sprite = energyIcon;
                            Log($"✓ Set energy icon on {iconTransform.name}");
                        }
                        break;
                }
            }
            else
            {
                LogWarning($"IconItemsRW found but missing Image component!");
            }
        }
        else
        {
            LogWarning($"IconItemsRW not found in {item.name}!");
        }

        TMP_Text amountText = item.GetComponentInChildren<TMP_Text>();
        if (amountText != null)
        {
            amountText.text = $"+{reward.amount}";
        }
        else
        {
            LogWarning($"TMP_Text not found in {item.name} for amount display!");
        }

        Log($"✓ Setup reward item: {reward.type} x{reward.amount}");
    }

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

    IEnumerator TransitionToMainMenu()
    {
        isTransitioning = true;

        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

        Log("Loading main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }

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

    // ========================================
    // ✅ NEW: Reward Tracking System
    // ========================================

    bool HasReceivedReward(string levelId)
    {
        string rewarded = PlayerPrefs.GetString(PREF_REWARDED_LEVELS, "");
        if (string.IsNullOrEmpty(rewarded)) return false;

        string[] levels = rewarded.Split(',');
        foreach (string lvl in levels)
        {
            if (lvl.Trim() == levelId)
            {
                return true;
            }
        }

        return false;
    }

    void MarkLevelRewarded(string levelId)
    {
        string rewarded = PlayerPrefs.GetString(PREF_REWARDED_LEVELS, "");

        if (string.IsNullOrEmpty(rewarded))
        {
            rewarded = levelId;
        }
        else
        {
            // Check if already in list
            if (!HasReceivedReward(levelId))
            {
                rewarded += "," + levelId;
            }
        }

        PlayerPrefs.SetString(PREF_REWARDED_LEVELS, rewarded);
        PlayerPrefs.Save();

        Log($"✓ Marked {levelId} as rewarded");
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

    [ContextMenu("Test: Level Complete (First Time)")]
    public void TestLevelCompleteFirstTime()
    {
        earnedStars = 3;
        currentLevelNumber = 1;
        currentLevelId = "level_1";

        // Clear reward history for testing
        PlayerPrefs.DeleteKey(PREF_REWARDED_LEVELS);

        isFirstCompletion = true;
        GenerateImprovedRewards();
        StartCoroutine(ShowLevelCompleteUI());
    }

    [ContextMenu("Test: Level Complete (Replay)")]
    public void TestLevelCompleteReplay()
    {
        earnedStars = 2;
        currentLevelNumber = 1;
        currentLevelId = "level_1";

        // Mark as already rewarded
        MarkLevelRewarded(currentLevelId);

        isFirstCompletion = false;
        generatedRewards.Clear();
        StartCoroutine(ShowLevelCompleteUI());
    }

    [ContextMenu("Debug: Clear Reward History")]
    public void ClearRewardHistory()
    {
        PlayerPrefs.DeleteKey(PREF_REWARDED_LEVELS);
        PlayerPrefs.Save();
        Debug.Log("[LevelCompleteUI] Reward history cleared");
    }

    [ContextMenu("Debug: Print Rewarded Levels")]
    public void PrintRewardedLevels()
    {
        string rewarded = PlayerPrefs.GetString(PREF_REWARDED_LEVELS, "");
        Debug.Log($"[LevelCompleteUI] Rewarded levels: {(string.IsNullOrEmpty(rewarded) ? "NONE" : rewarded)}");
    }
}

// ========================================
// HELPER CLASSES
// ========================================

public enum RewardType
{
    Coin,
    Energy
}

[System.Serializable]
public class RewardData
{
    public RewardType type;
    public int amount;
}