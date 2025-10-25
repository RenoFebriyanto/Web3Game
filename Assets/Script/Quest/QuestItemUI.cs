using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quest item UI used both in Quest panel and Level quick view.
/// Handles display, non-interactable progress slider, claim button states,
/// and opens popup confirmation when player clicks Claim.
/// </summary>
public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;              // "Bermain 5 menit"
    public Image iconImage;                 // Icon reward di dalam BorderIcon
    public TMP_Text amountText;             // Jumlah reward (5, 10, etc)
    public Button claimButton;              // Button Claim / Go
    public TMP_Text claimButtonText;        // optional button text
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

        // Title with prefix
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

        // Setup progress slider (non-interactable)
        if (progressSlider != null)
        {
            progressSlider.maxValue = Mathf.Max(1, data.requiredAmount);
            progressSlider.minValue = 0;
            progressSlider.value = Mathf.Clamp(progressModel?.progress ?? 0, 0, data.requiredAmount);
            progressSlider.interactable = false; // disable player dragging
            // Also disable handle (if there is a handle Selectable)
            var handle = progressSlider.handleRect;
            if (handle != null)
            {
                var sel = handle.GetComponent<Selectable>();
                if (sel != null) sel.interactable = false;
            }
        }

        // Hook claim button
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        Refresh(progressModel);
    }

    void Update()
    {
        // Auto-refresh (keamanan untuk tampilan Level panel yang terpisah)
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
        int req = Mathf.Max(1, questData.requiredAmount);

        // progress slider
        if (progressSlider != null)
        {
            progressSlider.maxValue = req;
            progressSlider.value = Mathf.Clamp(cur, 0, req);
            progressSlider.interactable = false;
        }

        bool isComplete = cur >= req && !model.claimed;
        bool isClaimed = model.claimed;

        // Button image state
        if (claimButton != null)
        {
            Image buttonImage = claimButton.GetComponent<Image>();
            if (isClaimed)
            {
                if (buttonImage != null && claimButtonYellow != null) buttonImage.sprite = claimButtonYellow;
                claimButton.interactable = false;
                if (claimButtonText != null) claimButtonText.text = "Claimed";
            }
            else if (isComplete)
            {
                if (buttonImage != null && claimButtonBlue != null) buttonImage.sprite = claimButtonBlue;
                claimButton.interactable = true;
                if (claimButtonText != null) claimButtonText.text = "Claim";
            }
            else
            {
                if (buttonImage != null && claimButtonGray != null) buttonImage.sprite = claimButtonGray;
                claimButton.interactable = false;
                if (claimButtonText != null) claimButtonText.text = "Go";
            }
        }

        if (readyIndicator != null) readyIndicator.SetActive(isComplete && !isClaimed);
        if (claimedOverlay != null) claimedOverlay.SetActive(isClaimed);
    }

    void OnClaimClicked()
    {
        if (questData == null) return;
        // If not ready -> Go (you can implement navigation)
        var modelNow = manager?.GetProgress(questData.questId);
        if (modelNow == null) return;

        if (modelNow.claimed) return;
        if (modelNow.progress < questData.requiredAmount)
        {
            Debug.Log($"[QuestItemUI] Go pressed for {questData.questId}");
            return;
        }

        // Ready to claim -> show popup confirmation
        Sprite rewardIcon = questData.icon;
        string rewardDisplay = GetRewardDisplayName();
        string amountText = GetRewardAmountText();

        if (PopupClaimQuest.Instance != null)
        {
            PopupClaimQuest.Instance.Open(rewardIcon, amountText, rewardDisplay, () =>
            {
                // Confirm callback
                manager?.ClaimQuest(questData.questId);

                // If level quick panel exists, notify it to refresh
                var levelDisplay = FindObjectOfType<DailyQuestDisplayLevel>();
                if (levelDisplay != null) levelDisplay.OnQuestClaimed();

                // optional: notify chest controller
                var chest = FindObjectOfType<QuestChestController>();
                chest?.OnQuestClaimed(questData);
            });
        }
        else
        {
            // fallback: claim immediately
            manager?.ClaimQuest(questData.questId);
            var levelDisplay = FindObjectOfType<DailyQuestDisplayLevel>();
            if (levelDisplay != null) levelDisplay.OnQuestClaimed();
        }
    }

    string GetRewardDisplayName()
    {
        switch (questData.rewardType)
        {
            case QuestRewardType.Coin: return "Coins";
            case QuestRewardType.Shard: return "Shards";
            case QuestRewardType.Energy: return "Energy";
            case QuestRewardType.Booster:
                return !string.IsNullOrEmpty(questData.rewardBoosterId) ? questData.rewardBoosterId : "Booster";
            default: return "Reward";
        }
    }

    string GetRewardAmountText()
    {
        if (questData.rewardAmount <= 0) return "";
        if (questData.rewardType == QuestRewardType.Booster) return $"x{questData.rewardAmount}";
        return questData.rewardAmount.ToString("N0");
    }

    void OnDestroy()
    {
        if (claimButton != null) claimButton.onClick.RemoveAllListeners();
    }
}
