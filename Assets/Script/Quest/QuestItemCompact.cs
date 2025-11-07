using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// COMPACT version of QuestItemUI untuk display di Level Panel
/// Ukuran lebih kecil, less details, quick claim support
/// </summary>
public class QuestItemCompact : MonoBehaviour
{
    [Header("UI Components (Compact)")]
    public TMP_Text titleText;
    public Image iconImage;
    public Image progressFillImage;     // Fill image untuk progress bar
    public TMP_Text progressText;       // "2/5" format
    public Button claimButton;
    public GameObject claimedOverlay;

    [Header("Button Sprites")]
    public Sprite claimButtonReady;     // Blue/active
    public Sprite claimButtonDisabled;  // Gray
    public Sprite claimButtonClaimed;   // Yellow/green

    [Header("Settings")]
    public float autoRefreshInterval = 0.5f;

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
            Debug.LogWarning("[QuestItemCompact] data is NULL!");
            return;
        }

        // Set title (compact - no prefix)
        if (titleText != null)
        {
            titleText.text = data.title;
        }

        // Set icon
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
        }

        // Initial refresh
        Refresh(progressModel);

        // Setup button callback
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }
    }

    void Update()
    {
        // Auto-refresh untuk sinkronisasi dengan QuestManager
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshFromManager();
        }
    }

    /// <summary>
    /// Refresh data dari QuestManager (untuk sinkronisasi real-time)
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

        // Update progress fill (0-1)
        if (progressFillImage != null)
        {
            float fillAmount = required > 0 ? (float)current / required : 0f;
            progressFillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }

        // Update progress text "2/5"
        if (progressText != null)
        {
            progressText.text = $"{current}/{required}";
        }

        // Update button state
        UpdateButtonState(isComplete, isClaimed);

        // Update claimed overlay
        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(isClaimed);
        }
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
            // Claimed state
            if (buttonImage != null && claimButtonClaimed != null)
            {
                buttonImage.sprite = claimButtonClaimed;
            }
            claimButton.interactable = false;
        }
        else if (isComplete)
        {
            // Ready to claim
            if (buttonImage != null && claimButtonReady != null)
            {
                buttonImage.sprite = claimButtonReady;
            }
            claimButton.interactable = true;
        }
        else
        {
            // Not ready
            if (buttonImage != null && claimButtonDisabled != null)
            {
                buttonImage.sprite = claimButtonDisabled;
            }
            claimButton.interactable = false;
        }
    }

    /// <summary>
    /// Called when claim button clicked
    /// </summary>
    void OnClaimClicked()
    {
        if (questData == null || model == null) return;
        if (model.claimed) return;
        if (model.progress < questData.requiredAmount) return;

        // Get reward info
        string rewardDisplayName = GetRewardDisplayName();
        Sprite rewardIcon = GetRewardIcon();
        string amountText = GetRewardAmountText();

        // Show popup
        if (PopupClaimQuest.Instance != null)
        {
            PopupClaimQuest.Instance.Open(
                rewardIcon,
                amountText,
                rewardDisplayName,
                () => {
                    // Claim quest
                    manager?.ClaimQuest(questData.questId);

                    // Notify QuestP panel untuk refresh
                    NotifyQuestPanelRefresh();

                    // Play sound
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseSuccess();
                    }
                }
            );
        }
        else
        {
            // Fallback: claim directly
            manager?.ClaimQuest(questData.questId);
            NotifyQuestPanelRefresh();
        }
    }

    /// <summary>
    /// Notify QuestP panel to refresh (untuk sinkronisasi)
    /// </summary>
    void NotifyQuestPanelRefresh()
    {
        // QuestItemUI akan auto-refresh via Update loop
        // Tidak perlu manual notify karena sudah ada auto-refresh system
        Debug.Log($"[QuestItemCompact] Quest {questData.questId} claimed - QuestP will auto-sync");
    }

    // ========================================
    // REWARD DISPLAY HELPERS
    // ========================================

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

    string GetRewardAmountText()
    {
        if (questData.rewardAmount <= 0) return "";

        if (questData.rewardType == QuestRewardType.Booster)
        {
            return $"x{questData.rewardAmount}";
        }

        return questData.rewardAmount.ToString("N0");
    }

    Sprite GetRewardIcon()
    {
        if (questData != null && questData.icon != null)
        {
            return questData.icon;
        }
        return null;
    }

    void OnDestroy()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
        }
    }
}