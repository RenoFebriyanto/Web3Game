using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller untuk Daily Reward button di LevelP
/// Attach ke GameObject "RewardQuest"
/// </summary>
public class DailyRewardUI : MonoBehaviour
{
    [Header("=== BUTTON COMPONENT ===")]
    [Tooltip("Button component (akan auto-find jika null)")]
    public Button claimButton;

    [Header("=== VISUAL STATES ===")]
    [Tooltip("Sprite saat reward available (bisa diklik)")]
    public Sprite buttonSpriteAvailable;

    [Tooltip("Sprite saat reward locked/claimed (gray)")]
    public Sprite buttonSpriteLocked;

    [Header("=== OPTIONAL: TEXT DISPLAYS ===")]
    [Tooltip("Text untuk display info (optional)")]
    public TMP_Text infoText;

    [Header("=== OPTIONAL: ICON DISPLAYS ===")]
    [Tooltip("Icon shard (optional)")]
    public Image shardIcon;

    [Tooltip("Icon energy (optional)")]
    public Image energyIcon;

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = true;

    private Image buttonImage;

    void Awake()
    {
        // Auto-find components
        if (claimButton == null)
        {
            claimButton = GetComponent<Button>();

            if (claimButton == null)
            {
                claimButton = GetComponentInChildren<Button>();
            }
        }

        if (claimButton != null)
        {
            buttonImage = claimButton.GetComponent<Image>();
        }
    }

    void Start()
    {
        // Setup button callback
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }
        else
        {
            LogError("claimButton is NULL! Cannot setup callback.");
        }

        // Subscribe to DailyRewardSystem events
        SubscribeToEvents();

        // Initial refresh
        RefreshUI();

        Log("✓ DailyRewardUI initialized");
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ========================================
    // EVENT SUBSCRIPTION
    // ========================================

    void SubscribeToEvents()
    {
        if (DailyRewardSystem.Instance == null)
        {
            LogWarning("DailyRewardSystem.Instance is null! Cannot subscribe to events.");
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
        RefreshUI();
    }

    // ========================================
    // UI REFRESH
    // ========================================

    /// <summary>
    /// Update UI berdasarkan state reward
    /// </summary>
    public void RefreshUI()
    {
        if (DailyRewardSystem.Instance == null)
        {
            LogError("DailyRewardSystem.Instance is null!");
            SetLockedState();
            return;
        }

        bool isAvailable = DailyRewardSystem.Instance.IsRewardAvailable();
        bool isClaimed = DailyRewardSystem.Instance.IsRewardClaimed();

        Log($"RefreshUI: available={isAvailable}, claimed={isClaimed}");

        if (isAvailable)
        {
            SetAvailableState();
        }
        else
        {
            SetLockedState();
        }

        // Update info text (optional)
        UpdateInfoText(isAvailable, isClaimed);
    }

    /// <summary>
    /// Set button ke state AVAILABLE (bisa diklik)
    /// </summary>
    void SetAvailableState()
    {
        if (claimButton != null)
        {
            claimButton.interactable = true;
        }

        if (buttonImage != null && buttonSpriteAvailable != null)
        {
            buttonImage.sprite = buttonSpriteAvailable;
        }

        // Optional: Set color
        if (buttonImage != null)
        {
            buttonImage.color = Color.white;
        }

        // Show icons
        if (shardIcon != null) shardIcon.gameObject.SetActive(true);
        if (energyIcon != null) energyIcon.gameObject.SetActive(true);

        Log("UI set to AVAILABLE state");
    }

    /// <summary>
    /// Set button ke state LOCKED (tidak bisa diklik)
    /// </summary>
    void SetLockedState()
    {
        if (claimButton != null)
        {
            claimButton.interactable = false;
        }

        if (buttonImage != null && buttonSpriteLocked != null)
        {
            buttonImage.sprite = buttonSpriteLocked;
        }

        // Optional: Set gray color
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        // Hide icons
        if (shardIcon != null) shardIcon.gameObject.SetActive(false);
        if (energyIcon != null) energyIcon.gameObject.SetActive(false);

        Log("UI set to LOCKED state");
    }

    /// <summary>
    /// Update info text (optional)
    /// </summary>
    void UpdateInfoText(bool isAvailable, bool isClaimed)
    {
        if (infoText == null) return;

        if (isClaimed)
        {
            infoText.text = "Claimed!";
        }
        else if (isAvailable)
        {
            int shards = DailyRewardSystem.Instance.GetRolledShardAmount();
            int energy = DailyRewardSystem.Instance.GetRolledEnergyAmount();

            if (shards > 0)
            {
                infoText.text = $"Claim: {shards} Shards + {energy} Energy";
            }
            else
            {
                infoText.text = $"Claim: {energy} Energy";
            }
        }
        else
        {
            infoText.text = "Complete all Daily Quests";
        }
    }

    // ========================================
    // BUTTON CALLBACK
    // ========================================

    /// <summary>
    /// Called when claim button clicked
    /// </summary>
    void OnClaimButtonClicked()
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

        Log("✓ Claim button clicked!");

        // Get reward amounts untuk popup
        int shardAmount = DailyRewardSystem.Instance.GetRolledShardAmount();
        int energyAmount = DailyRewardSystem.Instance.GetRolledEnergyAmount();

        // Show popup dengan bundle items
        ShowClaimPopup(shardAmount, energyAmount);
    }

    /// <summary>
    /// Show popup untuk claim reward
    /// </summary>
    void ShowClaimPopup(int shardAmount, int energyAmount)
    {
        if (PopupClaimQuest.Instance == null)
        {
            LogError("PopupClaimQuest.Instance is null! Claiming directly.");
            DailyRewardSystem.Instance.ClaimReward();
            return;
        }

        // Build bundle items list
        var bundleItems = new System.Collections.Generic.List<BundleItemData>();

        // Add shard (if any)
        if (shardAmount > 0)
        {
            Sprite shardSprite = GetShardIcon();
            if (shardSprite != null)
            {
                bundleItems.Add(new BundleItemData(shardSprite, shardAmount, "Blue Shard"));
            }
        }

        // Add energy
        Sprite energySprite = GetEnergyIcon();
        if (energySprite != null)
        {
            bundleItems.Add(new BundleItemData(energySprite, energyAmount, "Energy"));
        }

        string title = "Daily Quest Reward";
        string description = "Congratulations! You completed all daily quests!";

        Log($"Opening popup: {bundleItems.Count} items (Shard: {shardAmount}, Energy: {energyAmount})");

        // Open popup
        PopupClaimQuest.Instance.OpenBundle(
            bundleItems,
            title,
            description,
            () => {
                // Callback saat confirm
                Log("Popup confirmed! Claiming reward...");
                DailyRewardSystem.Instance.ClaimReward();
            }
        );
    }

    /// <summary>
    /// Get shard icon sprite
    /// </summary>
    Sprite GetShardIcon()
    {
        // Try get from ShopManager
        var shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null && shopManager.iconShard != null)
        {
            return shopManager.iconShard;
        }

        // Try get from assigned icon
        if (shardIcon != null && shardIcon.sprite != null)
        {
            return shardIcon.sprite;
        }

        LogWarning("Shard icon not found!");
        return null;
    }

    /// <summary>
    /// Get energy icon sprite
    /// </summary>
    Sprite GetEnergyIcon()
    {
        // Try get from ShopManager
        var shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null && shopManager.iconEnergy != null)
        {
            return shopManager.iconEnergy;
        }

        // Try get from assigned icon
        if (energyIcon != null && energyIcon.sprite != null)
        {
            return energyIcon.sprite;
        }

        LogWarning("Energy icon not found!");
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

    [ContextMenu("Debug: Test Claim Button")]
    void Context_TestClaimButton()
    {
        OnClaimButtonClicked();
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== DAILY REWARD UI STATUS ===");
        Debug.Log($"claimButton: {(claimButton != null ? "OK" : "NULL")}");
        Debug.Log($"buttonImage: {(buttonImage != null ? "OK" : "NULL")}");
        Debug.Log($"buttonSpriteAvailable: {(buttonSpriteAvailable != null ? buttonSpriteAvailable.name : "NULL")}");
        Debug.Log($"buttonSpriteLocked: {(buttonSpriteLocked != null ? buttonSpriteLocked.name : "NULL")}");
        Debug.Log($"infoText: {(infoText != null ? "OK" : "NULL")}");
        Debug.Log($"shardIcon: {(shardIcon != null ? "OK" : "NULL")}");
        Debug.Log($"energyIcon: {(energyIcon != null ? "OK" : "NULL")}");

        if (DailyRewardSystem.Instance != null)
        {
            Debug.Log($"\nReward Status:");
            Debug.Log($"  Available: {DailyRewardSystem.Instance.IsRewardAvailable()}");
            Debug.Log($"  Claimed: {DailyRewardSystem.Instance.IsRewardClaimed()}");
            Debug.Log($"  Shard Amount: {DailyRewardSystem.Instance.GetRolledShardAmount()}");
            Debug.Log($"  Energy Amount: {DailyRewardSystem.Instance.GetRolledEnergyAmount()}");
        }
        Debug.Log("==============================");
    }
}