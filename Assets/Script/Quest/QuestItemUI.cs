using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FIXED: QuestItemUI dengan icon dan display name yang benar
/// </summary>
public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_Text titleText;
    public Image iconImage;
    public TMP_Text amountText;
    public Button claimButton;
    public Slider progressSlider;

    [Header("Claim Button Sprites")]
    public Sprite claimButtonGray;
    public Sprite claimButtonBlue;
    public Sprite claimButtonYellow;

    [Header("Optional overlays")]
    public GameObject readyIndicator;
    public GameObject claimedOverlay;

    [Header("Auto Refresh Settings")]
    public float autoRefreshInterval = 0.5f;

    [HideInInspector] public QuestData questData;
    QuestProgressModel model;
    QuestManager manager;
    float lastRefreshTime = 0f;

    public void Setup(QuestData data, QuestProgressModel progressModel, QuestManager mgr)
    {
        questData = data;
        model = progressModel;
        manager = mgr;

        if (data == null) return;

        // Title dengan prefix
        if (titleText != null)
        {
            string prefix = data.isDaily ? "[Daily]" : "[Weekly]";
            titleText.text = $"{prefix} {data.title}";
        }

        // Icon reward
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
        }

        // Amount reward
        if (amountText != null)
        {
            amountText.text = data.rewardAmount > 0 ? data.rewardAmount.ToString() : "";
        }

        // Setup progress slider
        if (progressSlider != null)
        {
            progressSlider.maxValue = data.requiredAmount;
            progressSlider.minValue = 0;
            progressSlider.interactable = false;
        }

        Refresh(progressModel);

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }
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

        int cur = model.progress;
        int req = questData.requiredAmount;

        if (progressSlider != null)
        {
            progressSlider.value = cur;
            progressSlider.interactable = false;
        }

        bool isComplete = cur >= req && !model.claimed;
        bool isClaimed = model.claimed;

        if (claimButton != null)
        {
            Image buttonImage = claimButton.GetComponent<Image>();

            if (isClaimed)
            {
                if (buttonImage != null && claimButtonYellow != null)
                {
                    buttonImage.sprite = claimButtonYellow;
                }
                claimButton.interactable = false;
            }
            else if (isComplete)
            {
                if (buttonImage != null && claimButtonBlue != null)
                {
                    buttonImage.sprite = claimButtonBlue;
                }
                claimButton.interactable = true;
            }
            else
            {
                if (buttonImage != null && claimButtonGray != null)
                {
                    buttonImage.sprite = claimButtonGray;
                }
                claimButton.interactable = false;
            }
        }

        if (readyIndicator != null) readyIndicator.SetActive(isComplete && !isClaimed);
        if (claimedOverlay != null) claimedOverlay.SetActive(isClaimed);
    }

    void OnClaimClicked()
    {
        if (questData == null || model == null) return;
        if (model.claimed) return;
        if (model.progress < questData.requiredAmount) return;

        // === FIXED: Get correct display name ===
        string rewardDisplayName = GetRewardDisplayName();

        // === FIXED: Get correct icon ===
        Sprite rewardIcon = GetRewardIcon();

        // Debug check
        if (rewardIcon == null)
        {
            Debug.LogError($"[QuestItemUI] Icon is NULL for quest {questData.questId}!");
        }
        else
        {
            Debug.Log($"[QuestItemUI] Opening popup with icon: {rewardIcon.name}, name: {rewardDisplayName}");
        }

        if (PopupClaimQuest.Instance != null)
        {
            PopupClaimQuest.Instance.Open(
                rewardIcon,
                GetRewardAmountText(),
                rewardDisplayName, // Nama item tanpa prefix
                () => {
                    manager?.ClaimQuest(questData.questId);
                    NotifyLevelDisplay();
                }
            );
        }
        else
        {
            manager?.ClaimQuest(questData.questId);
            NotifyLevelDisplay();
        }
    }

    void NotifyLevelDisplay()
    {
        var levelDisplay = FindFirstObjectByType<DailyQuestDisplayLevel>();
        if (levelDisplay != null)
        {
            levelDisplay.OnQuestClaimed();
        }
    }

    // ========================================
    // FIXED: Reward Display Helpers
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
            case "coin2x":
                return "Coin 2x Booster";
            case "magnet":
                return "Magnet Booster";
            case "shield":
                return "Shield Booster";
            case "speedboost":
            case "rocketboost":
                return "Speed Boost";
            case "timefreeze":
                return "Time Freeze";
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
        // FIXED: Gunakan icon dari questData
        if (questData != null && questData.icon != null)
        {
            return questData.icon;
        }

        Debug.LogWarning($"[QuestItemUI] Quest {questData?.questId} has no icon!");
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