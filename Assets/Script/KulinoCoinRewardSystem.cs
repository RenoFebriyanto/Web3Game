using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FINAL: Kulino Coin Reward System - Uses existing PopupClaimCoinKulino structure
/// Tracks total levels played, shows popup after 20-30 levels with random chance
/// </summary>
public class KulinoCoinRewardSystem : MonoBehaviour
{
    public static KulinoCoinRewardSystem Instance { get; private set; }

    [Header("Reward Chance Settings")]
    [Tooltip("Minimum total levels played sebelum bisa dapat reward")]
    public int minimumLevelsPlayedForReward = 20;

    [Tooltip("Setelah berapa level played, chance mulai meningkat")]
    public int levelsForIncreasedChance = 30;

    [Tooltip("Base chance untuk dapat Kulino Coin setelah minimum reached (0-100%)")]
    [Range(0f, 100f)]
    public float baseRewardChance = 5f; // 5% chance

    [Tooltip("Increased chance setelah 30+ levels (0-100%)")]
    [Range(0f, 100f)]
    public float increasedRewardChance = 10f; // 10% chance

    [Tooltip("Guaranteed reward setiap N level (0 = disabled)")]
    public int guaranteedRewardEveryNLevels = 50;

    [Header("Popup Reference - SEPARATE GAMEOBJECT")]
    [Tooltip("Drag PopupClaimCoinKulino GameObject dari Hierarchy")]
    public GameObject popupClaimCoinKulino;

    [Header("Popup Components - From Hierarchy")]
    [Tooltip("DeskItem Text (will be set to 'Kulino Coin')")]
    public TMP_Text deskItemText;

    [Tooltip("Icon Image untuk Kulino Coin")]
    public Image coinIconImage;

    [Tooltip("Kulino Coin icon sprite")]
    public Sprite kulinoCoinIcon;

    [Tooltip("Confirm Button (ConfirmBTN)")]
    public Button confirmButton;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Save keys
    const string PREF_TOTAL_LEVELS_PLAYED = "Kulino_TotalLevelsPlayed_v1";
    const string PREF_CLAIMED_LEVELS = "Kulino_ClaimedLevels_v1"; // CSV: "5,12,..."

    // Runtime
    private int totalLevelsPlayed = 0;
    private int currentLevel = 0;
    private bool hasRewardThisLevel = false;

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
        // Load total levels played
        totalLevelsPlayed = PlayerPrefs.GetInt(PREF_TOTAL_LEVELS_PLAYED, 0);
        Log($"Total levels played so far: {totalLevelsPlayed}");

        // Hide popup initially
        if (popupClaimCoinKulino != null)
        {
            popupClaimCoinKulino.SetActive(false);
        }
        else
        {
            LogError("popupClaimCoinKulino not assigned in Inspector!");
        }

        // Setup popup UI
        SetupPopupUI();

        // Setup button
        SetupButton();

        // Subscribe to level complete event
        var session = FindFirstObjectByType<LevelGameSession>();
        if (session != null)
        {
            session.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Subscribed to LevelGameSession.OnLevelCompleted");
        }
        else
        {
            LogWarning("LevelGameSession not found! Will try late subscribe...");
            Invoke(nameof(LateSubscribe), 0.5f);
        }
    }

    void LateSubscribe()
    {
        var session = FindFirstObjectByType<LevelGameSession>();
        if (session != null)
        {
            session.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Late subscribed to LevelGameSession.OnLevelCompleted");
        }
        else
        {
            LogError("LevelGameSession still not found after delay!");
        }
    }

    void SetupPopupUI()
    {
        // Set DeskItem text to "Kulino Coin"
        if (deskItemText != null)
        {
            deskItemText.text = "Kulino Coin";
            Log("✓ Set DeskItem text to 'Kulino Coin'");
        }
        else
        {
            LogWarning("deskItemText not assigned!");
        }

        // Set coin icon
        if (coinIconImage != null && kulinoCoinIcon != null)
        {
            coinIconImage.sprite = kulinoCoinIcon;
            coinIconImage.gameObject.SetActive(true);
            Log($"✓ Set coin icon: {kulinoCoinIcon.name}");
        }
        else
        {
            if (coinIconImage == null) LogWarning("coinIconImage not assigned!");
            if (kulinoCoinIcon == null) LogWarning("kulinoCoinIcon sprite not assigned!");
        }
    }

    void SetupButton()
    {
        if (confirmButton != null)
        {
            // Remove any existing listeners first
            confirmButton.onClick.RemoveAllListeners();

            // Add our listener
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            Log("✓ Confirm button listener setup complete");
        }
        else
        {
            LogError("confirmButton not assigned in Inspector!");
        }
    }

    /// <summary>
    /// Called when level complete (via LevelGameSession.OnLevelCompleted event)
    /// </summary>
    void OnLevelComplete()
    {
        // Increment total levels played
        totalLevelsPlayed++;
        PlayerPrefs.SetInt(PREF_TOTAL_LEVELS_PLAYED, totalLevelsPlayed);
        PlayerPrefs.Save();

        // Get current level number
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        int levelNum = PlayerPrefs.GetInt("SelectedLevelNumber", 1);
        currentLevel = levelNum;

        Log($"=== LEVEL {levelNum} COMPLETE ===");
        Log($"Total levels played: {totalLevelsPlayed}");

        // Check if already claimed this level
        if (HasClaimedLevel(levelNum))
        {
            Log($"Level {levelNum} reward already claimed. Skipping.");
            return;
        }

        // Check if eligible untuk reward
        if (ShouldGiveReward(levelNum))
        {
            Log($"✅ Player eligible untuk Kulino Coin reward!");
            ShowRewardPopup();
        }
        else
        {
            Log($"❌ No reward this level.");
            Log($"   Total played: {totalLevelsPlayed}/{minimumLevelsPlayedForReward}");
        }
    }

    /// <summary>
    /// Determine apakah player dapat reward di level ini
    /// </summary>
    bool ShouldGiveReward(int levelNumber)
    {
        // RULE 1: Check minimum levels played
        if (totalLevelsPlayed < minimumLevelsPlayedForReward)
        {
            Log($"❌ Total played {totalLevelsPlayed} < minimum {minimumLevelsPlayedForReward}");
            return false;
        }

        // RULE 2: Guaranteed reward setiap N level
        if (guaranteedRewardEveryNLevels > 0 &&
            totalLevelsPlayed % guaranteedRewardEveryNLevels == 0)
        {
            Log($"✅ GUARANTEED REWARD! (Total played {totalLevelsPlayed} % {guaranteedRewardEveryNLevels} == 0)");
            return true;
        }

        // RULE 3: Random chance (increased after 30+ levels)
        float chanceToUse = totalLevelsPlayed >= levelsForIncreasedChance ?
            increasedRewardChance : baseRewardChance;

        float roll = Random.Range(0f, 100f);
        bool won = roll < chanceToUse;

        Log($"🎲 Random roll: {roll:F2}% (need < {chanceToUse:F1}%) = {(won ? "✅ WIN!" : "❌ LOSE")}");

        return won;
    }

    /// <summary>
    /// Check apakah level sudah pernah di-claim
    /// </summary>
    bool HasClaimedLevel(int levelNumber)
    {
        string claimed = PlayerPrefs.GetString(PREF_CLAIMED_LEVELS, "");
        if (string.IsNullOrEmpty(claimed)) return false;

        string[] levels = claimed.Split(',');
        foreach (string lvl in levels)
        {
            if (int.TryParse(lvl, out int num) && num == levelNumber)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Mark level as claimed
    /// </summary>
    void MarkLevelClaimed(int levelNumber)
    {
        string claimed = PlayerPrefs.GetString(PREF_CLAIMED_LEVELS, "");

        if (string.IsNullOrEmpty(claimed))
        {
            claimed = levelNumber.ToString();
        }
        else
        {
            claimed += "," + levelNumber.ToString();
        }

        PlayerPrefs.SetString(PREF_CLAIMED_LEVELS, claimed);
        PlayerPrefs.Save();

        Log($"✓ Marked level {levelNumber} as claimed");
    }

    /// <summary>
    /// Show popup claim coin kulino
    /// </summary>
    void ShowRewardPopup()
    {
        if (popupClaimCoinKulino == null)
        {
            LogError("❌ popupClaimCoinKulino not assigned!");
            return;
        }

        hasRewardThisLevel = true;

        // Show popup
        popupClaimCoinKulino.SetActive(true);

        Log("✅ Kulino Coin reward popup shown!");

        // ✅ Play popup open sound (if SoundManager handles it, otherwise skip)
        if (SoundManager.Instance != null)
        {
            // Check if SoundManager has popup open sound method
            // If not, it will be handled by other script (like PopupClaimQuest)
            // We don't need to manually play sound here
            Log("✓ Popup sound will be handled by existing sound system");
        }

        // Pause game (optional - tergantung desain)
        // Time.timeScale = 0f; // Uncomment jika ingin pause game
    }

    /// <summary>
    /// Called dari Confirm button (ConfirmBTN)
    /// Triggers GameManager.OnClaimButtonClick() untuk start transaksi
    /// </summary>
    public void OnConfirmButtonClicked()
    {
        Log("🎯 Confirm button clicked!");

        // Play button click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
            Log("✓ Played button click sound");
        }

        // Mark level as claimed BEFORE starting transaction
        MarkLevelClaimed(currentLevel);

        // ✅ Trigger GameManager claim transaction
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClaimButtonClick();
            Log("✅ Triggered GameManager.OnClaimButtonClick() - Transaction started!");
        }
        else
        {
            LogError("❌ GameManager.Instance is NULL! Cannot start transaction.");
        }

        // Hide popup automatically (no close button needed)
        HidePopup();
    }

    /// <summary>
    /// Hide popup dan resume game
    /// </summary>
    void HidePopup()
    {
        if (popupClaimCoinKulino != null)
        {
            popupClaimCoinKulino.SetActive(false);
            Log("✓ Popup hidden");
        }

        // Resume game if paused
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        hasRewardThisLevel = false;
    }

    // ========================================
    // DEBUG CONTEXT MENU HELPERS
    // ========================================

    [ContextMenu("🎁 Test: Force Show Reward Popup")]
    void Context_ForceShowReward()
    {
        currentLevel = PlayerPrefs.GetInt("SelectedLevelNumber", 1);
        ShowRewardPopup();
    }

    [ContextMenu("➕ Test: Add 10 Levels Played")]
    void Context_Add10Levels()
    {
        totalLevelsPlayed += 10;
        PlayerPrefs.SetInt(PREF_TOTAL_LEVELS_PLAYED, totalLevelsPlayed);
        PlayerPrefs.Save();
        Debug.Log($"[KulinoCoinReward] ➕ Added 10 levels. Total: {totalLevelsPlayed}");
    }

    [ContextMenu("➕ Test: Add 1 Level Played")]
    void Context_Add1Level()
    {
        totalLevelsPlayed++;
        PlayerPrefs.SetInt(PREF_TOTAL_LEVELS_PLAYED, totalLevelsPlayed);
        PlayerPrefs.Save();
        Debug.Log($"[KulinoCoinReward] ➕ Added 1 level. Total: {totalLevelsPlayed}");
    }

    [ContextMenu("🔄 Test: Reset Total Levels to 0")]
    void Context_ResetTotalLevels()
    {
        totalLevelsPlayed = 0;
        PlayerPrefs.SetInt(PREF_TOTAL_LEVELS_PLAYED, 0);
        PlayerPrefs.Save();
        Debug.Log("[KulinoCoinReward] 🔄 Reset total levels played to 0");
    }

    [ContextMenu("🔄 Test: Set Total Levels to 20")]
    void Context_SetTo20Levels()
    {
        totalLevelsPlayed = 20;
        PlayerPrefs.SetInt(PREF_TOTAL_LEVELS_PLAYED, 20);
        PlayerPrefs.Save();
        Debug.Log("[KulinoCoinReward] 🔄 Set total levels to 20");
    }

    [ContextMenu("🔄 Test: Set Total Levels to 30")]
    void Context_SetTo30Levels()
    {
        totalLevelsPlayed = 30;
        PlayerPrefs.SetInt(PREF_TOTAL_LEVELS_PLAYED, 30);
        PlayerPrefs.Save();
        Debug.Log("[KulinoCoinReward] 🔄 Set total levels to 30");
    }

    [ContextMenu("🗑️ Debug: Clear Claimed Levels")]
    void Context_ClearClaimed()
    {
        PlayerPrefs.DeleteKey(PREF_CLAIMED_LEVELS);
        PlayerPrefs.Save();
        Debug.Log("[KulinoCoinReward] 🗑️ Cleared claimed levels");
    }

    [ContextMenu("🗑️ Debug: Clear ALL Save Data")]
    void Context_ClearAllSaveData()
    {
        PlayerPrefs.DeleteKey(PREF_TOTAL_LEVELS_PLAYED);
        PlayerPrefs.DeleteKey(PREF_CLAIMED_LEVELS);
        PlayerPrefs.Save();
        totalLevelsPlayed = 0;
        Debug.Log("[KulinoCoinReward] 🗑️ Cleared ALL save data (total levels + claimed levels)");
    }

    [ContextMenu("📊 Debug: Print Stats")]
    void Context_PrintStats()
    {
        string claimed = PlayerPrefs.GetString(PREF_CLAIMED_LEVELS, "");

        Debug.Log("====================================");
        Debug.Log("   KULINO COIN REWARD SYSTEM STATS");
        Debug.Log("====================================");
        Debug.Log($"📊 Total Levels Played: {totalLevelsPlayed}");
        Debug.Log($"📏 Minimum Required: {minimumLevelsPlayedForReward}");
        Debug.Log($"🎲 Base Chance: {baseRewardChance}%");
        Debug.Log($"🎲 Increased Chance (30+): {increasedRewardChance}%");
        Debug.Log($"🎁 Guaranteed Every: {guaranteedRewardEveryNLevels} levels");
        Debug.Log($"📍 Current Level: {currentLevel}");
        Debug.Log($"✅ Claimed Levels: {(string.IsNullOrEmpty(claimed) ? "None" : claimed)}");
        Debug.Log($"🎯 Next Guaranteed: {(guaranteedRewardEveryNLevels > 0 ? (guaranteedRewardEveryNLevels - (totalLevelsPlayed % guaranteedRewardEveryNLevels)).ToString() : "Disabled")}");
        Debug.Log("====================================");
    }

    [ContextMenu("🎲 Test: Simulate Level Complete (with chance)")]
    void Context_SimulateLevelComplete()
    {
        currentLevel++;
        OnLevelComplete();
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[KulinoCoinReward] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[KulinoCoinReward] ⚠️ {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[KulinoCoinReward] ❌ {message}");
    }

    void OnDestroy()
    {
        // Cleanup event subscription
        var session = FindFirstObjectByType<LevelGameSession>();
        if (session != null)
        {
            session.OnLevelCompleted.RemoveListener(OnLevelComplete);
        }
    }
}