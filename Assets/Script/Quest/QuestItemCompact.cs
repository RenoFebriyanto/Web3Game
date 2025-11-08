using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ✅✅✅ FINAL FIX: QuestItemCompact dengan amount text & popup claim
/// - Amount text WAJIB update sesuai QuestData
/// - Popup claim WAJIB muncul (sama seperti QuestItemUI)
/// - Full sinkronisasi via event system
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

    /// <summary>
    /// Setup komponen dengan data quest
    /// </summary>
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

        // ✅ Set title
        if (deskText != null)
        {
            deskText.text = data.title;
            Log($"✓ Title set: {data.title}");
        }

        // ✅ Set icon
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
            Log($"✓ Icon set: {data.icon.name}");
        }

        // ✅✅✅ CRITICAL: Set amount text (WAJIB!)
        UpdateAmountText(data);

        // ✅ Setup progress bar
        if (progessBar != null)
        {
            progessBar.minValue = 0;
            progessBar.maxValue = data.requiredAmount;
            progessBar.value = 0;
            progessBar.interactable = false;
            Log($"✓ Progress bar setup: 0/{data.requiredAmount}");
        }

        // ✅✅✅ CRITICAL FIX: Setup button callback dengan FORCE
        SetupClaimButton();

        // ✅ Initial refresh
        Refresh(progressModel);

        Log($"=== SETUP COMPLETE: {data.questId} ===");
    }

    /// <summary>
    /// ✅ NEW: Setup claim button dengan validation lengkap
    /// </summary>
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

        // Debug: Check button state
        Log($"  Button interactable: {claimButton.interactable}");
        Log($"  Button gameObject active: {claimButton.gameObject.activeSelf}");
    }

    /// <summary>
    /// ✅✅✅ UPDATE AMOUNT TEXT (CRITICAL FIX)
    /// </summary>
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

        // Format amount text
        string formattedAmount = "";

        if (data.rewardAmount > 0)
        {
            if (data.rewardType == QuestRewardType.Booster)
            {
                // Booster: x3, x5, etc
                formattedAmount = $"x{data.rewardAmount}";
            }
            else
            {
                // Economy: 1,000 or 5,000
                formattedAmount = data.rewardAmount.ToString("N0");
            }
        }

        amountText.text = formattedAmount;
        amountText.gameObject.SetActive(!string.IsNullOrEmpty(formattedAmount));

        Log($"✓✓✓ Amount text updated: '{formattedAmount}' (type: {data.rewardType}, amount: {data.rewardAmount})");
    }

    void Update()
    {
        // Auto-refresh untuk sinkronisasi real-time
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshFromManager();
        }
    }

    /// <summary>
    /// Refresh data dari QuestManager
    /// </summary>
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

    /// <summary>
    /// Update UI berdasarkan progress model
    /// </summary>
    public void Refresh(QuestProgressModel progressModel)
    {
        model = progressModel;
        if (questData == null || model == null) return;

        int current = model.progress;
        int required = questData.requiredAmount;
        bool isComplete = current >= required && !model.claimed;
        bool isClaimed = model.claimed;

        Log($"Refresh: Progress={current}/{required}, Complete={isComplete}, Claimed={isClaimed}");

        // Update progress bar
        if (progessBar != null)
        {
            progessBar.value = current;
        }

        // Update button state
        UpdateButtonState(isComplete, isClaimed);

        // Update claimed overlay
        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(isClaimed);
        }

        // ✅ Re-update amount text (ensure always correct)
        UpdateAmountText(questData);
    }

    /// <summary>
    /// Update button sprite dan interactability
    /// </summary>
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
    /// ✅✅✅ CRITICAL: Called when claim button clicked
    /// WAJIB menampilkan popup (sama seperti QuestItemUI)
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

        Log($"✅ CLAIM BUTTON CLICKED: {questData.questId}");

        // ✅ Get reward info
        Sprite rewardIcon = questData.icon; // Gunakan icon dari questData
        string amountText = GetRewardAmountText();
        string displayName = GetRewardDisplayName();

        Log($"Reward info: icon={rewardIcon?.name}, amount={amountText}, name={displayName}");

        // ✅✅✅ CRITICAL: Validate popup instance
        if (PopupClaimQuest.Instance == null)
        {
            LogError("❌ PopupClaimQuest.Instance is NULL! Cannot show popup!");
            LogError("Make sure PopupClaimQuest GameObject exists in scene!");

            // Fallback: claim directly
            if (manager != null)
            {
                manager.ClaimQuest(questData.questId);
                Log("⚠️ Claimed directly without popup (fallback)");
            }
            return;
        }

        // ✅ Validate icon
        if (rewardIcon == null)
        {
            LogError($"❌ Reward icon is NULL for {questData.questId}!");
            LogError("Make sure QuestData has icon assigned in Inspector!");
        }

        Log("✅ Opening PopupClaimQuest...");

        // ✅✅✅ Show popup (SAMA seperti QuestItemUI)
        PopupClaimQuest.Instance.Open(
            rewardIcon,
            amountText,
            displayName,
            () => {
                // Callback saat confirm
                Log($"✅ Popup confirmed! Claiming {questData.questId}...");

                // Claim via QuestManager (trigger event)
                if (manager != null)
                {
                    manager.ClaimQuest(questData.questId);
                    Log("✅ Quest claimed via QuestManager");
                }
                else
                {
                    LogError("❌ QuestManager is NULL!");
                }

                // Play sound
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPurchaseSuccess();
                }

                // Force refresh
                RefreshFromManager();

                Log("✅✅✅ CLAIM COMPLETE!");
            }
        );

        Log("✅ Popup opened successfully!");
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

    [ContextMenu("Debug: Print Component Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== QUEST ITEM COMPACT STATUS ===");
        Debug.Log($"Quest ID: {questData?.questId}");
        Debug.Log($"deskText: {(deskText != null ? "OK" : "NULL")}");
        Debug.Log($"iconImage: {(iconImage != null ? "OK" : "NULL")}");
        Debug.Log($"amountText: {(amountText != null ? "OK ✓" : "NULL ❌")}");
        Debug.Log($"progessBar: {(progessBar != null ? "OK" : "NULL")}");
        Debug.Log($"claimButton: {(claimButton != null ? "OK" : "NULL")}");
        Debug.Log($"claimedOverlay: {(claimedOverlay != null ? "OK" : "NULL")}");

        if (questData != null)
        {
            Debug.Log($"\nQuest Data:");
            Debug.Log($"  Title: {questData.title}");
            Debug.Log($"  Reward Type: {questData.rewardType}");
            Debug.Log($"  Reward Amount: {questData.rewardAmount}");
            Debug.Log($"  Icon: {(questData.icon != null ? questData.icon.name : "NULL")}");
        }

        Debug.Log($"\nPopupClaimQuest.Instance: {(PopupClaimQuest.Instance != null ? "OK ✓" : "NULL ❌")}");
        Debug.Log("================================");
    }

    [ContextMenu("Debug: Force Update Amount Text")]
    void Context_ForceUpdateAmount()
    {
        if (questData != null)
        {
            UpdateAmountText(questData);
            Debug.Log("✓ Amount text force updated");
        }
        else
        {
            Debug.LogError("❌ Cannot update: questData is NULL");
        }
    }
}