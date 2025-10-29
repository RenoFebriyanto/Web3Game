using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;              // "Bermain 5 menit"
    public Image iconImage;                 // Icon reward di dalam BorderIcon
    public TMP_Text amountText;             // Jumlah reward (5, 10, etc)
    public Button claimButton;              // Button Claim
    public Slider progressSlider;           // Progress bar

    [Header("Claim Button Sprites (3 states)")]
    public Sprite claimButtonGray;          // belum selesai
    public Sprite claimButtonBlue;          // ready to claim
    public Sprite claimButtonYellow;        // already claimed

    [Header("Optional overlays")]
    public GameObject readyIndicator;
    public GameObject claimedOverlay;

    [Header("Auto Refresh Settings")]
    public float autoRefreshInterval = 0.5f; // Refresh setiap 0.5 detik

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

        // Title dengan auto prefix [Daily] atau [Weekly]
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
            progressSlider.interactable = false; // PENTING: Disable interaction!
        }

        Refresh(progressModel);

        // Hook button
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }
    }

    void Update()
    {
        // Auto-refresh untuk sync dengan QuestManager
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshFromManager();
        }
    }

    /// <summary>
    /// Refresh dari QuestManager (untuk sync real-time)
    /// </summary>
    void RefreshFromManager()
    {
        if (manager == null || questData == null) return;

        var latestModel = manager.GetProgress(questData.questId);
        if (latestModel != null)
        {
            // Update hanya jika ada perubahan
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

        // Update progress slider
        if (progressSlider != null)
        {
            progressSlider.value = cur;
            progressSlider.interactable = false; // Ensure tetap disabled
        }

        bool isComplete = cur >= req && !model.claimed;
        bool isClaimed = model.claimed;

        // Button state & sprite
        if (claimButton != null)
        {
            Image buttonImage = claimButton.GetComponent<Image>();

            if (isClaimed)
            {
                // Already claimed - yellow sprite
                if (buttonImage != null && claimButtonYellow != null)
                {
                    buttonImage.sprite = claimButtonYellow;
                }
                claimButton.interactable = false;
            }
            else if (isComplete)
            {
                // Ready to claim - blue sprite
                if (buttonImage != null && claimButtonBlue != null)
                {
                    buttonImage.sprite = claimButtonBlue;
                }
                claimButton.interactable = true;
            }
            else
            {
                // Not complete yet - gray sprite
                if (buttonImage != null && claimButtonGray != null)
                {
                    buttonImage.sprite = claimButtonGray;
                }
                claimButton.interactable = false;
            }
        }

        // Optional indicators
        if (readyIndicator != null) readyIndicator.SetActive(isComplete && !isClaimed);
        if (claimedOverlay != null) claimedOverlay.SetActive(isClaimed);
    }

    void OnClaimClicked()
    {
        if (questData == null || model == null) return;
        if (model.claimed) return;
        if (model.progress < questData.requiredAmount) return;

        // Get reward display name
        string rewardDisplayName = GetRewardDisplayName();
        Sprite rewardIcon = GetRewardIcon();

        // Open popup dengan nama item yang benar
        if (PopupClaimQuest.Instance != null)
        {
            PopupClaimQuest.Instance.Open(
                rewardIcon,
                GetRewardAmountText(),
                rewardDisplayName, // Hanya nama item, tanpa [Daily]/[Weekly]
                () => {
                    // Callback saat confirm di popup
                    manager?.ClaimQuest(questData.questId);

                    // Notify DailyQuestDisplayLevel jika ada
                    NotifyLevelDisplay();
                }
            );
        }
        else
        {
            // Fallback jika popup tidak ada
            manager?.ClaimQuest(questData.questId);
            NotifyLevelDisplay();
        }
    }

    /// <summary>
    /// Notify DailyQuestDisplayLevel untuk refresh (jika ada)
    /// </summary>
    void NotifyLevelDisplay()
    {
        var levelDisplay = FindFirstObjectByType<DailyQuestDisplayLevel>();

        if (levelDisplay != null)
        {
            levelDisplay.OnQuestClaimed();
        }
    }

    /// <summary>
    /// Dapatkan display name untuk reward (tanpa prefix Daily/Weekly)
    /// </summary>
    string GetRewardDisplayName()
    {
        switch (questData.rewardType)
        {
            case QuestRewardType.Coin:
                return "Coins";

            case QuestRewardType.Shard:
                return "Blue Shard";

            case QuestRewardType.Energy:
                return "Energy";

            case QuestRewardType.Booster:
                // Map booster ID ke display name
                return GetBoosterDisplayName(questData.rewardBoosterId);

            default:
                return "Reward";
        }
    }

    /// <summary>
    /// Map booster itemId ke display name yang user-friendly
    /// </summary>
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
                // Fallback: capitalize first letter
                if (id.Length > 0)
                {
                    return char.ToUpper(id[0]) + id.Substring(1) + " Booster";
                }
                return "Booster";
        }
    }

    /// <summary>
    /// Get reward amount text (e.g. "x5", "1000", etc)
    /// </summary>
    string GetRewardAmountText()
    {
        if (questData.rewardAmount <= 0) return "";

        // Untuk booster, tampilkan "x5" style
        if (questData.rewardType == QuestRewardType.Booster)
        {
            return $"x{questData.rewardAmount}";
        }

        // Untuk currency, tampilkan angka dengan format
        return questData.rewardAmount.ToString("N0");
    }

    /// <summary>
    /// Get reward icon (fallback ke quest icon jika tidak ada)
    /// </summary>
    Sprite GetRewardIcon()
    {
        // Gunakan icon dari quest data
        return questData.icon;
    }

    void OnDestroy()
    {
        // Cleanup
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
        }
    }
}