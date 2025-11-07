using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ✅ FIXED: QuestItemCompact dengan popup claim & full sinkronisasi
/// - Menampilkan popup saat claim (sama seperti QuestItemUI)
/// - Auto-sync dengan QuestP via event system
/// - Structure sesuai hierarchy: Desk, IconImage, ProgessBar, ClaimButton, ClaimedOverlay
/// </summary>
public class QuestItemCompact : MonoBehaviour
{
    [Header("=== UI COMPONENTS (Sesuai Hierarchy) ===")]
    [Tooltip("Text component untuk judul quest (child: Desk)")]
    public TMP_Text deskText;

    [Tooltip("Image component untuk icon quest (child: IconImage)")]
    public Image iconImage;

    [Tooltip("Slider component untuk progress bar (child: ProgessBar)")]
    public Slider progessBar; // typo sesuai hierarchy: "Progess"

    [Tooltip("Button component untuk claim (child: ClaimButton)")]
    public Button claimButton;

    [Tooltip("GameObject overlay ketika sudah diclaim (child: ClaimedOverlay)")]
    public GameObject claimedOverlay;

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
    public bool enableDebugLogs = false;

    // Runtime data
    [HideInInspector] public QuestData questData;
    private QuestProgressModel model;
    private QuestManager manager;
    private float lastRefreshTime = 0f;

    /// <summary>
    /// Setup komponen dengan data quest
    /// Dipanggil dari DailyQuestDisplayLevel saat spawn
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

        // ✅ Set title (tanpa prefix untuk compact version)
        if (deskText != null)
        {
            deskText.text = data.title;
            Log($"✓ Set title: {data.title}");
        }
        else
        {
            LogWarning("deskText is NULL!");
        }

        // ✅ Set icon
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
            Log($"✓ Set icon: {data.icon.name}");
        }
        else
        {
            LogWarning($"iconImage={(iconImage != null)}, icon={(data.icon != null)}");
        }

        // ✅ Setup progress bar (slider)
        if (progessBar != null)
        {
            progessBar.minValue = 0;
            progessBar.maxValue = data.requiredAmount;
            progessBar.value = 0;
            progessBar.interactable = false; // Non-interactable (read-only)
            Log("✓ Setup progress bar");
        }
        else
        {
            LogWarning("progessBar is NULL!");
        }

        // ✅ Setup button callback
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
            Log("✓ Setup claim button callback");
        }
        else
        {
            LogError("claimButton is NULL!");
        }

        // ✅ Initial refresh
        Refresh(progressModel);

        Log($"✓✓✓ Setup complete for quest: {data.questId}");
    }

    void Update()
    {
        // ✅ Auto-refresh untuk sinkronisasi real-time dengan QuestManager
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshFromManager();
        }
    }

    /// <summary>
    /// ✅ Refresh data dari QuestManager (sinkronisasi dengan QuestP)
    /// </summary>
    void RefreshFromManager()
    {
        if (manager == null || questData == null) return;

        var latestModel = manager.GetProgress(questData.questId);
        if (latestModel != null)
        {
            // Check if data changed
            if (model == null ||
                model.progress != latestModel.progress ||
                model.claimed != latestModel.claimed)
            {
                Refresh(latestModel);
            }
        }
    }

    /// <summary>
    /// ✅ Update UI berdasarkan progress model
    /// </summary>
    public void Refresh(QuestProgressModel progressModel)
    {
        model = progressModel;
        if (questData == null || model == null) return;

        int current = model.progress;
        int required = questData.requiredAmount;
        bool isComplete = current >= required && !model.claimed;
        bool isClaimed = model.claimed;

        Log($"Refresh: {questData.questId} - Progress: {current}/{required}, Complete: {isComplete}, Claimed: {isClaimed}");

        // ✅ Update progress bar
        if (progessBar != null)
        {
            progessBar.value = current;
            progessBar.interactable = false; // Always non-interactable
        }

        // ✅ Update button state
        UpdateButtonState(isComplete, isClaimed);

        // ✅ Update claimed overlay
        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(isClaimed);
        }
    }

    /// <summary>
    /// ✅ Update button sprite dan interactability
    /// </summary>
    void UpdateButtonState(bool isComplete, bool isClaimed)
    {
        if (claimButton == null) return;

        Image buttonImage = claimButton.GetComponent<Image>();

        if (isClaimed)
        {
            // ✅ State: Claimed (yellow/green)
            if (buttonImage != null && claimButtonClaimed != null)
            {
                buttonImage.sprite = claimButtonClaimed;
            }
            claimButton.interactable = false;
            Log("Button state: CLAIMED");
        }
        else if (isComplete)
        {
            // ✅ State: Ready to claim (blue)
            if (buttonImage != null && claimButtonReady != null)
            {
                buttonImage.sprite = claimButtonReady;
            }
            claimButton.interactable = true;
            Log("Button state: READY");
        }
        else
        {
            // ✅ State: Not ready (gray)
            if (buttonImage != null && claimButtonDisabled != null)
            {
                buttonImage.sprite = claimButtonDisabled;
            }
            claimButton.interactable = false;
            Log("Button state: DISABLED");
        }
    }

    /// <summary>
    /// ✅✅✅ FIXED: Called when claim button clicked
    /// Menampilkan popup SAMA seperti QuestItemUI di QuestP
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
            Log("Quest already claimed, ignoring click");
            return;
        }

        if (model.progress < questData.requiredAmount)
        {
            Log("Quest not complete, ignoring click");
            return;
        }

        Log($"✓ Claim button clicked for quest: {questData.questId}");

        // ✅ Get reward info untuk popup
        Sprite rewardIcon = GetRewardIcon();
        string amountText = GetRewardAmountText();
        string displayName = GetRewardDisplayName();

        // ✅ Validate icon
        if (rewardIcon == null)
        {
            LogError($"Reward icon is NULL for quest {questData.questId}!");
        }

        // ✅✅✅ Show popup (SAMA seperti QuestItemUI)
        if (PopupClaimQuest.Instance != null)
        {
            Log($"Opening popup: icon={rewardIcon?.name}, amount={amountText}, name={displayName}");

            PopupClaimQuest.Instance.Open(
                rewardIcon,
                amountText,
                displayName,
                () => {
                    // ✅ Callback saat player confirm di popup
                    Log($"Popup confirmed! Claiming quest: {questData.questId}");

                    // ✅ Claim quest via QuestManager (ini akan trigger event)
                    if (manager != null)
                    {
                        manager.ClaimQuest(questData.questId);
                        Log($"✓✓✓ Quest claimed via QuestManager - Event broadcast!");
                    }
                    else
                    {
                        LogError("QuestManager is NULL!");
                    }

                    // ✅ Play success sound
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseSuccess();
                    }

                    // ✅ Force refresh untuk update UI immediately
                    RefreshFromManager();

                    Log($"✓✓✓ Claim complete! Auto-synced to QuestP via events.");
                }
            );
        }
        else
        {
            LogError("PopupClaimQuest.Instance is NULL! Claiming directly without popup.");

            // ✅ Fallback: claim directly (tanpa popup)
            if (manager != null)
            {
                manager.ClaimQuest(questData.questId);
            }

            RefreshFromManager();
        }
    }

    // ========================================
    // ✅ REWARD DISPLAY HELPERS (Sama seperti QuestItemUI)
    // ========================================

    Sprite GetRewardIcon()
    {
        // ✅ Gunakan icon dari questData
        if (questData != null && questData.icon != null)
        {
            return questData.icon;
        }

        LogWarning($"Quest {questData?.questId} has no icon!");
        return null;
    }

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

    void LogWarning(string message)
    {
        Debug.LogWarning($"[QuestItemCompact:{questData?.questId}] ⚠️ {message}");
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
    // PUBLIC API (untuk external refresh)
    // ========================================

    /// <summary>
    /// Force refresh dari external (called dari DailyQuestDisplayLevel)
    /// </summary>
    public void ForceRefresh()
    {
        RefreshFromManager();
    }
}