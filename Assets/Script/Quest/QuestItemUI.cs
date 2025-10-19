using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;         // Desk (TextMeshPro)
    public Image iconImage;           // Icon (Image)
    public TMP_Text amountText;       // Amount (TextMeshPro) -> shows reward amount (not progress)
    public Button actionButton;       // Claim / Go
    public TMP_Text actionButtonText; // label on button
    public Image actionButtonImage;   // optional image for button visuals

    [Header("Progress bar")]
    public Slider progressSlider;     // slider (read-only visual)
    public Image sliderFillImage;     // fill image (for color)
    public Image sliderBackground;    // background image (empty color)

    [Header("Colors / Sprites")]
    public Color colorEmpty = new Color(0.9f, 0.9f, 0.9f); // greyish
    public Color colorFill = new Color(0.0f, 0.5f, 1.0f);  // blue
    public Sprite buttonLockedSprite;
    public Sprite buttonReadySprite;
    public Sprite buttonClaimedSprite;

    // runtime refs
    QuestData questDef;
    QuestProgressModel model;
    QuestManager manager;

    void Awake()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
    }

    /// <summary>
    /// Setup called by QuestManager after instantiating prefab.
    /// </summary>
    public void Setup(QuestData def, QuestProgressModel progModel, QuestManager qm)
    {
        questDef = def;
        model = progModel;
        manager = qm;
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (questDef == null || model == null) return;

        // Title & icon
        if (titleText != null) titleText.text = questDef.title ?? "";
        if (iconImage != null)
        {
            iconImage.sprite = questDef.icon;
            iconImage.enabled = questDef.icon != null;
        }

        // Reward amount text — per request: show reward amount (not progress "0/3")
        if (amountText != null)
        {
            string amt = "";
            switch (questDef.rewardType)
            {
                case QuestRewardType.Coin: amt = $"{questDef.rewardAmount}"; break;
                case QuestRewardType.Shard: amt = $"{questDef.rewardAmount}"; break;
                case QuestRewardType.Energy: amt = $"{questDef.rewardAmount}"; break;
                case QuestRewardType.Booster: amt = $"{questDef.rewardAmount}x"; break;
                default: amt = ""; break;
            }
            amountText.text = amt;
        }

        // Progress slider (fill / empty colors)
        if (progressSlider != null)
        {
            float t = 0f;
            if (questDef.requiredAmount > 0) t = Mathf.Clamp01((float)model.progress / questDef.requiredAmount);
            progressSlider.value = t;
            if (sliderFillImage != null) sliderFillImage.color = colorFill;
            if (sliderBackground != null) sliderBackground.color = colorEmpty;
        }

        // Button states
        bool isClaimed = model.claimed;
        bool isComplete = (questDef.requiredAmount <= 0) || (model.progress >= questDef.requiredAmount);

        if (actionButton != null)
        {
            actionButton.interactable = !isClaimed && isComplete;
            if (actionButtonImage != null)
            {
                if (isClaimed) actionButtonImage.sprite = buttonClaimedSprite;
                else if (isComplete) actionButtonImage.sprite = buttonReadySprite;
                else actionButtonImage.sprite = buttonLockedSprite;
            }

            if (actionButtonText != null)
            {
                if (isClaimed) actionButtonText.text = "Claimed";
                else if (isComplete) actionButtonText.text = "Claim";
                else actionButtonText.text = "Go"; // go = not ready yet
            }
        }
    }

    void OnActionButtonClicked()
    {
        if (questDef == null || manager == null) return;

        // If ready and not claimed -> claim
        if (!model.claimed && model.progress >= questDef.requiredAmount)
        {
            manager.ClaimReward(questDef.questId);
        }
        else
        {
            // Not ready: you can implement Go-to-level logic here (optional)
            Debug.Log($"[QuestItemUI] Quest not ready. questId={questDef.questId} progress={model.progress}/{questDef.requiredAmount}");
        }
    }

    /// <summary>
    /// Called by manager when progress/claimed changed.
    /// </summary>
    public void OnManagerUpdated()
    {
        // refresh model reference from manager (safe)
        if (manager != null && questDef != null)
        {
            var newModel = manager.GetProgressModel(questDef.questId);
            if (newModel != null) model = newModel;
        }
        RefreshVisuals();
    }
}
