// QuestItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text progressText;
    public Image iconImage;
    public Button actionButton;        // button that either "GO" / "Claim"
    public Image actionButtonImage;    // sprite to visualize state (optional)
    public TMP_Text actionButtonText;

    [Header("Button Sprites")]
    public Sprite spriteLocked;   // gray / cannot claim
    public Sprite spriteReady;    // blue / claim available
    public Sprite spriteClaimed;  // yellow / already claimed

    QuestData data;
    QuestProgressModel progress;
    QuestManager manager;

    public void Setup(QuestData d, QuestProgressModel p, QuestManager mgr)
    {
        data = d;
        progress = p;
        manager = mgr;

        titleText.text = d.title;
        descText.text = d.description;
        iconImage.sprite = d.icon;
        Refresh();
        // set listener
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClicked);
    }

    public void Refresh()
    {
        if (data == null || progress == null) return;

        int cur = progress.current;
        int req = data.requiredCount;
        progressText.text = $"{cur}/{req}";

        bool completed = cur >= req;
        bool claimed = progress.claimed;

        if (claimed)
        {
            // show claimed state
            actionButtonImage.sprite = spriteClaimed;
            actionButtonText.text = "CLAIMED";
            actionButton.interactable = false;
        }
        else if (completed)
        {
            actionButtonImage.sprite = spriteReady;
            actionButtonText.text = "CLAIM";
            actionButton.interactable = true;
        }
        else
        {
            actionButtonImage.sprite = spriteLocked;
            actionButtonText.text = "GO";
            actionButton.interactable = true;
        }
    }

    void OnActionClicked()
    {
        if (data == null || progress == null || manager == null) return;

        if (progress.current >= data.requiredCount && !progress.claimed)
        {
            // Claim flow
            manager.ClaimQuest(data.questId);
        }
        else
        {
            // GO / help flow: could open relevant UI or show tooltip
            // For now just log
            Debug.Log($"[QuestItemUI] GO for quest {data.questId}");
        }
    }
}
