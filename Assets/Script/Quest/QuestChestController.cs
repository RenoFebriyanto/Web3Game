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

    // Save keys
    const string PREF_CLAIMED_COUNT = "Kulino_CrateClaimedCount_v1";
    const string PREF_CRATE_CLAIMED_PREFIX = "Kulino_CrateStatus_"; // + index

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

        // Load saved progress
        LoadProgress();

        UpdateVisuals();
    }

    /// <summary>
    /// Load saved crate progress dari PlayerPrefs
    /// </summary>
    void LoadProgress()
    {
        // Load claimed count
        claimedCount = PlayerPrefs.GetInt(PREF_CLAIMED_COUNT, 0);

        // Load each crate claimed status
        for (int i = 0; i < claimedCrates.Length; i++)
        {
            string key = PREF_CRATE_CLAIMED_PREFIX + i;
            claimedCrates[i] = PlayerPrefs.GetInt(key, 0) == 1;
        }

        Debug.Log($"[QuestChestController] Loaded progress: claimedCount={claimedCount}, crates claimed: {string.Join(",", claimedCrates)}");
    }

    /// <summary>
    /// Save crate progress ke PlayerPrefs
    /// </summary>
    void SaveProgress()
    {
        // Save claimed count
        PlayerPrefs.SetInt(PREF_CLAIMED_COUNT, claimedCount);

        // Save each crate claimed status
        for (int i = 0; i < claimedCrates.Length; i++)
        {
            string key = PREF_CRATE_CLAIMED_PREFIX + i;
            PlayerPrefs.SetInt(key, claimedCrates[i] ? 1 : 0);
        }

        PlayerPrefs.Save();
        Debug.Log($"[QuestChestController] Saved progress: claimedCount={claimedCount}");
    }

    /// <summary>
    /// Dipanggil oleh QuestManager saat player claim quest
    /// </summary>
    public void OnQuestClaimed(QuestData q)
    {
        claimedCount++;
        SaveProgress(); // Save setiap kali ada perubahan
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

            // Generate reward data (tanpa langsung grant ke player)
            var rewardData = QuestRewardGenerator.RollRandomReward();

            if (rewardData != null)
            {
                // Show popup dengan reward yang didapat
                ShowCrateRewardPopup(index, rewardData);
            }
            else
            {
                Debug.LogError("[QuestChestController] Failed to roll reward!");
            }
        }
        else
        {
            Debug.Log($"[QuestChestController] Crate {index} not ready. Need {thresholds[index]} claimed quests, have {claimedCount}");
        }
    }

    /// <summary>
    /// Tampilkan popup reward dan grant reward setelah confirm
    /// </summary>
    void ShowCrateRewardPopup(int crateIndex, QuestRewardGenerator.RewardData reward)
    {
        if (PopupClaimQuest.Instance == null)
        {
            Debug.LogError("[QuestChestController] PopupClaimQuest.Instance is null!");
            // Fallback: grant langsung tanpa popup
            QuestRewardGenerator.GrantRewardDirect(reward);
            claimedCrates[crateIndex] = true;
            SaveProgress(); // Save setelah claim
            UpdateVisuals();
            return;
        }

        // Get icon untuk reward
        Sprite rewardIcon = GetRewardIcon(reward);

        // Format amount text
        string amountText = reward.amount > 0 ? reward.amount.ToString("N0") : "";

        // Get display name
        string displayName = GetRewardDisplayName(reward);

        // Open popup
        PopupClaimQuest.Instance.Open(
            rewardIcon,
            amountText,
            displayName,
            () => {
                // Callback saat player confirm di popup
                // Grant reward ke player
                QuestRewardGenerator.GrantRewardDirect(reward);

                // Mark crate as claimed
                claimedCrates[crateIndex] = true;
                SaveProgress(); // Save setelah claim
                UpdateVisuals();

                Debug.Log($"[QuestChestController] Crate {crateIndex} claimed successfully!");
            }
        );
    }

    /// <summary>
    /// Get icon sprite untuk reward
    /// </summary>
    Sprite GetRewardIcon(QuestRewardGenerator.RewardData reward)
    {
        if (reward == null) return null;

        // Load icon dari Resources berdasarkan reward name
        // Adjust path sesuai struktur folder Anda
        string iconPath = "";

        if (reward.isBooster)
        {
            // Icon booster
            iconPath = $"Icons/Booster/{reward.rewardName}";
        }
        else
        {
            // Icon economy (coins, shards, energy)
            string lowerName = reward.rewardName.ToLower();
            if (lowerName.Contains("coin")) iconPath = "Icons/Economy/Coin";
            else if (lowerName.Contains("shard")) iconPath = "Icons/Economy/Shard";
            else if (lowerName.Contains("energy")) iconPath = "Icons/Economy/Energy";
        }

        Sprite icon = Resources.Load<Sprite>(iconPath);

        if (icon == null)
        {
            Debug.LogWarning($"[QuestChestController] Icon not found at path: {iconPath}");
        }

        return icon;
    }

    /// <summary>
    /// Get display name untuk reward
    /// </summary>
    string GetRewardDisplayName(QuestRewardGenerator.RewardData reward)
    {
        if (reward == null) return "Reward";

        if (reward.isBooster)
        {
            // Booster display names
            string id = reward.rewardName.ToLower();
            switch (id)
            {
                case "coin2x": return "Coin 2x Booster";
                case "magnet": return "Magnet Booster";
                case "shield": return "Shield Booster";
                case "speedboost":
                case "rocketboost": return "Speed Boost";
                case "timefreeze": return "Time Freeze";
                default:
                    if (id.Length > 0)
                        return char.ToUpper(id[0]) + id.Substring(1) + " Booster";
                    return "Booster";
            }
        }
        else
        {
            // Economy display names
            string lowerName = reward.rewardName.ToLower();
            if (lowerName.Contains("coin")) return "Coins";
            if (lowerName.Contains("shard")) return "Blue Shard";
            if (lowerName.Contains("energy")) return "Energy";
            return reward.rewardName;
        }
    }

    // Debug helpers
    [ContextMenu("Debug: Add Quest Progress")]
    void DebugAddProgress()
    {
        claimedCount++;
        SaveProgress();
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
        SaveProgress();
        UpdateVisuals();
        Debug.Log("[DEBUG] All crates reset");
    }

    [ContextMenu("Debug: Print Reward Probabilities")]
    void DebugPrintProbabilities()
    {
        QuestRewardGenerator.DebugPrintProbabilities();
    }

    [ContextMenu("Debug: Clear Saved Progress")]
    void DebugClearSavedProgress()
    {
        PlayerPrefs.DeleteKey(PREF_CLAIMED_COUNT);
        for (int i = 0; i < 10; i++) // clear up to 10 crates
        {
            PlayerPrefs.DeleteKey(PREF_CRATE_CLAIMED_PREFIX + i);
        }
        PlayerPrefs.Save();

        claimedCount = 0;
        for (int i = 0; i < claimedCrates.Length; i++)
        {
            claimedCrates[i] = false;
        }
        UpdateVisuals();

        Debug.Log("[DEBUG] Cleared saved crate progress from PlayerPrefs");
    }
}