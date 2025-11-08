using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅✅✅ FINAL FIX: QuestChestController yang count SEMUA quest (daily + weekly)
/// - Remove filter isDaily (count semua quest)
/// - Robust event subscription
/// - Works dari LevelP & QuestP
/// </summary>
public class QuestChestController : MonoBehaviour
{
    [Header("UI")]
    public Image[] crateImages;            // 4 crate images (assign di Inspector)
    public Slider progressSlider;          // bottom big slider

    [Header("Crate Sprites")]
    public Sprite crateLocked;
    public Sprite crateReady;
    public Sprite crateClaimed;

    [Header("Thresholds (quest completed count untuk unlock crate)")]
    [Tooltip("Threshold untuk setiap crate. Default: 1, 3, 5, 8 untuk 4 crates")]
    public int[] thresholds = new int[] { 1, 3, 5, 8 }; // 4 thresholds untuk 4 crates

    [Header("Reward Icons (Assign sprites di Inspector)")]
    [Tooltip("Icon untuk Economy rewards")]
    public Sprite iconCoin;
    public Sprite iconShard;
    public Sprite iconEnergy;

    [Tooltip("Icon untuk Booster rewards")]
    public Sprite iconMagnet;
    public Sprite iconShield;
    public Sprite iconCoin2x;
    public Sprite iconSpeedBoost;
    public Sprite iconTimeFreeze;

    [Header("Quest Counting Mode")]
    [Tooltip("Count only daily quests? (false = count all quests)")]
    public bool countOnlyDailyQuests = false;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Save keys
    const string PREF_CLAIMED_COUNT = "Kulino_CrateClaimedCount_v1";
    const string PREF_CRATE_CLAIMED_PREFIX = "Kulino_CrateStatus_"; // + index

    int claimedCount = 0;
    bool[] claimedCrates;

    void Awake()
    {
        // Ensure array size matches thresholds
        if (crateImages.Length != thresholds.Length)
        {
            Debug.LogWarning($"[QuestChestController] crateImages.Length ({crateImages.Length}) != thresholds.Length ({thresholds.Length})! Adjusting...");

            int minLength = Mathf.Min(crateImages.Length, thresholds.Length);
            System.Array.Resize(ref crateImages, minLength);
            System.Array.Resize(ref thresholds, minLength);
        }

        claimedCrates = new bool[thresholds.Length];

        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            Log("Progress slider set to non-interactable");
        }
    }

    void Start()
    {
        LoadProgress();
        SubscribeToQuestEvents();
        UpdateVisuals();

        Log($"QuestChestController initialized (countOnlyDailyQuests={countOnlyDailyQuests})");
    }

    void OnEnable()
    {
        SubscribeToQuestEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromQuestEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromQuestEvents();
    }

    // ========================================
    // ✅✅✅ EVENT SUBSCRIPTION
    // ========================================

    void SubscribeToQuestEvents()
    {
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null! Will retry...");
            Invoke(nameof(RetrySubscribe), 0.5f);
            return;
        }

        // Remove listener first (prevent double subscription)
        QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);

        // Add listener
        QuestManager.Instance.OnQuestClaimed.AddListener(OnQuestClaimedHandler);

        Log("✓✓✓ Subscribed to QuestManager.OnQuestClaimed event");
    }

    void RetrySubscribe()
    {
        Log("Retrying subscribe to QuestManager events...");
        SubscribeToQuestEvents();
    }

    void UnsubscribeFromQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);
            Log("Unsubscribed from QuestManager events");
        }
    }

    /// <summary>
    /// ✅✅✅ CRITICAL FIX: Count ALL quests (daily + weekly) atau hanya daily
    /// </summary>
    void OnQuestClaimedHandler(string questId, QuestData questData)
    {
        if (questData == null)
        {
            LogWarning($"OnQuestClaimedHandler: questData is NULL for {questId}");
            return;
        }

        // ✅ NEW: Configurable filter
        if (countOnlyDailyQuests && !questData.isDaily)
        {
            Log($"Quest {questId} is WEEKLY - SKIPPING (countOnlyDailyQuests=true)");
            return;
        }

        string questType = questData.isDaily ? "DAILY" : "WEEKLY";
        Log($"✅✅✅ QUEST CLAIMED EVENT: {questId} ({questType})");

        // Increment crate progress
        claimedCount++;
        SaveProgress();
        UpdateVisuals();

        Log($"✓ Crate progress updated: {claimedCount} quests claimed");
    }

    // ========================================
    // LEGACY METHOD (backward compatibility)
    // ========================================

    public void OnQuestClaimed(QuestData q)
    {
        Log($"[LEGACY] OnQuestClaimed called - using event system instead");
    }

    // ========================================
    // LOAD / SAVE PROGRESS
    // ========================================

    void LoadProgress()
    {
        claimedCount = PlayerPrefs.GetInt(PREF_CLAIMED_COUNT, 0);

        for (int i = 0; i < claimedCrates.Length; i++)
        {
            string key = PREF_CRATE_CLAIMED_PREFIX + i;
            claimedCrates[i] = PlayerPrefs.GetInt(key, 0) == 1;
        }

        Log($"Loaded progress: claimedCount={claimedCount}");
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt(PREF_CLAIMED_COUNT, claimedCount);

        for (int i = 0; i < claimedCrates.Length; i++)
        {
            string key = PREF_CRATE_CLAIMED_PREFIX + i;
            PlayerPrefs.SetInt(key, claimedCrates[i] ? 1 : 0);
        }

        PlayerPrefs.Save();
        Log($"Saved progress: claimedCount={claimedCount}");
    }

    // ========================================
    // VISUAL UPDATE
    // ========================================

    void UpdateVisuals()
    {
        int max = thresholds.Length > 0 ? thresholds[thresholds.Length - 1] : 1;
        
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = max;
            progressSlider.value = Mathf.Clamp(claimedCount, 0, max);
            progressSlider.interactable = false;
        }

        for (int i = 0; i < crateImages.Length && i < thresholds.Length; i++)
        {
            if (crateImages[i] == null) continue;

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

        Log($"Visuals updated: progress={claimedCount}/{max}");
    }

    // ========================================
    // CRATE CLAIM
    // ========================================

    public void TryClaimCrate(int index)
    {
        if (index < 0 || index >= thresholds.Length)
        {
            LogWarning($"Invalid crate index: {index}");
            return;
        }

        if (claimedCrates[index])
        {
            Log($"Crate {index} already claimed");
            return;
        }

        if (claimedCount >= thresholds[index])
        {
            Log($"Claiming crate {index}...");

            var rewardData = QuestRewardGenerator.RollRandomReward();

            if (rewardData != null)
            {
                ShowCrateRewardPopup(index, rewardData);
            }
            else
            {
                LogError("Failed to roll reward!");
            }
        }
        else
        {
            Log($"Crate {index} not ready. Need {thresholds[index]}, have {claimedCount}");
        }
    }

    void ShowCrateRewardPopup(int crateIndex, QuestRewardGenerator.RewardData reward)
    {
        if (PopupClaimQuest.Instance == null)
        {
            LogError("PopupClaimQuest.Instance is null!");
            QuestRewardGenerator.GrantRewardDirect(reward);
            claimedCrates[crateIndex] = true;
            SaveProgress();
            UpdateVisuals();
            return;
        }

        Sprite rewardIcon = GetRewardIcon(reward);
        string amountText = reward.amount > 0 ? reward.amount.ToString("N0") : "";
        string displayName = GetRewardDisplayName(reward);

        if (rewardIcon == null)
        {
            LogWarning($"Icon is NULL for {reward.rewardName}!");
        }

        PopupClaimQuest.Instance.Open(
            rewardIcon,
            amountText,
            displayName,
            () => {
                QuestRewardGenerator.GrantRewardDirect(reward);
                claimedCrates[crateIndex] = true;
                SaveProgress();
                UpdateVisuals();
                Log($"Crate {crateIndex} claimed successfully!");
            }
        );
    }

    // ========================================
    // REWARD HELPERS
    // ========================================

    Sprite GetRewardIcon(QuestRewardGenerator.RewardData reward)
    {
        if (reward == null) return null;

        if (reward.isBooster)
        {
            string id = reward.rewardName.ToLower();
            switch (id)
            {
                case "coin2x": return iconCoin2x;
                case "magnet": return iconMagnet;
                case "shield": return iconShield;
                case "speedboost":
                case "rocketboost": return iconSpeedBoost;
                case "timefreeze": return iconTimeFreeze;
                default:
                    LogWarning($"Unknown booster icon: {reward.rewardName}");
                    return null;
            }
        }
        else
        {
            string lowerName = reward.rewardName.ToLower();
            if (lowerName.Contains("coin")) return iconCoin;
            if (lowerName.Contains("shard")) return iconShard;
            if (lowerName.Contains("energy")) return iconEnergy;

            LogWarning($"Unknown economy icon: {reward.rewardName}");
            return null;
        }
    }

    string GetRewardDisplayName(QuestRewardGenerator.RewardData reward)
    {
        if (reward == null) return "Reward";

        if (reward.isBooster)
        {
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
            string lowerName = reward.rewardName.ToLower();
            if (lowerName.Contains("coin")) return "Coins";
            if (lowerName.Contains("shard")) return "Blue Shard";
            if (lowerName.Contains("energy")) return "Energy";
            return reward.rewardName;
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[QuestChestController] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[QuestChestController] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[QuestChestController] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

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

    [ContextMenu("Debug: Print Status")]
    void DebugPrintStatus()
    {
        Debug.Log("=== QUEST CHEST CONTROLLER STATUS ===");
        Debug.Log($"Claimed Count: {claimedCount}");
        Debug.Log($"Count Only Daily: {countOnlyDailyQuests}");
        Debug.Log($"QuestManager.Instance: {(QuestManager.Instance != null ? "OK" : "NULL")}");
        
        if (QuestManager.Instance != null)
        {
            var dailyQuests = QuestManager.Instance.GetDailyQuests();
            Debug.Log($"Available Daily Quests: {dailyQuests?.Count ?? 0}");
            
            if (dailyQuests != null)
            {
                int claimedDaily = 0;
                foreach (var q in dailyQuests)
                {
                    if (q == null) continue;
                    var p = QuestManager.Instance.GetProgress(q.questId);
                    if (p != null && p.claimed)
                    {
                        claimedDaily++;
                        Debug.Log($"  ✓ DAILY {q.questId}: CLAIMED");
                    }
                }
                Debug.Log($"Claimed Daily: {claimedDaily}/{dailyQuests.Count}");
            }
        }

        Debug.Log($"\nCrates status:");
        for (int i = 0; i < claimedCrates.Length; i++)
        {
            string status = claimedCrates[i] ? "CLAIMED" : 
                           (claimedCount >= thresholds[i] ? "READY" : $"LOCKED ({claimedCount}/{thresholds[i]})");
            Debug.Log($"  Crate {i}: {status}");
        }
        Debug.Log("====================================");
    }

    [ContextMenu("Debug: Toggle Count Mode")]
    void DebugToggleCountMode()
    {
        countOnlyDailyQuests = !countOnlyDailyQuests;
        Debug.Log($"[DEBUG] Count mode changed to: {(countOnlyDailyQuests ? "DAILY ONLY" : "ALL QUESTS")}");
    }

    [ContextMenu("Debug: Force Subscribe")]
    void DebugForceSubscribe()
    {
        SubscribeToQuestEvents();
    }

    [ContextMenu("Debug: Clear Saved Progress")]
    void DebugClearSavedProgress()
    {
        PlayerPrefs.DeleteKey(PREF_CLAIMED_COUNT);
        for (int i = 0; i < 10; i++)
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

        Debug.Log("[DEBUG] Cleared saved progress");
    }
}