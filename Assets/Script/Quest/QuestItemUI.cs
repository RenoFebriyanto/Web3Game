// QuestItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;
    public Image borderIcon;
    public TMP_Text amountText;
    public Slider progressSlider;
    public Image fillImage;
    public Button claimButton;
    public Image claimButtonImage;
    public TMP_Text claimButtonText;

    [Header("Button Sprites")]
    public Sprite spriteGo;
    public Sprite spriteClaim;
    public Sprite spriteClaimed;

    QuestData data;
    QuestManager manager;

    public void Setup(QuestData questData, QuestProgressModel model, QuestManager mgr)
    {
        data = questData;
        manager = mgr;

        if (progressSlider != null) progressSlider.interactable = false;

        Refresh(questData, model, mgr);
    }

    public void Refresh(QuestData questData, QuestProgressModel model, QuestManager mgr)
    {
        data = questData;
        manager = mgr;

        if (data == null)
        {
            Debug.LogWarning("[QuestItemUI] Refresh called with null data");
            return;
        }

        // Title
        if (titleText != null) titleText.text = data.title ?? "";

        // Icon (optional)
        if (borderIcon != null && data.icon != null) borderIcon.sprite = data.icon;

        // Reward amount
        if (amountText != null)
        {
            switch (data.rewardType)
            {
                case QuestRewardType.Coin:
                    amountText.text = data.rewardAmount.ToString("N0");
                    break;
                case QuestRewardType.Shard:
                    amountText.text = data.rewardAmount.ToString("N0");
                    break;
                case QuestRewardType.Energy:
                    amountText.text = data.rewardAmount.ToString();
                    break;
                case QuestRewardType.Booster:
                    amountText.text = data.rewardAmount.ToString();
                    break;
                default:
                    amountText.text = "";
                    break;
            }
        }

        // Progress slider
        if (progressSlider != null)
        {
            progressSlider.maxValue = Mathf.Max(1, data.requiredAmount);
            progressSlider.value = model != null ? Mathf.Clamp(model.progress, 0, data.requiredAmount) : 0;

            Debug.Log($"[QuestItemUI] {data.questId} progress: {progressSlider.value}/{progressSlider.maxValue}");
        }

        // Fill color
        if (fillImage != null)
        {
            fillImage.color = new Color(0.2f, 0.6f, 1f, 1f); // Blue
        }

        // Button state
        UpdateButtonState(model);
    }

    void UpdateButtonState(QuestProgressModel model)
    {
        if (claimButton == null) return;

        // PENTING: Remove semua listeners sebelum add baru
        claimButton.onClick.RemoveAllListeners();

        bool complete = (model != null && model.progress >= data.requiredAmount);
        bool claimed = (model != null && model.claimed);

        if (claimed)
        {
            // Already claimed
            claimButton.interactable = false;
            if (claimButtonText != null) claimButtonText.text = "Claimed";
            if (claimButtonImage != null && spriteClaimed != null) claimButtonImage.sprite = spriteClaimed;

            Debug.Log($"[QuestItemUI] {data.questId} state: CLAIMED");
        }
        else if (complete)
        {
            // Ready to claim
            claimButton.interactable = true;
            if (claimButtonText != null) claimButtonText.text = "Claim";
            if (claimButtonImage != null && spriteClaim != null) claimButtonImage.sprite = spriteClaim;

            // Add fresh listener
            claimButton.onClick.AddListener(OnClaimClicked);

            Debug.Log($"[QuestItemUI] {data.questId} state: READY TO CLAIM (button interactive)");
        }
        else
        {
            // Not ready: show "Go"
            claimButton.interactable = true;
            if (claimButtonText != null) claimButtonText.text = "Go";
            if (claimButtonImage != null && spriteGo != null) claimButtonImage.sprite = spriteGo;

            claimButton.onClick.AddListener(OnGoClicked);

            Debug.Log($"[QuestItemUI] {data.questId} state: GO ({model?.progress ?? 0}/{data.requiredAmount})");
        }
    }

    void OnClaimClicked()
    {
        Debug.Log($"[QuestItemUI] OnClaimClicked for {data?.questId}");

        if (manager != null && data != null)
        {
            manager.ClaimReward(data.questId);
        }
        else
        {
            Debug.LogError($"[QuestItemUI] Cannot claim: manager={manager != null}, data={data != null}");
        }
    }

    void OnGoClicked()
    {
        Debug.Log($"[QuestItemUI] Go pressed for quest {data?.questId}");
        // Optional: navigate to gameplay or show hint
    }

    public void OnManagerUpdated()
    {
        if (manager != null && data != null)
        {
            var model = manager.GetProgressModel(data.questId);
            Refresh(data, model, manager);
        }
    }
}