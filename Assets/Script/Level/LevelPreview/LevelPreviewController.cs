using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ✅✅✅ CRITICAL FIX: Static instance yang SELALU aktif
/// - Panel bisa inactive, tapi script HARUS active
/// - Taruh script di GameObject TERPISAH yang SELALU active
/// - Panel cukup dijadikan child/reference
/// </summary>
public class LevelPreviewController : MonoBehaviour
{
    // ✅ CRITICAL: Static instance untuk akses dari mana saja
    public static LevelPreviewController Instance { get; private set; }

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
        // ✅ CRITICAL: Setup singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Log("✓ LevelPreviewController instance created");
    }

    void Start()
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

        // ✅ CRITICAL: Hide panel initially
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(false);
            Log("✓ Panel hidden initially");
        }

        if (starImages == null || starImages.Length == 0)
        {
            starImages = new Image[3];
            if (stars != null)
            {
                for (int i = 0; i < stars.Length && i < 3; i++)
                {
                    if (stars[i] != null)
                        starImages[i] = stars[i].GetComponent<Image>();
                }
            }
        }

        Log("✓ LevelPreviewController initialized");
    }

    // Stars cache
    private Image[] starImages;

    /// <summary>
    /// ✅ Main method: Show preview
    /// </summary>
    public void ShowLevelPreview(LevelConfig levelConfig)
    {
        if (levelConfig == null)
        {
            LogError("LevelConfig is null!");
            return;
        }

        currentLevel = levelConfig;

        Log($"=== SHOWING PREVIEW: {levelConfig.displayName} ===");

        // ✅ CRITICAL: Show panel
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(true);
            Log("✓ Panel shown");
        }
        else
        {
            LogError("levelPreviewPanel is NULL!");
        }

        // Update UI
        UpdateLevelNumberText();
        UpdateLinoCharacterSprite();
        UpdateStarsDisplay();
        UpdateMissionFragments();
        GenerateAndDisplayRewards();

        Log($"=== PREVIEW COMPLETE: {levelConfig.displayName} ===");
    }

    void UpdateLevelNumberText()
    {
        if (currentLevel == null) return;

        if (lvlText != null)
        {
            lvlText.text = $"Lv. {currentLevel.number}";
            Log($"✓ Level text: Lv. {currentLevel.number}");
        }

        if (levelTitleText != null)
        {
            levelTitleText.text = currentLevel.displayName;
        }
    }

    void UpdateLinoCharacterSprite()
    {
        if (currentLevel == null || linoCharacterImage == null) return;

        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(currentLevel.id);
        }

        if (earnedStars > 0)
        {
            if (linoHappy != null)
            {
                linoCharacterImage.sprite = linoHappy;
                Log($"✓ Lino: Happy ({earnedStars} stars)");
            }
        }
        else
        {
            if (linoSad != null)
            {
                linoCharacterImage.sprite = linoSad;
                Log("✓ Lino: Sad (0 stars)");
            }
        }
    }

    void UpdateStarsDisplay()
    {
        if (currentLevel == null || stars == null) return;

        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(currentLevel.id);
        }

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;

            Image starImage = starImages != null && i < starImages.Length ? starImages[i] : stars[i].GetComponent<Image>();
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

        Log($"✓ Stars: {earnedStars}/3");
    }

    void UpdateMissionFragments()
    {
        if (currentLevel == null || fragmentListContent == null) return;

        ClearSpawnedItems(spawnedFragmentItems);

        if (currentLevel.requirements == null || currentLevel.requirements.Count == 0)
        {
            LogWarning("No requirements!");
            return;
        }

        int reqCount = currentLevel.requirements.Count;
        Log($"Spawning {reqCount} fragments...");

        foreach (var requirement in currentLevel.requirements)
        {
            if (fragmentItemPrefab == null)
            {
                LogError("fragmentItemPrefab is NULL!");
                break;
            }

            GameObject item = Instantiate(fragmentItemPrefab, fragmentListContent);
            SetupFragmentItem(item, requirement);
            spawnedFragmentItems.Add(item);
        }

        // ✅ Enable scroll if > 3 fragments
        if (fragmentScrollRect != null)
        {
            bool needScroll = reqCount > 3;
            fragmentScrollRect.enabled = needScroll;

            if (needScroll)
            {
                Canvas.ForceUpdateCanvases();
                fragmentScrollRect.horizontalNormalizedPosition = 0f;
                Log("✓ ScrollRect enabled");
            }
        }

        Log($"✓ Fragments: {reqCount} items");
    }

    void SetupFragmentItem(GameObject item, FragmentRequirement requirement)
    {
        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = item.GetComponentInChildren<Image>();
        }

        TMP_Text countText = item.transform.Find("CountText")?.GetComponent<TMP_Text>();
        if (countText == null)
        {
            countText = item.GetComponentInChildren<TMP_Text>();
        }

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

        LogWarning($"No icon for {type} variant {variant}");
        return null;
    }

    void GenerateAndDisplayRewards()
    {
        if (currentLevel == null) return;

        ClearSpawnedItems(spawnedRewardItems);
        generatedRewards.Clear();

        var tier = GetRewardTierForLevel(currentLevel.number);
        if (tier == null)
        {
            LogWarning($"No reward tier for level {currentLevel.number}");
            return;
        }

        int rewardCount = Random.Range(tier.minRewards, tier.maxRewards + 1);

        for (int i = 0; i < rewardCount; i++)
        {
            RewardData reward = GenerateRandomReward(tier);
            if (reward != null)
            {
                generatedRewards.Add(reward);
            }
        }

        DisplayRewards();

        Log($"✓ Generated {generatedRewards.Count} rewards");
    }

    RewardData GenerateRandomReward(LevelRewardTier tier)
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        cumulative += tier.boosterChance;
        if (roll < cumulative)
        {
            return GenerateRandomBooster(tier);
        }

        cumulative += tier.coinChance;
        if (roll < cumulative)
        {
            return GenerateCoinReward(tier);
        }

        cumulative += tier.energyChance;
        if (roll < cumulative)
        {
            return GenerateEnergyReward(tier);
        }

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
            LogWarning("No reward tiers!");
            return null;
        }

        foreach (var tier in rewardTiers)
        {
            if (levelNumber >= tier.startLevel && levelNumber <= tier.endLevel)
            {
                return tier;
            }
        }

        return rewardTiers[rewardTiers.Length - 1];
    }

    void DisplayRewards()
    {
        if (rewardItemsContainer == null || rewardItemPrefab == null) return;

        foreach (var reward in generatedRewards)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);
            SetupRewardItem(rewardItem, reward);
            spawnedRewardItems.Add(rewardItem);
        }

        if (rewardDescriptionText != null)
        {
            rewardDescriptionText.text = $"Complete this level to get {generatedRewards.Count} reward{(generatedRewards.Count > 1 ? "s" : "")}!";
        }

        Log($"✓ Displayed {generatedRewards.Count} rewards");
    }

    void SetupRewardItem(GameObject item, RewardData reward)
    {
        // ✅ CRITICAL FIX: Cari Icon yang BENAR (child, bukan parent)
        // Hierarchy: RewardItem (border) > Icon (yang harus diganti sprite)
        
        Transform iconTransform = item.transform.Find("Icon");
        Image iconImage = null;
        
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
            Log($"✓ Found Icon child: {iconTransform.name}");
        }
        else
        {
            // Fallback: cari Image di children (tapi SKIP yang parent/border)
            Image[] images = item.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                // Skip parent border (cek jika GameObject name adalah "Icon" atau bukan parent)
                if (img.gameObject != item && img.gameObject.name.Contains("Icon"))
                {
                    iconImage = img;
                    Log($"✓ Found Icon via search: {img.gameObject.name}");
                    break;
                }
            }
        }

        // Find amount text
        TMP_Text amountText = null;
        Transform amountTransform = item.transform.Find("Amount");
        
        if (amountTransform != null)
        {
            amountText = amountTransform.GetComponent<TMP_Text>();
        }
        
        if (amountText == null)
        {
            // Fallback: find any TMP_Text in children
            amountText = item.GetComponentInChildren<TMP_Text>();
        }

        // ✅ Update icon (ONLY the Icon child, NOT the parent border)
        if (iconImage != null && reward.icon != null)
        {
            iconImage.sprite = reward.icon;
            Log($"✓ Set icon sprite: {reward.icon.name}");
        }
        else
        {
            LogWarning($"Failed to set icon! iconImage={iconImage != null}, reward.icon={reward.icon != null}");
        }

        // Update amount text
        if (amountText != null)
        {
            amountText.text = $"x{reward.amount}";
            Log($"✓ Set amount text: x{reward.amount}");
        }
        else
        {
            LogWarning("Amount text not found!");
        }
    }

    public void OnPlayButtonClicked()
{
    if (currentLevel == null)
    {
        LogError("No level selected!");
        return;
    }

    // ✅ DOUBLE-CHECK ENERGY BEFORE LOADING SCENE
    if (PlayerEconomy.Instance != null)
    {
        int requiredEnergy = 10; // Energy cost per level
        int currentEnergy = PlayerEconomy.Instance.Energy;

        if (currentEnergy < requiredEnergy)
        {
            LogWarning($"Not enough energy! Need {requiredEnergy}, have {currentEnergy}");

            // Show popup
            if (PopUpAlert.Instance != null)
            {
                PopUpAlert.Instance.ShowNotEnoughEnergy();
            }

            // Play fail sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPurchaseFail();
            }

            return; // STOP
        }

        // ✅ CONSUME ENERGY
        bool consumed = PlayerEconomy.Instance.ConsumeEnergy(requiredEnergy);

        if (!consumed)
        {
            LogError("Failed to consume energy!");
            return;
        }

        Log($"✓ Energy consumed: {requiredEnergy} (Remaining: {PlayerEconomy.Instance.Energy})");
    }
    else
    {
        LogWarning("PlayerEconomy not found! Playing without energy check.");
    }

    // Save level data
    PlayerPrefs.SetString("SelectedLevelId", currentLevel.id);
    PlayerPrefs.SetInt("SelectedLevelNumber", currentLevel.number);

    SaveGeneratedRewards();

    PlayerPrefs.Save();

    // Play click sound
    if (SoundManager.Instance != null)
    {
        SoundManager.Instance.PlayButtonClick();
    }

    Log($"Starting level {currentLevel.number}...");

    SceneManager.LoadScene(gameplaySceneName);
}

    void SaveGeneratedRewards()
    {
        if (generatedRewards == null || generatedRewards.Count == 0) return;

        RewardDataList rewardList = new RewardDataList { rewards = generatedRewards };
        string json = JsonUtility.ToJson(rewardList);

        PlayerPrefs.SetString($"LevelRewards_{currentLevel.id}", json);

        Log($"✓ Saved {generatedRewards.Count} rewards");
    }

    void OnCloseButtonClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(false);
            Log("✓ Panel closed");
        }

        currentLevel = null;
        ClearSpawnedItems(spawnedFragmentItems);
        ClearSpawnedItems(spawnedRewardItems);
        generatedRewards.Clear();
    }

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
    /// ✅ Static helper
    /// </summary>
    public static void ShowPreview(LevelConfig levelConfig)
    {
        if (Instance != null)
        {
            Instance.ShowLevelPreview(levelConfig);
        }
        else
        {
            Debug.LogError("[LevelPreview] ❌ Instance is NULL!");
        }
    }
}

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