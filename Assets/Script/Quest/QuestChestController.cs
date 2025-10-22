using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestChestController : MonoBehaviour
{
    [Header("UI")]
    public Image[] crateImages;            // 3 crates images in order left->right
    public Sprite crateLocked;
    public Sprite crateReady;
    public Sprite crateClaimed;

    [Header("Progress")]
    public Slider progressSlider;          // bottom big slider
    public int[] thresholds = new int[] { 1, 3, 5 }; // number of claimed quests needed for crates

    int claimedCount = 0;
    bool[] claimedCrates;

    void Start()
    {
        claimedCrates = new bool[thresholds.Length];

        // PENTING: Disable slider interaction agar tidak bisa digeser
        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            Debug.Log("[QuestChestController] Progress slider set to non-interactable");
        }

        UpdateVisuals();
    }

    public void OnQuestClaimed(QuestData q)
    {
        // increment claimed count and update
        claimedCount++;
        UpdateVisuals();
        Debug.Log($"[QuestChestController] Quest claimed. Total: {claimedCount}");
    }

    void UpdateVisuals()
    {
        // update slider (range 0..max threshold)
        int max = thresholds.Length > 0 ? thresholds[thresholds.Length - 1] : 1;
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = max;
            progressSlider.value = Mathf.Clamp(claimedCount, 0, max);

            // Make sure slider stays non-interactable
            progressSlider.interactable = false;
        }

        for (int i = 0; i < crateImages.Length && i < thresholds.Length; i++)
        {
            if (claimedCrates[i])
            {
                crateImages[i].sprite = crateClaimed;
            }
            else if (claimedCount >= thresholds[i])
            {
                crateImages[i].sprite = crateReady;
            }
            else
            {
                crateImages[i].sprite = crateLocked;
            }
        }
    }

    // call from UI when player clicks a crate
    public void TryClaimCrate(int index)
    {
        if (index < 0 || index >= thresholds.Length)
        {
            Debug.LogWarning($"[QuestChestController] Invalid crate index: {index}");
            return;
        }

        if (claimedCrates[index])
        {
            Debug.Log($"[QuestChestController] Crate {index} already claimed");
            return;
        }

        if (claimedCount >= thresholds[index])
        {
            // Generate and grant random reward
            Debug.Log($"[QuestChestController] Claiming crate {index}...");
            QuestRewardGenerator.GenerateRandomReward();

            claimedCrates[index] = true;
            UpdateVisuals();

            Debug.Log($"[QuestChestController] Crate {index} claimed successfully!");
        }
        else
        {
            Debug.Log($"[QuestChestController] Crate {index} not ready. Need {thresholds[index]} claimed quests, have {claimedCount}");
        }
    }

    // Debug helpers
    [ContextMenu("Debug: Add Quest Progress")]
    void DebugAddProgress()
    {
        claimedCount++;
        UpdateVisuals();
        Debug.Log($"[DEBUG] Claimed count: {claimedCount}");
    }

    [ContextMenu("Debug: Reset All Crates")]
    void DebugResetCrates()
    {
        claimedCount = 0;
        for (int i = 0; i < claimedCrates.Length; i++)
        {
            claimedCrates[i] = false;
        }
        UpdateVisuals();
        Debug.Log("[DEBUG] All crates reset");
    }
}