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
        UpdateVisuals();
    }

    public void OnQuestClaimed(QuestData q)
    {
        // increment claimed count and update
        claimedCount++;
        UpdateVisuals();
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
        if (index < 0 || index >= thresholds.Length) return;
        if (claimedCrates[index]) return;
        if (claimedCount >= thresholds[index])
        {
            // grant random reward
            QuestRewardGenerator.GenerateRandomReward();
            claimedCrates[index] = true;
            UpdateVisuals();
        }
    }
}
