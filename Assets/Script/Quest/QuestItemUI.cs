using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;        // Desk
    public Image iconImage;           // Icon
    public TMP_Text amountText;       // Amount (reward amount display)
    public Slider progressSlider;     // Progress slider (non-interactable)
    public Image sliderFillImage;     // Fill image to tint
    public Image sliderBackgroundImage; // background image (empty color)
    public Button actionButton;       // Claim/Go button
    public TMP_Text actionButtonText;
    public Image actionButtonImage;   // optional image for button change

    [Header("Button sprites (optional)")]
    public Sprite spriteLocked;
    public Sprite spriteReady;
    public Sprite spriteClaimed;

    [Header("PopUp Claim Quest")]
    public PopupClaimQuest popupClaimQuest; // assign in inspector or leave null to auto-find

    [HideInInspector] public QuestData questData;
    QuestProgressModel model;
    QuestManager manager;

    public void Setup(QuestData q, QuestProgressModel m, QuestManager mgr)
    {
        questData = q;
        model = m;
        manager = mgr;

        // text(s)
        if (titleText != null) titleText.text = q.title;
        if (iconImage != null) iconImage.sprite = q.icon;
        if (amountText != null) amountText.text = q.rewardAmount.ToString();

        // slider
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = Mathf.Max(1, q.requiredAmount);
            progressSlider.interactable = false; // make it non-interactable so player can't drag
        }

        // button
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButton);
        }

        // try to auto-find popup if not set
        if (popupClaimQuest == null)
        {
            popupClaimQuest = PopupClaimQuest.Instance;
        }

        Refresh(m);
    }

    public void Refresh(QuestProgressModel m)
    {
        model = m;
        if (questData == null) return;
        // update slider
        if (progressSlider != null)
        {
            progressSlider.maxValue = Mathf.Max(1, questData.requiredAmount);
            progressSlider.value = Mathf.Clamp(model.progress, 0, questData.requiredAmount);
        }

        // decide button state
        if (model.claimed)
        {
            if (actionButtonText != null) actionButtonText.text = "Claimed";
            if (actionButtonImage != null && spriteClaimed != null) actionButtonImage.sprite = spriteClaimed;
            actionButton.interactable = false;
            if (sliderFillImage != null) sliderFillImage.color = Color.yellow;
        }
        else if (model.progress >= questData.requiredAmount)
        {
            // ready to claim
            if (actionButtonText != null) actionButtonText.text = "Claim";
            if (actionButtonImage != null && spriteReady != null) actionButtonImage.sprite = spriteReady;
            actionButton.interactable = true;
            if (sliderFillImage != null) sliderFillImage.color = Color.cyan;
        }
        else
        {
            // not ready
            if (actionButtonText != null) actionButtonText.text = "Go";
            if (actionButtonImage != null && spriteLocked != null) actionButtonImage.sprite = spriteLocked;
            actionButton.interactable = false;
            if (sliderFillImage != null) sliderFillImage.color = Color.white;
        }
    }

    void OnActionButton()
    {
        if (manager == null || questData == null || model == null) return;
        if (model.claimed) return;

        if (model.progress >= questData.requiredAmount)
        {
            // ready -> open confirmation popup instead of immediate claim
            OpenConfirmPopup();
        }
        else
        {
            // Go — implement navigation to relevant screen if needed
            Debug.Log($"[QuestItemUI] Go pressed for {questData.questId}");
        }
    }

    void OpenConfirmPopup()
    {
        // ensure popup exists
        var popup = popupClaimQuest ?? PopupClaimQuest.Instance;
        if (popup == null)
        {
            Debug.LogWarning("[QuestItemUI] PopupClaimQuest not found, claiming directly.");
            manager.ClaimQuest(questData.questId);
            return;
        }

        // build reward text to show
        string rewardText;
        switch (questData.rewardType)
        {
            case QuestRewardType.Coin:
                rewardText = questData.rewardAmount.ToString("N0") + " Coins";
                break;
            case QuestRewardType.Shard:
                rewardText = questData.rewardAmount.ToString("N0") + " Shards";
                break;
            case QuestRewardType.Energy:
                rewardText = questData.rewardAmount.ToString("N0") + " Energy";
                break;
            case QuestRewardType.Booster:
                rewardText = $"{questData.rewardAmount}x {questData.rewardBoosterId}";
                break;
            default:
                rewardText = questData.rewardAmount.ToString();
                break;
        }

        // open popup; confirm callback will actually perform claim via manager
        popup.Open(
            questData.icon,
            rewardText,
            questData.title,
            () =>
            {
                // confirm callback
                manager.ClaimQuest(questData.questId);

                // after claim, UI will be refreshed by QuestManager.UpdateUIForQuest call inside ClaimQuest.
                // But to be safe, call Refresh with latest model:
                var m = manager.GetProgress(questData.questId);
                if (m != null) Refresh(m);
            }
        );
    }
}
