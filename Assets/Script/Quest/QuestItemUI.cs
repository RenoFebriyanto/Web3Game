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

    [Header("Slider")]
    
    

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
            progressSlider.maxValue = q.requiredAmount;
            progressSlider.interactable = false;
        }

        // button
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButton);
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
            // fill color to indicated complete
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
            // claim
            Debug.Log("CLAIMED");
            manager.ClaimQuest(questData.questId);
        }
        else
        {
            // Go — you can implement navigation to relevant screen; for now we'll just log
            Debug.Log($"[QuestItemUI] Go pressed for {questData.questId}");
        }
    }
}
