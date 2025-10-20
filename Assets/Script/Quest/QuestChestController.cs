using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CrateData
{
    public string crateName;
    public Image crateImage;
    public Sprite crateGraySprite;
    public Sprite crateYellowSprite;
    public Sprite crateClaimedSprite;
    public Button crateButton;
    public int requiredClaimedCount;
    public bool claimed = false;
}

public class QuestChestController : MonoBehaviour
{
    [Header("Progress Bar")]
    public Slider progressSlider;
    public Image fillImage;

    [Header("Crates (assign in order)")]
    public List<CrateData> crates = new List<CrateData>();

    [Header("Settings")]
    public int maxClaimedForFullBar = 5;

    void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestClaimed += OnQuestClaimedHandler;
        }

        RefreshUI();
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestClaimed -= OnQuestClaimedHandler;
        }
    }

    void OnQuestClaimedHandler()
    {
        Debug.Log("[QuestChestController] Quest claimed event received!");
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestChestController] QuestManager.Instance is null");
            return;
        }

        int totalClaimed = QuestManager.Instance.GetTotalClaimedCount();

        // Update progress bar
        if (progressSlider != null)
        {
            float progress = Mathf.Clamp01((float)totalClaimed / maxClaimedForFullBar);
            progressSlider.value = progress;

            Debug.Log($"[QuestChestController] Progress: {totalClaimed}/{maxClaimedForFullBar} = {progress:F2}");
        }

        // Update fill color
        if (fillImage != null)
        {
            Color startColor = new Color(0.3f, 0.5f, 1f, 1f); // Blue
            Color endColor = new Color(1f, 0.9f, 0f, 1f);    // Yellow
            fillImage.color = Color.Lerp(startColor, endColor, progressSlider.value);
        }

        // Update each crate
        foreach (var crate in crates)
        {
            UpdateCrate(crate, totalClaimed);
        }
    }

    void UpdateCrate(CrateData crate, int totalClaimed)
    {
        if (crate == null)
        {
            Debug.LogWarning("[QuestChestController] Crate data is null");
            return;
        }

        bool canClaim = (totalClaimed >= crate.requiredClaimedCount) && !crate.claimed;

        // Update sprite
        if (crate.crateImage != null)
        {
            if (crate.claimed)
            {
                if (crate.crateClaimedSprite != null)
                    crate.crateImage.sprite = crate.crateClaimedSprite;
            }
            else if (canClaim)
            {
                if (crate.crateYellowSprite != null)
                    crate.crateImage.sprite = crate.crateYellowSprite;
            }
            else
            {
                if (crate.crateGraySprite != null)
                    crate.crateImage.sprite = crate.crateGraySprite;
            }

            crate.crateImage.color = Color.white; // Full opacity, no tint
        }

        // Update button
        if (crate.crateButton != null)
        {
            crate.crateButton.interactable = canClaim;

            // Remove old listeners
            crate.crateButton.onClick.RemoveAllListeners();

            if (canClaim)
            {
                crate.crateButton.onClick.AddListener(() => OnCrateClicked(crate));
            }
        }

        Debug.Log($"[QuestChestController] Crate '{crate.crateName}': required={crate.requiredClaimedCount}, total={totalClaimed}, canClaim={canClaim}, claimed={crate.claimed}");
    }

    void OnCrateClicked(CrateData crate)
    {
        if (crate.claimed)
        {
            Debug.Log($"[QuestChestController] Crate '{crate.crateName}' already claimed");
            return;
        }

        Debug.Log($"[QuestChestController] Crate '{crate.crateName}' clicked! Generating reward...");

        // Generate random reward
        QuestRewardGenerator.GenerateRandomReward();

        // Mark as claimed
        crate.claimed = true;

        // Refresh UI
        RefreshUI();
    }

    [ContextMenu("Reset All Crates")]
    public void ResetAllCrates()
    {
        foreach (var crate in crates)
        {
            crate.claimed = false;
        }
        RefreshUI();
        Debug.Log("[QuestChestController] All crates reset");
    }

    [ContextMenu("Test - Simulate 1 Quest Claimed")]
    public void TestSimulate1Claim()
    {
        if (QuestManager.Instance != null)
        {
            OnQuestClaimedHandler();
        }
    }

    
}