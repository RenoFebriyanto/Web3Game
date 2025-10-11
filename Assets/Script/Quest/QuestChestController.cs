// QuestChestController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestChestController : MonoBehaviour
{
    [Header("UI")]
    public Slider progressBar;      // the blue bar
    public Image chestImage;        // chest image that changes sprite
    public Button chestButton;      // claim chest button
    public TMP_Text chestLabel;

    [Header("Sprites")]
    public Sprite chestDim;
    public Sprite chestYellow;
    public Sprite chestChecked;

    [Header("Thresholds")]
    public int chestThreshold = 3; // number of claimed quests required to enable chest

    bool chestClaimed = false;
    int currentClaimedCount = 0;

    public event Action OnChestClaimed;

    void Awake()
    {
        if (chestButton != null)
        {
            chestButton.onClick.RemoveAllListeners();
            chestButton.onClick.AddListener(OnChestClicked);
        }
    }

    public void UpdateProgress(int claimedCount, int totalNeededForFullBar = 10)
    {
        currentClaimedCount = claimedCount;
        float t = Mathf.Clamp01((float)claimedCount / (float)totalNeededForFullBar);
        if (progressBar != null) progressBar.value = t;

        // Chest visuals
        if (chestClaimed)
        {
            chestImage.sprite = chestChecked;
            chestButton.interactable = false;
            chestLabel.text = "Claimed";
        }
        else if (claimedCount >= chestThreshold)
        {
            chestImage.sprite = chestYellow;
            chestButton.interactable = true;
            chestLabel.text = "Claim!";
        }
        else
        {
            chestImage.sprite = chestDim;
            chestButton.interactable = false;
            chestLabel.text = $"Need {chestThreshold - claimedCount}";
        }
    }

    void OnChestClicked()
    {
        if (chestClaimed) return;
        if (currentClaimedCount < chestThreshold) return;
        chestClaimed = true;
        UpdateProgress(currentClaimedCount);
        OnChestClaimed?.Invoke();
        Debug.Log("[QuestChestController] Chest claimed!");
    }

    public void ResetChest() // if you want to reset for testing
    {
        chestClaimed = false;
        UpdateProgress(currentClaimedCount);
    }
}
