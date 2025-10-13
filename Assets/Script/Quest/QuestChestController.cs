// QuestChestController.cs
// Put this in Assets/Script/Quest/QuestChestController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the chest progress UI. Subscribes to QuestManager.OnClaimedCountChanged
/// and updates slider + chest sprite. When chest is claimable, the chest button will call ClaimChest().
/// </summary>
public class QuestChestController : MonoBehaviour
{
    [Header("UI refs")]
    public Slider progressSlider; // slider filled from 0..1
    public Image progressFillImage; // optional
    public Image chestImage;
    public Button chestButton;
    public TMP_Text chestLabel; // optional label showing "Claim" etc

    [Header("Sprites")]
    public Sprite chestDimSprite;
    public Sprite chestYellowSprite;
    public Sprite chestCheckedSprite;

    [Header("Chest thresholds")]
    public int totalNeededForFull = 10; // how many claimed quests needed to reach chest
    public int chestUnlockClaimCount = 3; // example threshold for first chest (if multiple, adapt)

    // internal
    int currentClaimed = 0;

    void Start()
    {
        // subscribe
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnClaimedCountChanged += OnClaimCountChanged;
            // initialize UI
            OnClaimCountChanged(QuestManager.Instance.GetClaimedCount());
        }

        if (chestButton != null)
        {
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(OnChestClicked);
        }

        RefreshUI();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnClaimedCountChanged -= OnClaimCountChanged;
    }

    void OnClaimCountChanged(int claimedCount)
    {
        currentClaimed = claimedCount;
        RefreshUI();
    }

    void RefreshUI()
    {
        // progress normalized
        float t = 0f;
        if (totalNeededForFull > 0) t = Mathf.Clamp01((float)currentClaimed / totalNeededForFull);

        if (progressSlider != null)
        {
            progressSlider.value = t;
            if (progressFillImage != null)
            {
                // optionally color fill based on t (already set in slider fill)
            }
        }

        bool unlockable = (currentClaimed >= chestUnlockClaimCount);

        if (chestImage != null)
        {
            if (currentClaimed >= totalNeededForFull)
                chestImage.sprite = chestCheckedSprite ?? chestYellowSprite ?? chestDimSprite;
            else if (unlockable)
                chestImage.sprite = chestYellowSprite ?? chestDimSprite;
            else
                chestImage.sprite = chestDimSprite;
        }

        if (chestLabel != null)
        {
            if (currentClaimed >= totalNeededForFull) chestLabel.text = "Claimed";
            else if (unlockable) chestLabel.text = "Open";
            else chestLabel.text = $"{currentClaimed}/{chestUnlockClaimCount}";
        }

        if (chestButton != null)
        {
            chestButton.interactable = (currentClaimed >= chestUnlockClaimCount);
        }
    }

    void OnChestClicked()
    {
        // logic: grant random reward & mark as claimed (or mark chest opened for this cycle)
        // simple example: give coins and then reset the chest progress (or reduce by threshold).
        Debug.Log("[QuestChestController] Chest clicked.");

        // Deliver simple reward example:
        PlayerEconomy.Instance?.AddCoins(500); // example fixed reward

        // mark chest claimed: here we reset claimed count to 0 for simplicity via QuestManager.ResetDaily()
        // Option: you may subtract threshold instead of full reset.
        if (QuestManager.Instance != null)
        {
            // For daily chest: ResetDaily() might be heavy; instead you can just reduce saved claimed flags for some quests.
            // Here we call ResetDaily to simplify example. Adjust to your flow.
            QuestManager.Instance.ResetDaily();
        }

        // update ui manually (ResetDaily should fire OnClaimedCountChanged)
        RefreshUI();
    }

    // Optional helper for external callers:
    public void ForceRefresh() => RefreshUI();
}
