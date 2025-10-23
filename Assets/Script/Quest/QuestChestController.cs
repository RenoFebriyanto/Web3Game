using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestChestController : MonoBehaviour
{
    [Header("UI")]
    public Image[] crateImages;            // 4 crate images (assign di Inspector)
    public Sprite crateLocked;
    public Sprite crateReady;
    public Sprite crateClaimed;

    [Header("Progress")]
    public Slider progressSlider;          // bottom big slider

    [Header("Thresholds (quest completed count untuk unlock crate)")]
    [Tooltip("Threshold untuk setiap crate. Default: 1, 3, 5, 8 untuk 4 crates")]
    public int[] thresholds = new int[] { 1, 3, 5, 8 }; // 4 thresholds untuk 4 crates

    int claimedCount = 0;
    bool[] claimedCrates;

    void Start()
    {
        // Ensure array size matches thresholds
        if (crateImages.Length != thresholds.Length)
        {
            Debug.LogWarning($"[QuestChestController] crateImages.Length ({crateImages.Length}) != thresholds.Length ({thresholds.Length})! Adjusting...");

            // Resize arrays to match smaller one
            int minLength = Mathf.Min(crateImages.Length, thresholds.Length);
            System.Array.Resize(ref crateImages, minLength);
            System.Array.Resize(ref thresholds, minLength);
        }

        claimedCrates = new bool[thresholds.Length];

        // PENTING: Disable slider interaction agar tidak bisa digeser
        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            Debug.Log("[QuestChestController] Progress slider set to non-interactable");
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Dipanggil oleh QuestManager saat player claim quest
    /// </summary>
    public void OnQuestClaimed(QuestData q)
    {
        claimedCount++;
        UpdateVisuals();
        Debug.Log($"[QuestChestController] Quest claimed. Total: {claimedCount}");
    }

    void UpdateVisuals()
    {
        // Update slider (range 0..max threshold)
        int max = thresholds.Length > 0 ? thresholds[thresholds.Length - 1] : 1;
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = max;
            progressSlider.value = Mathf.Clamp(claimedCount, 0, max);

            // Make sure slider stays non-interactable
            progressSlider.interactable = false;
        }

        // Update crate sprites based on state
        for (int i = 0; i < crateImages.Length && i < thresholds.Length; i++)
        {
            if (crateImages[i] == null) continue;

            if (claimedCrates[i])
            {
                // Already claimed
                crateImages[i].sprite = crateClaimed;
            }
            else if (claimedCount >= thresholds[i])
            {
                // Ready to claim
                crateImages[i].sprite = crateReady;
            }
            else
            {
                // Still locked
                crateImages[i].sprite = crateLocked;
            }
        }
    }

    /// <summary>
    /// Dipanggil saat player klik crate (dari CrateButton script atau Button.onClick)
    /// </summary>
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
            Debug.Log($"[QuestChestController] Claiming crate {index}...");

            // Generate and grant random reward
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

    [ContextMenu("Debug: Print Reward Probabilities")]
    void DebugPrintProbabilities()
    {
        QuestRewardGenerator.DebugPrintProbabilities();
    }
}