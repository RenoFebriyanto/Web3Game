using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ✅✅✅ FINAL FIX: QuestItemCompact dengan PROPER event trigger untuk crate progress
/// - Amount text update
/// - Popup claim
/// - Event trigger ke QuestChestController (CRITICAL FIX)
/// </summary>
public class QuestItemCompact : MonoBehaviour
{
    [Header("=== UI COMPONENTS (Sesuai Hierarchy) ===")]
    [Tooltip("Text component untuk judul quest (child: Desk)")]
    public TMP_Text deskText;

    [Tooltip("Image component untuk icon quest (child: IconImage)")]
    public Image iconImage;

    [Tooltip("Slider component untuk progress bar (child: ProgessBar)")]
    public Slider progessBar;

    [Tooltip("Button component untuk claim (child: ClaimButton)")]
    public Button claimButton;

    [Tooltip("GameObject overlay ketika sudah diclaim (child: ClaimedOverlay)")]
    public GameObject claimedOverlay;

    [Header("=== ✅ CRITICAL: AMOUNT TEXT ===")]
    [Tooltip("Text untuk menampilkan jumlah reward (child: Amount)")]
    public TMP_Text amountText;

    [Header("=== BUTTON SPRITES ===")]
    [Tooltip("Sprite untuk button disabled (gray)")]
    public Sprite claimButtonDisabled;

    [Tooltip("Sprite untuk button ready (blue)")]
    public Sprite claimButtonReady;

    [Tooltip("Sprite untuk button claimed (yellow)")]
    public Sprite claimButtonClaimed;

    [Header("=== AUTO REFRESH ===")]
    [Tooltip("Interval refresh untuk sinkronisasi (detik)")]
    public float autoRefreshInterval = 0.5f;

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = true;

    // Runtime data
    [HideInInspector] public QuestData questData;
    private QuestProgressModel model;
    private QuestManager manager;
    private float lastRefreshTime = 0f;

    public void Setup(QuestData data, QuestProgressModel progressModel, QuestManager mgr)
    {
        questData = data;
        model = progressModel;
        manager = mgr;

        if (data == null)
        {
            LogError("Setup failed: questData is NULL!");
            return;
        }

        Log($"=== SETUP START: {data.questId} ===");

        // Set title
        if (deskText != null)
        {
            deskText.text = data.title;
            Log($"✓ Title set: {data.title}");
        }

        // Set icon
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
            Log($"✓ Icon set: {data.icon.name}");
        }

        // Set amount text
        UpdateAmountText(data);

        // Setup progress bar
        if (progessBar != null)
        {
            progessBar.minValue = 0;
            progessBar.maxValue = data.requiredAmount;
            progessBar.value = 0;
            progessBar.interactable = false;
            Log($"✓ Progress bar setup: 0/{data.requiredAmount}");
        }

        // Setup button callback
        SetupClaimButton();

        // Initial refresh
        Refresh(progressModel);

        Log($"=== SETUP COMPLETE: {data.questId} ===");
    }

    void SetupClaimButton()
    {
        if (claimButton == null)
        {
            LogError("❌ claimButton is NULL! Cannot setup callback!");

            // Try auto-find
            claimButton = GetComponentInChildren<Button>();
            if (claimButton != null)
            {
                Log("✓ Auto-found claim button");
            }
            else
            {
                LogError("❌ Still cannot find claim button!");
                return;
            }
        }

        // Remove all listeners first
        claimButton.onClick.RemoveAllListeners();

        // Add listener
        claimButton.onClick.AddListener(OnClaimClicked);

        Log($"✓ Claim button callback setup for {questData?.questId}");
    }

    void UpdateAmountText(QuestData data)
    {
        if (amountText == null)
        {
            LogError("❌ amountText is NULL! Assign di Inspector!");
            return;
        }

        if (data == null)
        {
            LogError("❌ questData is NULL!");
            return;
        }

        string formattedAmount = "";

        if (data.rewardAmount > 0)
        {
            if (data.rewardType == QuestRewardType.Booster)
            {
                formattedAmount = $"x{data.rewardAmount}";
            }
            else
            {
                formattedAmount = data.rewardAmount.ToString("N0");
            }
        }

        amountText.text = formattedAmount;
        amountText.gameObject.SetActive(!string.IsNullOrEmpty(formattedAmount));

        Log($"✓ Amount text updated: '{formattedAmount}'");
    }

    void Update()
    {
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshFromManager();
        }
    }

    void RefreshFromManager()
    {
        if (manager == null || questData == null) return;

        var latestModel = manager.GetProgress(questData.questId);
        if (latestModel != null)
        {
            if (model == null ||
                model.progress != latestModel.progress ||
                model.claimed != latestModel.claimed)
            {
                Refresh(latestModel);
            }
        }
    }

    public void Refresh(QuestProgressModel progressModel)
    {
        model = progressModel;
        if (questData == null || model == null) return;

        int current = model.progress;
        int required = questData.requiredAmount;
        bool isComplete = current >= required && !model.claimed;
        bool isClaimed = model.claimed;

        Log($"Refresh: Progress={current}/{required}, Complete={isComplete}, Claimed={isClaimed}");

        if (progessBar != null)
        {
            progessBar.value = current;
        }

        UpdateButtonState(isComplete, isClaimed);

        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(isClaimed);
        }

        UpdateAmountText(questData);
    }

    void UpdateButtonState(bool isComplete, bool isClaimed)
    {
        if (claimButton == null) return;

        Image buttonImage = claimButton.GetComponent<Image>();

        if (isClaimed)
        {
            if (buttonImage != null && claimButtonClaimed != null)
            {
                buttonImage.sprite = claimButtonClaimed;
            }
            claimButton.interactable = false;
            Log("Button: CLAIMED");
        }
        else if (isComplete)
        {
            if (buttonImage != null && claimButtonReady != null)
            {
                buttonImage.sprite = claimButtonReady;
            }
            claimButton.interactable = true;
            Log("Button: READY");
        }
        else
        {
            if (buttonImage != null && claimButtonDisabled != null)
            {
                buttonImage.sprite = claimButtonDisabled;
            }
            claimButton.interactable = false;
            Log("Button: DISABLED");
        }
    }

    /// <summary>
    /// ✅✅✅ CRITICAL FIX: Proper claim flow dengan event trigger
    /// </summary>
    void OnClaimClicked()
    {
        if (questData == null || model == null)
        {
            LogError("OnClaimClicked failed: questData or model is NULL!");
            return;
        }

        if (model.claimed)
        {
            Log("Quest already claimed");
            return;
        }

        if (model.progress < questData.requiredAmount)
        {
            Log("Quest not complete yet");
            return;
        }

        Log($"✅✅✅ CLAIM BUTTON CLICKED: {questData.questId}");

        // Get reward info
        Sprite rewardIcon = questData.icon;
        string amountText = GetRewardAmountText();
        string displayName = GetRewardDisplayName();

        Log($"Reward info: icon={rewardIcon?.name}, amount={amountText}, name={displayName}");

        // Validate popup instance
        if (PopupClaimQuest.Instance == null)
        {
            LogError("❌ PopupClaimQuest.Instance is NULL! Cannot show popup!");
            
            // ✅ CRITICAL FIX: Fallback claim dengan PROPER event trigger
            if (manager != null)
            {
                Log("⚠️ Using fallback claim (no popup)");
                ClaimQuestDirect();
            }
            return;
        }

        if (rewardIcon == null)
        {
            LogError($"❌ Reward icon is NULL for {questData.questId}!");
        }

        Log("✅ Opening PopupClaimQuest...");

        // Show popup
        PopupClaimQuest.Instance.Open(
            rewardIcon,
            amountText,
            displayName,
            () => {
                Log($"✅ Popup confirmed! Claiming {questData.questId}...");
                ClaimQuestDirect();
                Log("✅✅✅ CLAIM COMPLETE!");
            }
        );

        Log("✅ Popup opened successfully!");
    }

    /// <summary>
    /// ✅✅✅ CRITICAL FIX: Direct claim dengan PROPER event trigger
    /// Ini yang MISSING di versi sebelumnya!
    /// </summary>
    void ClaimQuestDirect()
    {
        if (manager == null)
        {
            LogError("❌ QuestManager is NULL!");
            return;
        }

        if (questData == null)
        {
            LogError("❌ questData is NULL!");
            return;
        }

        Log($"✅✅✅ ClaimQuestDirect called for: {questData.questId}");

        // ✅✅✅ CRITICAL: Call QuestManager.ClaimQuest()
        // Ini akan:
        // 1. Mark quest as claimed
        // 2. Grant rewards
        // 3. ✅ TRIGGER OnQuestClaimed EVENT → QuestChestController akan update!
        manager.ClaimQuest(questData.questId);

        Log($"✅ Quest claimed via QuestManager - event triggered!");

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }

        // Force refresh UI
        RefreshFromManager();
    }

    // ========================================
    // REWARD DISPLAY HELPERS
    // ========================================

    string GetRewardAmountText()
    {
        if (questData.rewardAmount <= 0) return "";

        if (questData.rewardType == QuestRewardType.Booster)
        {
            return $"x{questData.rewardAmount}";
        }

        return questData.rewardAmount.ToString("N0");
    }

    string GetRewardDisplayName()
    {
        if (questData == null) return "Reward";

        switch (questData.rewardType)
        {
            case QuestRewardType.Coin:
                return "Coins";
            case QuestRewardType.Shard:
                return "Blue Shard";
            case QuestRewardType.Energy:
                return "Energy";
            case QuestRewardType.Booster:
                return GetBoosterDisplayName(questData.rewardBoosterId);
            default:
                return "Reward";
        }
    }

    string GetBoosterDisplayName(string boosterId)
    {
        if (string.IsNullOrEmpty(boosterId)) return "Booster";

        string id = boosterId.ToLower().Trim();
        switch (id)
        {
            case "coin2x": return "Coin 2x Booster";
            case "magnet": return "Magnet Booster";
            case "shield": return "Shield Booster";
            case "speedboost":
            case "rocketboost": return "Speed Boost";
            case "timefreeze": return "Time Freeze";
            default:
                if (id.Length > 0)
                    return char.ToUpper(id[0]) + id.Substring(1) + " Booster";
                return "Booster";
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[QuestItemCompact:{questData?.questId}] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[QuestItemCompact:{questData?.questId}] ❌ {message}");
    }

    // ========================================
    // CLEANUP
    // ========================================

    void OnDestroy()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
        }
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Debug: Force Claim Quest")]
    void Context_ForceClaimQuest()
    {
        if (questData == null)
        {
            Debug.LogError("questData is NULL!");
            return;
        }

        Debug.Log($"Force claiming quest: {questData.questId}");
        ClaimQuestDirect();
    }

    [ContextMenu("Debug: Print Component Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== QUEST ITEM COMPACT STATUS ===");
        Debug.Log($"Quest ID: {questData?.questId}");
        Debug.Log($"Quest Type: {(questData != null ? (questData.isDaily ? "DAILY" : "WEEKLY") : "NULL")}");
        Debug.Log($"manager: {(manager != null ? "OK" : "NULL")}");
        Debug.Log($"model: {(model != null ? $"progress={model.progress}, claimed={model.claimed}" : "NULL")}");
        Debug.Log($"claimButton: {(claimButton != null ? "OK" : "NULL")}");
        
        if (manager != null)
        {
            Debug.Log($"QuestManager.Instance: {(QuestManager.Instance != null ? "OK" : "NULL")}");
            if (QuestManager.Instance != null)
            {
                Debug.Log($"OnQuestClaimed event listener count: {QuestManager.Instance.OnQuestClaimed.GetPersistentEventCount()}");
            }
        }
        
        Debug.Log("================================");
    }
}