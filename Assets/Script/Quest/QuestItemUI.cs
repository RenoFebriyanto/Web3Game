// QuestItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller untuk satu quest row item.
/// - Setup(data, model, manager) dipanggil oleh QuestManager saat populate.
/// - Refreshable oleh QuestManager ketika progress berubah.
/// </summary>
public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;
    public Image iconImage;          // optional icon (assign in prefab; you can set sprite per quest in inspector if needed)
    public TMP_Text amountText;      // show reward amount (e.g. "5000")
    public Slider progressSlider;    // non-interactable, shows progress visually
    public Image fillAreaImage;      // image used for fill color (assign slider.fillRect image)
    public Button actionButton;      // Go / Claim button
    public TMP_Text actionButtonText;
    public Image actionButtonImage;  // button background image to swap sprite

    [Header("Optional button sprites (assign in inspector)")]
    public Sprite spriteLocked;
    public Sprite spriteReady;
    public Sprite spriteClaimed;

    QuestData currentData;
    QuestProgressModel currentModel;
    QuestManager manager;

    // called by QuestManager to do initial setup
    public void Setup(QuestData data, QuestProgressModel model, QuestManager mgr)
    {
        currentData = data;
        currentModel = model;
        manager = mgr;

        // Ensure slider non-interactable
        if (progressSlider != null) progressSlider.interactable = false;

        Refresh(data, model, mgr);
    }

    // called to refresh UI when progress changes
    public void Refresh(QuestData data, QuestProgressModel model, QuestManager mgr)
    {
        currentData = data;
        currentModel = model;
        manager = mgr;

        if (data == null) return;

        if (titleText != null) titleText.text = data.title ?? "";

        // reward amount text (user asked: amount text shows reward amount, not progress fraction)
        if (amountText != null)
        {
            switch (data.rewardType)
            {
                case QuestRewardType.Coin: amountText.text = data.rewardAmount.ToString("N0"); break;
                case QuestRewardType.Shard: amountText.text = data.rewardAmount.ToString("N0"); break;
                case QuestRewardType.Energy: amountText.text = data.rewardAmount.ToString(); break;
                case QuestRewardType.Booster: amountText.text = data.rewardAmount.ToString(); break;
                default: amountText.text = ""; break;
            }
        }

        // slider
        if (progressSlider != null)
        {
            progressSlider.maxValue = Mathf.Max(1, data.requiredAmount);
            progressSlider.value = model != null ? Mathf.Clamp(model.current, 0, data.requiredAmount) : 0;
        }

        // fill color: blue for fill, gray for background is expected by prefab art.
        if (fillAreaImage != null)
        {
            // make fill area blue (you can adjust color in inspector)
            fillAreaImage.color = new Color(0.0f, 0.56f, 1.0f); // example bluish
        }

        // button states
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();

            bool ready = (model != null && model.current >= data.requiredAmount);
            bool claimed = (model != null && model.claimed);

            if (claimed)
            {
                actionButton.interactable = false;
                if (actionButtonText != null) actionButtonText.text = "Claimed";
                if (actionButtonImage != null && spriteClaimed != null) actionButtonImage.sprite = spriteClaimed;
            }
            else if (ready)
            {
                actionButton.interactable = true;
                if (actionButtonText != null) actionButtonText.text = "Claim";
                if (actionButtonImage != null && spriteReady != null) actionButtonImage.sprite = spriteReady;

                actionButton.onClick.AddListener(() =>
                {
                    if (manager != null && currentData != null)
                    {
                        manager.ClaimReward(currentData.questId);
                    }
                });
            }
            else
            {
                // not ready: show "Go" (developer can change behavior to navigate)
                actionButton.interactable = true;
                if (actionButtonText != null) actionButtonText.text = "Go";
                if (actionButtonImage != null && spriteLocked != null) actionButtonImage.sprite = spriteLocked;
                actionButton.onClick.AddListener(() =>
                {
                    // default 'Go' behavior can be customized. For now we log:
                    Debug.Log("[QuestItemUI] Go pressed for quest " + currentData.questId);
                });
            }
        }
    }
}
