using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ✅ FIXED: LevelPreview dengan fitur lengkap:
/// - Level number text update
/// - Lino character sprite (happy/sad based on stars)
/// - Random rewards per level
/// - Fragment mission display (1-6 items dengan scroll)
/// - Close button
/// </summary>
public class LevelPreviewController : MonoBehaviour
{
    [Header("Panel Reference")]
    public GameObject levelPreviewPanel;

    [Header("Level Info")]
    public TMP_Text lvlText; // "Lvl. X"
    public TMP_Text levelTitleText; // Optional

    [Header("Character Lino")]
    public Image linoCharacterImage;
    public Sprite linoHappy;
    public Sprite linoSad;

    [Header("Stars Display")]
    public GameObject[] stars; // 3 star GameObjects
    public Sprite starFilled;
    public Sprite starEmpty;

    [Header("Mission Fragments")]
    public Transform fragmentListContent; // Content di ScrollRect
    public GameObject fragmentItemPrefab;
    public ScrollRect fragmentScrollRect;

    [Header("Rewards Display")]
    public Transform rewardItemsContainer;
    public GameObject rewardItemPrefab;
    public TMP_Text rewardDescriptionText;

    [Header("Buttons")]
    public Button playButton;
    public Button closeButton;

    [Header("Settings")]
    public string gameplaySceneName = "Gameplay";

    [Header("Reward Settings - Level Tiers")]
    public LevelRewardTier[] rewardTiers;

    [Header("Icon References")]
    public Sprite coinIcon;
    public Sprite energyIcon;
    public Sprite speedBoostIcon;
    public Sprite timeFreezeIcon;
    public Sprite shieldIcon;
    public Sprite coin2xIcon;
    public Sprite magnetIcon;

    [Header("Registry")]
    public FragmentPrefabRegistry fragmentRegistry;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Runtime
    private LevelConfig currentLevel;
    private List<RewardData> generatedRewards = new List<RewardData>();
    private List<GameObject> spawnedFragmentItems = new List<GameObject>();
    private List<GameObject> spawnedRewardItems = new List<GameObject>();

    void Awake()
    {
        // Setup buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // Hide panel initially
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ Main method: Show preview untuk level yang dipilih
    /// </summary>
    public void ShowLevelPreview(LevelConfig levelConfig)
    {
        if (levelConfig == null)
        {
            LogError("LevelConfig is null!");
            return;
        }

        currentLevel = levelConfig;

        // Show panel
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(true);
        }

        // Update semua UI
        UpdateLevelNumberText();
        UpdateLinoCharacterSprite();
        UpdateStarsDisplay();
        UpdateMissionFragments();
        GenerateAndDisplayRewards();

        Log($"✓ Showing preview for {levelConfig.displayName} (Level {levelConfig.number})");
    }

    // ========================================
    // UPDATE UI ELEMENTS
    // ========================================

    /// <summary>
    /// ✅ Update "Lvl. X" text
    /// </summary>
    void UpdateLevelNumberText()
    {
        if (currentLevel == null) return;

        if (lvlText != null)
        {
            lvlText.text = $"Lvl. {currentLevel.number}";
            Log($"Updated level text: Lvl. {currentLevel.number}");
        }

        if (levelTitleText != null)
        {
            levelTitleText.text = currentLevel.displayName;
        }
    }

    /// <summary>
    /// ✅ Update Lino character sprite (happy/sad based on earned stars)
    /// </summary>
    void UpdateLinoCharacterSprite()
    {
        if (currentLevel == null || linoCharacterImage == null) return;

        // Get earned stars untuk level ini
        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(currentLevel.id);
        }

        // 0 stars = Sad, 1-3 stars = Happy
        if (earnedStars > 0)
        {
            if (linoHappy != null)
            {
                linoCharacterImage.sprite = linoHappy;
                Log($"Lino: Happy ({earnedStars} stars earned)");
            }
        }
        else
        {
            if (linoSad != null)
            {
                linoCharacterImage.sprite = linoSad;
                Log("Lino: Sad (0 stars)");
            }
        }
    }

    /// <summary>
    /// ✅ Update stars display
    /// </summary>
    void UpdateStarsDisplay()
    {
        if (currentLevel == null || stars == null) return;

        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(currentLevel.id);
        }

        // Update each star
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;

            Image starImage = stars[i].GetComponent<Image>();
            if (starImage == null) continue;

            if (i < earnedStars)
            {
                starImage.sprite = starFilled;
            }
            else
            {
                starImage.sprite = starEmpty;
            }
        }

        Log($"Stars updated: {earnedStars}/3");
    }

    /// <summary>
    /// ✅ Update mission fragments (1-6 items dengan ScrollRect)
    /// </summary>
    void UpdateMissionFragments()
    {
        if (currentLevel == null || fragmentListContent == null) return;

        // Clear existing
        ClearSpawnedItems(spawnedFragmentItems);

        if (currentLevel.requirements == null || currentLevel.requirements.Count == 0)
        {
            LogWarning("No requirements for this level!");
            return;
        }

        int reqCount = currentLevel.requirements.Count;
        Log($"Spawning {reqCount} fragment items...");

        // Spawn fragment items
        foreach (var requirement in currentLevel.requirements)
        {
            if (fragmentItemPrefab == null)
            {
                LogError("fragmentItemPrefab not assigned!");
                break;
            }

            GameObject fragmentItem = Instantiate(fragmentItemPrefab, fragmentListContent);
            SetupFragmentItem(fragmentItem, requirement);
            spawnedFragmentItems.Add(fragmentItem);
        }

        // ✅ Enable ScrollRect jika fragments > 3
        if (fragmentScrollRect != null)
        {
            bool needScroll = reqCount > 3;
            fragmentScrollRect.enabled = needScroll;

            if (needScroll)
            {
                // Reset scroll position
                Canvas.ForceUpdateCanvases();
                fragmentScrollRect.horizontalNormalizedPosition = 0f;
                Log("ScrollRect enabled (more than 3 fragments)");
            }
            else
            {
                Log("ScrollRect disabled (3 or fewer fragments)");
            }
        }

        Log($"✓ Mission fragments updated: {reqCount} items");
    }

    /// <summary>
    /// Setup individual fragment item UI
    /// </summary>
    void SetupFragmentItem(GameObject item, FragmentRequirement requirement)
    {
        // Find icon image
        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = item.GetComponentInChildren<Image>();
        }

        // Find count text
        TMP_Text countText = item.transform.Find("CountText")?.GetComponent<TMP_Text>();
        if (countText == null)
        {
            countText = item.GetComponentInChildren<TMP_Text>();
        }

        // Get fragment icon
        Sprite fragmentIcon = GetFragmentIcon(requirement.type, requirement.colorVariant);

        if (iconImage != null && fragmentIcon != null)
        {
            iconImage.sprite = fragmentIcon;
        }

        if (countText != null)
        {
            countText.text = $"x{requirement.count}";
        }
    }

    /// <summary>
    /// Get fragment icon dari registry
    /// </summary>
    Sprite GetFragmentIcon(FragmentType type, int variant)
    {
        if (fragmentRegistry != null)
        {
            GameObject prefab = fragmentRegistry.GetPrefab(type, variant);
            if (prefab != null)
            {
                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    return sr.sprite;
                }
            }
        }

        LogWarning($"No icon found for {type} variant {variant}");
        return null;
    }

    // ========================================
    // REWARD GENERATION SYSTEM
    // ========================================

    /// <summary>
    /// ✅ Generate random rewards berdasarkan level tier
    /// </summary>
    void GenerateAndDisplayRewards()
    {
        if (currentLevel == null) return;

        // Clear existing
        ClearSpawnedItems(spawnedRewardItems);
        generatedRewards.Clear();

        // Get reward tier untuk level ini
        LevelRewardTier tier = GetRewardTierForLevel(currentLevel.number);

        if (tier == null)
        {
            LogWarning($"No reward tier found for level {currentLevel.number}");
            return;
        }

        // Generate random rewards (1-3 items)
        int rewardCount = Random.Range(tier.minRewards, tier.maxRewards + 1);

        for (int i = 0; i < rewardCount; i++)
        {
            RewardData reward = GenerateRandomReward(tier);
            if (reward != null)
            {
                generatedRewards.Add(reward);
            }
        }

        // Display rewards
        DisplayRewards();

        Log($"✓ Generated {generatedRewards.Count} rewards for level {currentLevel.number}");
    }

    /// <summary>
    /// Generate single random reward berdasarkan tier
    /// </summary>
    RewardData GenerateRandomReward(LevelRewardTier tier)
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        // Check booster chance
        cumulative += tier.boosterChance;
        if (roll < cumulative)
        {
            return GenerateRandomBooster(tier);
        }

        // Check coin chance
        cumulative += tier.coinChance;
        if (roll < cumulative)
        {
            return GenerateCoinReward(tier);
        }

        // Check energy chance
        cumulative += tier.energyChance;
        if (roll < cumulative)
        {
            return GenerateEnergyReward(tier);
        }

        // Fallback: coin
        return GenerateCoinReward(tier);
    }

    RewardData GenerateRandomBooster(LevelRewardTier tier)
    {
        string[] boosterTypes = { "speedboost", "timefreeze", "shield", "coin2x", "magnet" };
        string randomBooster = boosterTypes[Random.Range(0, boosterTypes.Length)];

        int amount = Random.Range(tier.minBoosterAmount, tier.maxBoosterAmount + 1);

        return new RewardData
        {
            type = RewardType.Booster,
            boosterId = randomBooster,
            amount = amount,
            icon = GetBoosterIcon(randomBooster),
            displayName = GetBoosterDisplayName(randomBooster)
        };
    }

    RewardData GenerateCoinReward(LevelRewardTier tier)
    {
        int amount = Random.Range(tier.minCoinAmount, tier.maxCoinAmount + 1);

        return new RewardData
        {
            type = RewardType.Coin,
            amount = amount,
            icon = coinIcon,
            displayName = "Coins"
        };
    }

    RewardData GenerateEnergyReward(LevelRewardTier tier)
    {
        int amount = Random.Range(tier.minEnergyAmount, tier.maxEnergyAmount + 1);

        return new RewardData
        {
            type = RewardType.Energy,
            amount = amount,
            icon = energyIcon,
            displayName = "Energy"
        };
    }

    Sprite GetBoosterIcon(string boosterId)
    {
        switch (boosterId.ToLower())
        {
            case "speedboost": return speedBoostIcon;
            case "timefreeze": return timeFreezeIcon;
            case "shield": return shieldIcon;
            case "coin2x": return coin2xIcon;
            case "magnet": return magnetIcon;
            default: return null;
        }
    }

    string GetBoosterDisplayName(string boosterId)
    {
        switch (boosterId.ToLower())
        {
            case "speedboost": return "Speed Boost";
            case "timefreeze": return "Time Freeze";
            case "shield": return "Shield";
            case "coin2x": return "2x Coin";
            case "magnet": return "Magnet";
            default: return boosterId;
        }
    }

    LevelRewardTier GetRewardTierForLevel(int levelNumber)
    {
        if (rewardTiers == null || rewardTiers.Length == 0)
        {
            LogWarning("No reward tiers defined!");
            return null;
        }

        foreach (var tier in rewardTiers)
        {
            if (levelNumber >= tier.startLevel && levelNumber <= tier.endLevel)
            {
                return tier;
            }
        }

        // Fallback: return last tier
        return rewardTiers[rewardTiers.Length - 1];
    }

    /// <summary>
    /// Display rewards di UI
    /// </summary>
    void DisplayRewards()
    {
        if (rewardItemsContainer == null || rewardItemPrefab == null) return;

        foreach (var reward in generatedRewards)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);
            SetupRewardItem(rewardItem, reward);
            spawnedRewardItems.Add(rewardItem);
        }

        // Update description
        if (rewardDescriptionText != null)
        {
            rewardDescriptionText.text = $"Complete this level to get {generatedRewards.Count} reward{(generatedRewards.Count > 1 ? "s" : "")}!";
        }

        Log($"✓ Displayed {generatedRewards.Count} rewards");
    }

    void SetupRewardItem(GameObject item, RewardData reward)
    {
        // Find icon
        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = item.GetComponentInChildren<Image>();
        }

        // Find amount text
        TMP_Text amountText = item.transform.Find("Amount")?.GetComponent<TMP_Text>();
        if (amountText == null)
        {
            amountText = item.GetComponentInChildren<TMP_Text>();
        }

        if (iconImage != null && reward.icon != null)
        {
            iconImage.sprite = reward.icon;
        }

        if (amountText != null)
        {
            amountText.text = $"x{reward.amount}";
        }
    }

    // ========================================
    // BUTTON HANDLERS
    // ========================================

    /// <summary>
    /// ✅ Play button: Start level dengan rewards saved
    /// </summary>
    void OnPlayButtonClicked()
    {
        if (currentLevel == null)
        {
            LogError("No level selected!");
            return;
        }

        // Save level info
        PlayerPrefs.SetString("SelectedLevelId", currentLevel.id);
        PlayerPrefs.SetInt("SelectedLevelNumber", currentLevel.number);

        // ✅ Save generated rewards untuk diklaim setelah level complete
        SaveGeneratedRewards();

        PlayerPrefs.Save();

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        Log($"Starting level {currentLevel.number}...");

        // Load gameplay scene
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Save rewards ke PlayerPrefs
    /// </summary>
    void SaveGeneratedRewards()
    {
        if (generatedRewards == null || generatedRewards.Count == 0) return;

        RewardDataList rewardList = new RewardDataList { rewards = generatedRewards };
        string json = JsonUtility.ToJson(rewardList);

        PlayerPrefs.SetString($"LevelRewards_{currentLevel.id}", json);

        Log($"✓ Saved {generatedRewards.Count} rewards for {currentLevel.id}");
    }

    /// <summary>
    /// ✅ Close button: Hide panel
    /// </summary>
    void OnCloseButtonClicked()
    {
        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // Hide panel
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(false);
        }

        // Clear data
        currentLevel = null;
        ClearSpawnedItems(spawnedFragmentItems);
        ClearSpawnedItems(spawnedRewardItems);
        generatedRewards.Clear();

        Log("✓ Panel closed");
    }

    // ========================================
    // HELPERS
    // ========================================

    void ClearSpawnedItems(List<GameObject> items)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        items.Clear();
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[LevelPreview] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[LevelPreview] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[LevelPreview] {message}");
    }

    /// <summary>
    /// ✅ Static helper: Show preview dari script lain
    /// </summary>
    public static void ShowPreview(LevelConfig levelConfig)
    {
        var instance = FindFirstObjectByType<LevelPreviewController>();
        if (instance != null)
        {
            instance.ShowLevelPreview(levelConfig);
        }
        else
        {
            Debug.LogError("[LevelPreview] LevelPreviewController not found in scene!");
        }
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Debug: Print Current Level")]
    void Context_PrintLevel()
    {
        if (currentLevel != null)
        {
            Debug.Log($"=== CURRENT LEVEL ===\nID: {currentLevel.id}\nNumber: {currentLevel.number}\nName: {currentLevel.displayName}\nRequirements: {currentLevel.requirements?.Count ?? 0}");
        }
        else
        {
            Debug.Log("No level loaded");
        }
    }

    [ContextMenu("Debug: Print Generated Rewards")]
    void Context_PrintRewards()
    {
        Debug.Log($"=== GENERATED REWARDS ({generatedRewards.Count}) ===");
        foreach (var reward in generatedRewards)
        {
            Debug.Log($"- {reward.displayName}: x{reward.amount} ({reward.type})");
        }
    }
}

// ========================================
// LEVEL REWARD TIER
// ========================================

[System.Serializable]
public class LevelRewardTier
{
    [Header("Level Range")]
    public int startLevel = 1;
    public int endLevel = 20;

    [Header("Reward Count")]
    public int minRewards = 1;
    public int maxRewards = 3;

    [Header("Reward Chances (%)")]
    [Range(0f, 100f)] public float boosterChance = 40f;
    [Range(0f, 100f)] public float coinChance = 40f;
    [Range(0f, 100f)] public float energyChance = 20f;

    [Header("Booster Amounts")]
    public int minBoosterAmount = 1;
    public int maxBoosterAmount = 3;

    [Header("Coin Amounts")]
    public int minCoinAmount = 500;
    public int maxCoinAmount = 2000;

    [Header("Energy Amounts")]
    public int minEnergyAmount = 5;
    public int maxEnergyAmount = 20;
}