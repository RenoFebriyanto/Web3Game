using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controller untuk LevelPreview panel
/// Menampilkan info level, rewards, mission, dan stars
/// </summary>
public class LevelPreviewController : MonoBehaviour
{


[Header("Object Pooling")]
public int poolSize = 10;

private Queue<GameObject> fragmentItemPool = new Queue<GameObject>();
private Queue<GameObject> rewardItemPool = new Queue<GameObject>();

    [Header("Panel Reference")]
    public GameObject levelPreviewPanel;

    [Header("Level Info")]
    public TMP_Text lvlText;
    public TMP_Text levelTitleText; // Optional: untuk display name

    [Header("Character Lino")]
    public Image linoCharacterImage;
    public Sprite linoHappy;
    public Sprite linoSad;

    [Header("Stars Display")]
    public GameObject[] stars; // Array of 3 star GameObjects
    public Sprite starFilled;
    public Sprite starEmpty;

    [Header("Mission Fragments")]
    public Transform fragmentListContent; // Content di dalam ScrollRect
    public GameObject fragmentItemPrefab; // Prefab untuk display fragment
    public ScrollRect fragmentScrollRect;

    [Header("Rewards Display")]
    public Transform rewardItemsContainer;
    public GameObject rewardItemPrefab; // Prefab untuk display reward
    public TMP_Text rewardDescriptionText; // Optional description

    [Header("Buttons")]
    public Button playButton;
    public Button closeButton;

    [Header("Settings")]
    public string gameplaySceneName = "Gameplay";

    [Header("Reward Settings")]
    [Tooltip("Reward chances dan amounts untuk setiap tier level")]
    public LevelRewardTier[] rewardTiers;

    [Header("Icon References")]
    public Sprite coinIcon;
    public Sprite energyIcon;
    public Sprite speedBoostIcon;
    public Sprite timeFreezeIcon;
    public Sprite shieldIcon;
    public Sprite coin2xIcon;
    public Sprite magnetIcon;

    private LevelConfig currentLevel;
    private List<RewardData> generatedRewards = new List<RewardData>();
    private List<GameObject> spawnedFragmentItems = new List<GameObject>();
    private List<GameObject> spawnedRewardItems = new List<GameObject>();

    void Awake()
    {
        // Setup button listeners
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

        // ✅ Ensure BoosterInventory exists
        if (BoosterInventory.Instance == null)
        {
            GameObject go = new GameObject("BoosterInventory");
            go.AddComponent<BoosterInventory>();
            DontDestroyOnLoad(go);
            Debug.Log("[LevelPreview] Created BoosterInventory instance");
        }

        // Hide panel initially
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(false);
        }
    }


    void InitializePools()
    {
        // Fragment pool
        for (int i = 0; i < poolSize; i++)
        {
            if (fragmentItemPrefab != null)
            {
                GameObject obj = Instantiate(fragmentItemPrefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                fragmentItemPool.Enqueue(obj);
            }
        }

        // Reward pool
        for (int i = 0; i < poolSize; i++)
        {
            if (rewardItemPrefab != null)
            {
                GameObject obj = Instantiate(rewardItemPrefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                rewardItemPool.Enqueue(obj);
            }
        }
    }

GameObject GetFragmentItemFromPool()
{
    if (fragmentItemPool.Count > 0)
    {
        GameObject obj = fragmentItemPool.Dequeue();
        obj.SetActive(true);
        return obj;
    }
    
    // Fallback: instantiate new
    return Instantiate(fragmentItemPrefab);
}

    void ReturnFragmentItemToPool(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        fragmentItemPool.Enqueue(obj);
    }



    /// <summary>
    /// Show level preview dengan level data
    /// Called dari LevelSelectionItem
    /// </summary>
    public void ShowLevelPreview(LevelConfig levelConfig)
    {
        if (levelConfig == null)
        {
            Debug.LogError("[LevelPreview] LevelConfig is null!");
            return;
        }

        currentLevel = levelConfig;

        // Show panel
        if (levelPreviewPanel != null)
        {
            levelPreviewPanel.SetActive(true);
        }

        // Update UI
        UpdateLevelInfo();
        UpdateCharacterSprite();
        UpdateStarsDisplay();
        UpdateMissionFragments();
        GenerateAndDisplayRewards();

        Debug.Log($"[LevelPreview] Showing preview for {levelConfig.displayName}");
    }

    /// <summary>
    /// Update level number text
    /// </summary>
    void UpdateLevelInfo()
    {
        if (currentLevel == null) return;

        // Update "Lvl. X" text
        if (lvlText != null)
        {
            lvlText.text = $"Lvl. {currentLevel.number}";
        }

        // Update level title (optional)
        if (levelTitleText != null)
        {
            levelTitleText.text = currentLevel.displayName;
        }
    }

    /// <summary>
    /// Update character sprite based on stars earned
    /// </summary>
    void UpdateCharacterSprite()
    {
        if (currentLevel == null || linoCharacterImage == null) return;

        // Get earned stars for this level
        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(currentLevel.id);
        }

        // Show happy if any stars earned, sad if 0 stars
        if (earnedStars > 0)
        {
            if (linoHappy != null)
            {
                linoCharacterImage.sprite = linoHappy;
            }
        }
        else
        {
            if (linoSad != null)
            {
                linoCharacterImage.sprite = linoSad;
            }
        }

        Debug.Log($"[LevelPreview] Character sprite updated: {(earnedStars > 0 ? "Happy" : "Sad")} ({earnedStars} stars)");
    }

    /// <summary>
    /// Update stars display (filled/empty based on earned stars)
    /// </summary>
    void UpdateStarsDisplay()
    {
        if (currentLevel == null || stars == null) return;

        // Get earned stars
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

            // Set sprite based on earned stars
            if (i < earnedStars)
            {
                starImage.sprite = starFilled;
            }
            else
            {
                starImage.sprite = starEmpty;
            }
        }

        Debug.Log($"[LevelPreview] Stars display updated: {earnedStars}/3");
    }

    /// <summary>
    /// Update mission fragments list dengan ScrollRect support
    /// </summary>
    void UpdateMissionFragments()
    {

        if (currentLevel == null || fragmentListContent == null) return;

    // Return existing items to pool
    foreach (var item in spawnedFragmentItems)
    {
        if (item != null)
        {
            ReturnFragmentItemToPool(item);
        }
    }
    spawnedFragmentItems.Clear();

        if (currentLevel == null || fragmentListContent == null) return;

        // Clear existing fragments
        ClearSpawnedItems(spawnedFragmentItems);

        if (currentLevel.requirements == null || currentLevel.requirements.Count == 0)
        {
            Debug.LogWarning("[LevelPreview] No requirements for this level");
            return;
        }

        // Spawn fragment items
        foreach (var requirement in currentLevel.requirements)
        {

            
            if (fragmentItemPrefab == null)
            {
                Debug.LogError("[LevelPreview] fragmentItemPrefab not assigned!");
                break;
            }

            GameObject fragmentItem = Instantiate(fragmentItemPrefab, fragmentListContent);
            
            // Setup fragment item (gunakan FragmentPrefabRegistry untuk icon)
            SetupFragmentItem(fragmentItem, requirement);
            
            spawnedFragmentItems.Add(fragmentItem);
        }

        // Enable scroll jika items > 3
        if (fragmentScrollRect != null)
        {
            fragmentScrollRect.enabled = currentLevel.requirements.Count > 3;
            
            // Reset scroll position
            if (fragmentScrollRect.enabled)
            {
                Canvas.ForceUpdateCanvases();
                fragmentScrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        Debug.Log($"[LevelPreview] Mission fragments updated: {currentLevel.requirements.Count} items");
    }

    /// <summary>
    /// Setup individual fragment item UI
    /// </summary>
    void SetupFragmentItem(GameObject item, FragmentRequirement requirement)
    {
        // Cari Image component untuk icon (adjust sesuai hierarchy prefab)
        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = item.GetComponentInChildren<Image>();
        }

        // Cari Text component untuk count
        TMP_Text countText = item.transform.Find("CountText")?.GetComponent<TMP_Text>();
        if (countText == null)
        {
            countText = item.GetComponentInChildren<TMP_Text>();
        }

        // Get fragment icon dari FragmentPrefabRegistry
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
    /// Get fragment icon dari prefab registry
    /// </summary>
    Sprite GetFragmentIcon(FragmentType type, int variant)
{
    // Method 1: Try FragmentPrefabRegistry
    var registry = Resources.Load<FragmentPrefabRegistry>("FragmentPrefabRegistry");
    
    if (registry != null)
    {
        GameObject prefab = registry.GetPrefab(type, variant);
        if (prefab != null)
        {
            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                return sr.sprite;
            }
        }
    }

    // Method 2: Try FragmentPreviewManager (manual assignment)
    Sprite manualIcon = FragmentPreviewManager.GetFragmentIcon(type, variant);
    if (manualIcon != null)
    {
        return manualIcon;
    }

    Debug.LogWarning($"[LevelPreview] No icon found for {type} variant {variant}");
    return null;
}

    /// <summary>
    /// Generate random rewards based on level tier
    /// </summary>
    void GenerateAndDisplayRewards()
    {
        if (currentLevel == null) return;

        // Clear existing rewards
        ClearSpawnedItems(spawnedRewardItems);
        generatedRewards.Clear();

        // Get reward tier for this level
        LevelRewardTier tier = GetRewardTierForLevel(currentLevel.number);
        
        if (tier == null)
        {
            Debug.LogWarning($"[LevelPreview] No reward tier found for level {currentLevel.number}");
            return;
        }

        // Generate random number of rewards (1-3)
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

        Debug.Log($"[LevelPreview] Generated {generatedRewards.Count} rewards for level {currentLevel.number}");
    }

    /// <summary>
    /// Generate single random reward berdasarkan tier
    /// </summary>
    RewardData GenerateRandomReward(LevelRewardTier tier)
    {
        // Random reward type berdasarkan chances
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

    /// <summary>
    /// Generate random booster reward
    /// </summary>
    RewardData GenerateRandomBooster(LevelRewardTier tier)
    {
        string[] boosterTypes = { "speedboost", "timefreeze", "shield", "coin2x", "magnet" };
        string randomBooster = boosterTypes[Random.Range(0, boosterTypes.Length)];

        int amount = Random.Range(tier.minBoosterAmount, tier.maxBoosterAmount + 1);

        Sprite icon = GetBoosterIcon(randomBooster);

        return new RewardData
        {
            type = RewardType.Booster,
            boosterId = randomBooster,
            amount = amount,
            icon = icon,
            displayName = GetBoosterDisplayName(randomBooster)
        };
    }

    /// <summary>
    /// Generate coin reward
    /// </summary>
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

    /// <summary>
    /// Generate energy reward
    /// </summary>
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

    /// <summary>
    /// Get booster icon
    /// </summary>
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

    /// <summary>
    /// Get booster display name
    /// </summary>
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

    /// <summary>
    /// Display rewards di UI
    /// </summary>
    void DisplayRewards()
    {
        if (rewardItemsContainer == null || rewardItemPrefab == null) return;

        foreach (var reward in generatedRewards)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardItemsContainer);
            
            // Setup reward item UI
            SetupRewardItem(rewardItem, reward);
            
            spawnedRewardItems.Add(rewardItem);
        }

        // Update description (optional)
        if (rewardDescriptionText != null)
        {
            rewardDescriptionText.text = $"Complete this level to get {generatedRewards.Count} reward{(generatedRewards.Count > 1 ? "s" : "")}!";
        }
    }

    /// <summary>
    /// Setup individual reward item UI
    /// </summary>
    void SetupRewardItem(GameObject item, RewardData reward)
    {
        // Setup mirip dengan BundleItemDisplay
        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = item.GetComponentInChildren<Image>();
        }

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

    /// <summary>
    /// Get reward tier based on level number
    /// </summary>
    LevelRewardTier GetRewardTierForLevel(int levelNumber)
    {
        if (rewardTiers == null || rewardTiers.Length == 0)
        {
            Debug.LogWarning("[LevelPreview] No reward tiers defined!");
            return null;
        }

        // Cari tier yang sesuai
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
    /// Clear spawned items
    /// </summary>
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

    /// <summary>
    /// Play button clicked - start level dengan rewards saved
    /// </summary>
    void OnPlayButtonClicked()
    {
        if (currentLevel == null)
        {
            Debug.LogError("[LevelPreview] No level selected!");
            return;
        }

        // Save level info to PlayerPrefs
        PlayerPrefs.SetString("SelectedLevelId", currentLevel.id);
        PlayerPrefs.SetInt("SelectedLevelNumber", currentLevel.number);

        // Save generated rewards untuk diklaim setelah level complete
        SaveGeneratedRewards();

        PlayerPrefs.Save();

        // Play click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // Load gameplay scene
        Debug.Log($"[LevelPreview] Starting level {currentLevel.number}");
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Save generated rewards ke PlayerPrefs untuk diklaim later
    /// </summary>
    void SaveGeneratedRewards()
    {
        if (generatedRewards == null || generatedRewards.Count == 0) return;

        // Serialize rewards to JSON
        RewardDataList rewardList = new RewardDataList { rewards = generatedRewards };
        string json = JsonUtility.ToJson(rewardList);
        
        PlayerPrefs.SetString($"LevelRewards_{currentLevel.id}", json);
        
        Debug.Log($"[LevelPreview] Saved {generatedRewards.Count} rewards for {currentLevel.id}");
    }

    /// <summary>
    /// Close button clicked
    /// </summary>
    void OnCloseButtonClicked()
    {
        // Play click sound
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

        Debug.Log("[LevelPreview] Panel closed");
    }

    /// <summary>
    /// Static helper: Show preview dari external script
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
}

// ========================================
// Data Classes
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

[System.Serializable]
public class RewardData
{
    public RewardType type;
    public string boosterId; // untuk boosters
    public int amount;
    public Sprite icon;
    public string displayName;
}

[System.Serializable]
public class RewardDataList
{
    public List<RewardData> rewards;
}