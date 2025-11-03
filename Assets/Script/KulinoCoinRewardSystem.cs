using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kulino Coin Reward System dengan Chance-based rewards
/// Menentukan kapan player bisa claim Kulino Coin setelah level complete
/// </summary>
public class KulinoCoinRewardSystem : MonoBehaviour
{
    public static KulinoCoinRewardSystem Instance { get; private set; }

    [Header("Reward Chance Settings")]
    [Tooltip("Chance untuk dapat Kulino Coin per level (0-100%)")]
    [Range(0f, 100f)]
    public float rewardChancePerLevel = 3f; // 3% chance per level (default)

    [Tooltip("Guaranteed reward setiap N level (0 = disabled)")]
    public int guaranteedRewardEveryNLevels = 30; // Setiap 30 level pasti dapat

    [Tooltip("Minimum level untuk mulai dapat reward")]
    public int minimumLevelForReward = 1;

    [Header("Popup Reference")]
    [Tooltip("Drag PopupClaimCoinKulino GameObject")]
    public GameObject popupClaimCoin;

    [Header("Popup Components")]
    public TMP_Text deskItemText; // Text "Kulino Coin"
    public Image coinIconImage;   // Icon coin kulino
    public Sprite kulinoCoinIcon; // Assign icon dari Assets

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Save keys
    const string PREF_CLAIMED_LEVELS = "Kulino_ClaimedLevels_v1"; // CSV string: "1,5,12,..."

    // Runtime tracking
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
        // Hide popup initially
        if (popupClaimCoin != null)
        {
            popupClaimCoin.SetActive(false);
        }

        // Setup popup UI
        SetupPopupUI();

        // Subscribe to level complete event
        var session = FindFirstObjectByType<LevelGameSession>();
        if (session != null)
        {
            session.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Subscribed to LevelGameSession.OnLevelCompleted");
        }
        else
        {
            LogWarning("LevelGameSession not found! Reward system may not work.");
        }
    }

    /// <summary>
    /// Setup popup UI elements (text & icon)
    /// </summary>
    void SetupPopupUI()
    {
        if (deskItemText != null)
        {
            deskItemText.text = "Kulino Coin";
        }

        if (coinIconImage != null && kulinoCoinIcon != null)
        {
            coinIconImage.sprite = kulinoCoinIcon;
            coinIconImage.gameObject.SetActive(true);
        }

        Log("✓ Popup UI setup complete");
    }

    /// <summary>
    /// Called when level complete (dari LevelGameSession)
    /// </summary>
    void OnLevelComplete()
    {
        // Get current level number
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        int levelNum = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        currentLevel = levelNum;

        Log($"Level {levelNum} complete! Checking reward eligibility...");

        // Check if already claimed this level
        if (HasClaimedLevel(levelNum))
        {
            Log($"Level {levelNum} reward already claimed. Skipping.");
            return;
        }

        // Check if player eligible untuk reward
        if (ShouldGiveReward(levelNum))
        {
            Log($"✓ Player eligible untuk Kulino Coin reward!");
            ShowRewardPopup();
        }
        else
        {
            Log($"✗ No reward this level. Better luck next time!");
        }
    }

    /// <summary>
    /// Determine apakah player dapat reward di level ini
    /// </summary>
    bool ShouldGiveReward(int levelNumber)
    {
        // Check minimum level
        if (levelNumber < minimumLevelForReward)
        {
            Log($"Level {levelNumber} < minimum ({minimumLevelForReward})");
            return false;
        }

        // RULE 1: Guaranteed reward setiap N level
        if (guaranteedRewardEveryNLevels > 0 &&
            levelNumber % guaranteedRewardEveryNLevels == 0)
        {
            Log($"✓ Guaranteed reward! (Level {levelNumber} % {guaranteedRewardEveryNLevels} == 0)");
            return true;
        }

        // RULE 2: Random chance per level
        float roll = Random.Range(0f, 100f);
        bool won = roll < rewardChancePerLevel;

        Log($"Random roll: {roll:F2}% (need < {rewardChancePerLevel}%) = {(won ? "WIN" : "LOSE")}");

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
    /// Show popup claim coin
    /// </summary>
    void ShowRewardPopup()
    {
        if (popupClaimCoin == null)
        {
            LogError("popupClaimCoin not assigned!");
            return;
        }

        hasRewardThisLevel = true;

        // Show popup
        popupClaimCoin.SetActive(true);

        // Pause game (optional)
        Time.timeScale = 0f;

        Log("✓ Reward popup shown");
    }

    /// <summary>
    /// Called dari Confirm button di popup
    /// Triggers GameManager.OnClaimButtonClick() untuk start transaksi
    /// </summary>
    public void OnConfirmButtonClicked()
    {
        Log("Confirm button clicked!");

        // Mark level as claimed BEFORE starting transaction
        MarkLevelClaimed(currentLevel);

        // Trigger GameManager claim transaction
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnClaimButtonClick();
        }
        else
        {
            LogError("GameManager not found!");
        }

        // Hide popup setelah trigger claim
        HidePopup();
    }

    /// <summary>
    /// Called dari Close button (X) di popup
    /// </summary>
    public void OnCloseButtonClicked()
    {
        Log("Close button clicked - skipping reward");

        // Mark level as claimed even if skipped (agar tidak muncul lagi)
        MarkLevelClaimed(currentLevel);

        HidePopup();
    }

    /// <summary>
    /// Hide popup dan resume game
    /// </summary>
    void HidePopup()
    {
        if (popupClaimCoin != null)
        {
            popupClaimCoin.SetActive(false);
        }

        // Resume game
        Time.timeScale = 1f;

        hasRewardThisLevel = false;

        Log("✓ Popup hidden");
    }

    // ========================================
    // DEBUG HELPERS
    // ========================================

    [ContextMenu("Test: Force Show Reward Popup")]
    void Context_ForceShowReward()
    {
        ShowRewardPopup();
    }

    [ContextMenu("Test: Simulate Level Complete")]
    void Context_SimulateLevelComplete()
    {
        currentLevel++;
        OnLevelComplete();
    }

    [ContextMenu("Debug: Clear Claimed Levels")]
    void Context_ClearClaimed()
    {
        PlayerPrefs.DeleteKey(PREF_CLAIMED_LEVELS);
        PlayerPrefs.Save();
        Debug.Log("[KulinoCoinReward] Cleared claimed levels");
    }

    [ContextMenu("Debug: Print Claimed Levels")]
    void Context_PrintClaimed()
    {
        string claimed = PlayerPrefs.GetString(PREF_CLAIMED_LEVELS, "");
        Debug.Log($"[KulinoCoinReward] Claimed levels: {(string.IsNullOrEmpty(claimed) ? "NONE" : claimed)}");
    }

    [ContextMenu("Debug: Print Reward Stats")]
    void Context_PrintStats()
    {
        Debug.Log("=== KULINO COIN REWARD STATS ===");
        Debug.Log($"Current Level: {currentLevel}");
        Debug.Log($"Chance Per Level: {rewardChancePerLevel}%");
        Debug.Log($"Guaranteed Every: {guaranteedRewardEveryNLevels} levels");
        Debug.Log($"Minimum Level: {minimumLevelForReward}");
        Debug.Log($"Has Reward This Level: {hasRewardThisLevel}");
        Debug.Log("================================");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[KulinoCoinReward] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[KulinoCoinReward] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[KulinoCoinReward] {message}");
    }
}