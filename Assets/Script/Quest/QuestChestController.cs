// QuestChestController.cs
using UnityEngine;
using UnityEngine.UI;

public class QuestChestController : MonoBehaviour
{
    [Header("UI refs")]
    public Slider progressSlider;      // global progress bar for daily progress
    public Image chestImage;
    public Sprite chestLocked;
    public Sprite chestReady;
    public Sprite chestClaimed;

    // threshold (0..1 normalized) at which chest becomes claimable
    [Range(0f, 1f)] public float requiredProgress = 0.5f;

    bool claimed = false;

    public void SetProgressNormalized(float t)
    {
        if (progressSlider != null) progressSlider.value = Mathf.Clamp01(t);

        if (!claimed)
        {
            if (progressSlider != null && progressSlider.value >= requiredProgress)
            {
                if (chestImage != null && chestReady != null) chestImage.sprite = chestReady;
            }
            else
            {
                if (chestImage != null && chestLocked != null) chestImage.sprite = chestLocked;
            }
        }
    }

    public void ClaimChest()
    {
        if (claimed) return;
        if (progressSlider != null && progressSlider.value >= requiredProgress)
        {
            claimed = true;
            if (chestImage != null && chestClaimed != null) chestImage.sprite = chestClaimed;
            // reward logic here (randomize reward) -> call PlayerEconomy or BoosterInventory
            Debug.Log("[QuestChestController] Chest claimed (implement reward logic)");
        }
        else
        {
            Debug.Log("[QuestChestController] Chest not ready");
        }
    }
}
