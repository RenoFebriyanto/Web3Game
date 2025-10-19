using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestChestController : MonoBehaviour
{
    public Slider progressSlider;
    public Image fillImage;
    public Image chestImage;
    public Sprite chestDimSprite;
    public Sprite chestYellowSprite;
    public Sprite chestCheckedSprite;
    public Button chestButton;
    public TMP_Text chestLabel;

    public int chestUnlockClaimCount = 3; // berapa klaim yang perlu dicapai untuk unlock

    int currentClaimed = 0;

    void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            // no event in manager, but we can refresh on enable
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (progressSlider != null)
        {
            // simple: count claimed across daily (or use manager API if available)
            int claimedCount = 0;
            // count claimed in daily
            foreach (var q in QuestManager.Instance != null ? QuestManager.Instance.dailyQuests : new System.Collections.Generic.List<QuestData>())
            {
                if (q == null) continue;
                if (QuestManager.Instance.IsClaimed(q.questId)) claimedCount++;
            }

            currentClaimed = claimedCount;
            float total = Mathf.Max(1, chestUnlockClaimCount);
            progressSlider.value = Mathf.Clamp01((float)currentClaimed / total);
            if (fillImage != null && QuestManager.Instance != null) fillImage.color = Color.Lerp(Color.gray, Color.yellow, progressSlider.value);
        }

        if (chestImage != null)
        {
            if (currentClaimed >= chestUnlockClaimCount) chestImage.sprite = chestYellowSprite;
            else chestImage.sprite = chestDimSprite;
        }

        if (chestLabel != null)
        {
            chestLabel.text = $"{currentClaimed}/{chestUnlockClaimCount}";
        }

        if (chestButton != null) chestButton.interactable = (currentClaimed >= chestUnlockClaimCount);
    }

    public void OnChestClicked()
    {
        // give random reward (example)
        PlayerEconomy.Instance?.AddCoins(500);
        // after claim reset daily (or subtract)
        QuestManager.Instance?.ResetDaily();
        RefreshUI();
    }
}
