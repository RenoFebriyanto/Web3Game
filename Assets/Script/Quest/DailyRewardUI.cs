using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ✅✅✅ DUAL BUTTON VERSION: Energy Button + Shard Button terpisah
/// - 2 button terpisah dengan popup masing-masing
/// - Random amount di-roll saat reward available (bukan saat claim)
/// - Real-time daily reset (offline support)
/// - Full integration dengan DailyRewardSystem
/// </summary>
public class DailyRewardUI : MonoBehaviour
{
    [Header("=== ENERGY BUTTON ===")]
    [Tooltip("Button untuk claim Energy")]
    public Button energyButton;

    [Tooltip("Sprite saat energy reward available")]
    public Sprite energyButtonAvailable;

    [Tooltip("Sprite saat energy locked/claimed")]
    public Sprite energyButtonLocked;

    [Header("=== SHARD BUTTON ===")]
    [Tooltip("Button untuk claim Shard")]
    public Button shardButton;

    [Tooltip("Sprite saat shard reward available")]
    public Sprite shardButtonAvailable;

    [Tooltip("Sprite saat shard locked/claimed")]
    public Sprite shardButtonLocked;

    [Header("=== OPTIONAL: TEXT DISPLAYS ===")]
    [Tooltip("Text untuk energy amount (optional)")]
    public TMP_Text energyAmountText;

    [Tooltip("Text untuk shard amount (optional)")]
    public TMP_Text shardAmountText;

    [Header("=== ICONS (untuk popup) ===")]
    [Tooltip("Icon energy (untuk popup)")]
    public Sprite iconEnergy;

    [Tooltip("Icon shard (untuk popup)")]
    public Sprite iconShard;

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = true;

    // Button images
    private Image energyButtonImage;
    private Image shardButtonImage;

    // Claim tracking (per-button)
    private bool energyClaimedThisSession = false;
    private bool shardClaimedThisSession = false;

    void Awake()
    {
        // Get button images
        if (energyButton != null)
        {
            energyButtonImage = energyButton.GetComponent<Image>();
        }

        if (shardButton != null)
        {
            shardButtonImage = shardButton.GetComponent<Image>();
        }
    }

    void Start()
    {
        // Setup button callbacks
        SetupButtons();

        // Subscribe to DailyRewardSystem events
        SubscribeToEvents();

        // Initial refresh
        RefreshUI();

        Log("✓ DailyRewardUI (Dual Button) initialized");
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ========================================
    // BUTTON SETUP
    // ========================================

    void SetupButtons()
    {
        if (energyButton != null)
        {
            energyButton.onClick.RemoveAllListeners();
            energyButton.onClick.AddListener(OnEnergyButtonClicked);
            Log("✓ Energy button setup");
        }
        else
        {
            LogError("energyButton is NULL!");
        }

        if (shardButton != null)
        {
            shardButton.onClick.RemoveAllListeners();
            shardButton.onClick.AddListener(OnShardButtonClicked);
            Log("✓ Shard button setup");
        }
        else
        {
            LogError("shardButton is NULL!");
        }
    }

    // ========================================
    // EVENT SUBSCRIPTION
    // ========================================

    void SubscribeToEvents()
    {
        if (DailyRewardSystem.Instance == null)
        {
            LogWarning("DailyRewardSystem.Instance is null!");
            return;
        }

        DailyRewardSystem.Instance.OnRewardAvailable.AddListener(OnRewardAvailable);
        DailyRewardSystem.Instance.OnRewardClaimed.AddListener(OnRewardClaimed);
        DailyRewardSystem.Instance.OnRewardReset.AddListener(OnRewardReset);

        Log("✓ Subscribed to DailyRewardSystem events");
    }

    void UnsubscribeFromEvents()
    {
        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.OnRewardAvailable.RemoveListener(OnRewardAvailable);
            DailyRewardSystem.Instance.OnRewardClaimed.RemoveListener(OnRewardClaimed);
            DailyRewardSystem.Instance.OnRewardReset.RemoveListener(OnRewardReset);
        }
    }

    // ========================================
    // EVENT HANDLERS
    // ========================================

    void OnRewardAvailable()
    {
        Log("✓ Reward available event received");
        energyClaimedThisSession = false;
        shardClaimedThisSession = false;
        RefreshUI();
    }

    void OnRewardClaimed()
    {
        Log("✓ Reward claimed event received");
        RefreshUI();
    }

    void OnRewardReset()
    {
        Log("✓ Reward reset event received");
        energyClaimedThisSession = false;
        shardClaimedThisSession = false;
        RefreshUI();
    }

    // ========================================
    // UI REFRESH
    // ========================================

    public void RefreshUI()
    {
        if (DailyRewardSystem.Instance == null)
        {
            LogError("DailyRewardSystem.Instance is null!");
            SetAllButtonsLocked();
            return;
        }

        bool isAvailable = DailyRewardSystem.Instance.IsRewardAvailable();
        bool isClaimed = DailyRewardSystem.Instance.IsRewardClaimed();

        Log($"RefreshUI: available={isAvailable}, claimed={isClaimed}");

        // Get rolled amounts
        int energyAmount = DailyRewardSystem.Instance.GetRolledEnergyAmount();
        int shardAmount = DailyRewardSystem.Instance.GetRolledShardAmount();

        Log($"Amounts: Energy={energyAmount}, Shard={shardAmount}");

        // ✅ ENERGY BUTTON
        if (isClaimed || energyClaimedThisSession)
        {
            SetEnergyButtonLocked();
        }
        else if (isAvailable)
        {
            SetEnergyButtonAvailable(energyAmount);
        }
        else
        {
            SetEnergyButtonLocked();
        }

        // ✅ SHARD BUTTON
        if (isClaimed || shardClaimedThisSession)
        {
            SetShardButtonLocked();
        }
        else if (isAvailable && shardAmount > 0)
        {
            SetShardButtonAvailable(shardAmount);
        }
        else
        {
            SetShardButtonLocked();
        }
    }

    // ========================================
    // ENERGY BUTTON STATE
    // ========================================

    void SetEnergyButtonAvailable(int amount)
    {
        if (energyButton != null)
        {
            energyButton.interactable = true;
        }

        if (energyButtonImage != null && energyButtonAvailable != null)
        {
            energyButtonImage.sprite = energyButtonAvailable;
            energyButtonImage.color = Color.white;
        }

        if (energyAmountText != null)
        {
            energyAmountText.text = amount.ToString();
            energyAmountText.gameObject.SetActive(true);
        }

        Log($"Energy button: AVAILABLE ({amount})");
    }

    void SetEnergyButtonLocked()
    {
        if (energyButton != null)
        {
            energyButton.interactable = false;
        }

        if (energyButtonImage != null && energyButtonLocked != null)
        {
            energyButtonImage.sprite = energyButtonLocked;
            energyButtonImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        if (energyAmountText != null)
        {
            energyAmountText.gameObject.SetActive(false);
        }

        Log("Energy button: LOCKED");
    }

    // ========================================
    // SHARD BUTTON STATE
    // ========================================

    void SetShardButtonAvailable(int amount)
    {
        if (shardButton != null)
        {
            shardButton.interactable = true;
        }

        if (shardButtonImage != null && shardButtonAvailable != null)
        {
            shardButtonImage.sprite = shardButtonAvailable;
            shardButtonImage.color = Color.white;
        }

        if (shardAmountText != null)
        {
            shardAmountText.text = amount.ToString();
            shardAmountText.gameObject.SetActive(true);
        }

        Log($"Shard button: AVAILABLE ({amount})");
    }

    void SetShardButtonLocked()
    {
        if (shardButton != null)
        {
            shardButton.interactable = false;
        }

        if (shardButtonImage != null && shardButtonLocked != null)
        {
            shardButtonImage.sprite = shardButtonLocked;
            shardButtonImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        if (shardAmountText != null)
        {
            shardAmountText.gameObject.SetActive(false);
        }

        Log("Shard button: LOCKED");
    }

    void SetAllButtonsLocked()
    {
        SetEnergyButtonLocked();
        SetShardButtonLocked();
    }

    // ========================================
    // BUTTON CALLBACKS
    // ========================================

    /// <summary>
    /// ✅ Called when Energy button clicked
    /// </summary>
    void OnEnergyButtonClicked()
    {
        if (DailyRewardSystem.Instance == null)
        {
            LogError("DailyRewardSystem.Instance is null!");
            return;
        }

        if (!DailyRewardSystem.Instance.IsRewardAvailable())
        {
            LogWarning("Reward not available!");
            return;
        }

        if (energyClaimedThisSession)
        {
            LogWarning("Energy already claimed this session!");
            return;
        }

        int energyAmount = DailyRewardSystem.Instance.GetRolledEnergyAmount();

        Log($"✅ Energy button clicked! Amount: {energyAmount}");

        // Show popup
        ShowEnergyClaimPopup(energyAmount);
    }

    /// <summary>
    /// ✅ Called when Shard button clicked
    /// </summary>
    void OnShardButtonClicked()
    {
        if (DailyRewardSystem.Instance == null)
        {
            LogError("DailyRewardSystem.Instance is null!");
            return;
        }

        if (!DailyRewardSystem.Instance.IsRewardAvailable())
        {
            LogWarning("Reward not available!");
            return;
        }

        if (shardClaimedThisSession)
        {
            LogWarning("Shard already claimed this session!");
            return;
        }

        int shardAmount = DailyRewardSystem.Instance.GetRolledShardAmount();

        if (shardAmount <= 0)
        {
            LogWarning("No shard reward this time!");
            return;
        }

        Log($"✅ Shard button clicked! Amount: {shardAmount}");

        // Show popup
        ShowShardClaimPopup(shardAmount);
    }

    // ========================================
    // POPUP DISPLAY
    // ========================================

    void ShowEnergyClaimPopup(int amount)
    {
        if (PopupClaimQuest.Instance == null)
        {
            LogError("PopupClaimQuest.Instance is null! Claiming directly.");
            ClaimEnergy(amount);
            return;
        }

        Sprite icon = GetEnergyIcon();
        if (icon == null)
        {
            LogWarning("Energy icon not found!");
        }

        Log($"Opening popup: Energy x{amount}");

        PopupClaimQuest.Instance.Open(
            icon,
            amount.ToString(),
            "Energy",
            () => {
                Log("✅ Popup confirmed! Claiming energy...");
                ClaimEnergy(amount);
            }
        );
    }

    void ShowShardClaimPopup(int amount)
    {
        if (PopupClaimQuest.Instance == null)
        {
            LogError("PopupClaimQuest.Instance is null! Claiming directly.");
            ClaimShard(amount);
            return;
        }

        Sprite icon = GetShardIcon();
        if (icon == null)
        {
            LogWarning("Shard icon not found!");
        }

        Log($"Opening popup: Shard x{amount}");

        PopupClaimQuest.Instance.Open(
            icon,
            amount.ToString(),
            "Blue Shard",
            () => {
                Log("✅ Popup confirmed! Claiming shard...");
                ClaimShard(amount);
            }
        );
    }

    // ========================================
    // CLAIM LOGIC
    // ========================================

    void ClaimEnergy(int amount)
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddEnergy(amount);
            Log($"✓ Granted {amount} Energy");
        }
        else
        {
            LogError("PlayerEconomy.Instance is null!");
        }

        // Mark as claimed
        energyClaimedThisSession = true;

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }

        // Check if both claimed -> mark reward as fully claimed
        CheckFullyClaimed();

        // Refresh UI
        RefreshUI();
    }

    void ClaimShard(int amount)
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddShards(amount);
            Log($"✓ Granted {amount} Shards");
        }
        else
        {
            LogError("PlayerEconomy.Instance is null!");
        }

        // Mark as claimed
        shardClaimedThisSession = true;

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }

        // Check if both claimed -> mark reward as fully claimed
        CheckFullyClaimed();

        // Refresh UI
        RefreshUI();
    }

    /// <summary>
    /// ✅ Check if both rewards claimed -> mark as fully claimed
    /// </summary>
    void CheckFullyClaimed()
    {
        if (DailyRewardSystem.Instance == null) return;

        int shardAmount = DailyRewardSystem.Instance.GetRolledShardAmount();

        // If shard = 0, only need energy claimed
        bool needShard = shardAmount > 0;

        bool fullyClaimed = energyClaimedThisSession && (!needShard || shardClaimedThisSession);

        if (fullyClaimed)
        {
            Log("✅✅✅ ALL REWARDS CLAIMED! Marking as fully claimed.");
            DailyRewardSystem.Instance.ClaimReward();
        }
    }

    // ========================================
    // ICON HELPERS
    // ========================================

    Sprite GetEnergyIcon()
    {
        // Priority 1: Assigned icon
        if (iconEnergy != null) return iconEnergy;

        // Priority 2: From ShopManager
        var shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null && shopManager.iconEnergy != null)
        {
            return shopManager.iconEnergy;
        }

        LogWarning("Energy icon not found!");
        return null;
    }

    Sprite GetShardIcon()
    {
        // Priority 1: Assigned icon
        if (iconShard != null) return iconShard;

        // Priority 2: From ShopManager
        var shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null && shopManager.iconShard != null)
        {
            return shopManager.iconShard;
        }

        LogWarning("Shard icon not found!");
        return null;
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public void ForceRefresh()
    {
        RefreshUI();
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[DailyRewardUI] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[DailyRewardUI] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[DailyRewardUI] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU
    // ========================================

    [ContextMenu("Debug: Force Refresh UI")]
    void Context_ForceRefresh()
    {
        RefreshUI();
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== DAILY REWARD UI STATUS ===");
        Debug.Log($"energyButton: {(energyButton != null ? "OK" : "NULL")}");
        Debug.Log($"shardButton: {(shardButton != null ? "OK" : "NULL")}");
        Debug.Log($"energyClaimedThisSession: {energyClaimedThisSession}");
        Debug.Log($"shardClaimedThisSession: {shardClaimedThisSession}");

        if (DailyRewardSystem.Instance != null)
        {
            Debug.Log($"\nReward Status:");
            Debug.Log($"  Available: {DailyRewardSystem.Instance.IsRewardAvailable()}");
            Debug.Log($"  Claimed: {DailyRewardSystem.Instance.IsRewardClaimed()}");
            Debug.Log($"  Energy Amount: {DailyRewardSystem.Instance.GetRolledEnergyAmount()}");
            Debug.Log($"  Shard Amount: {DailyRewardSystem.Instance.GetRolledShardAmount()}");
        }
        Debug.Log("==============================");
    }
}