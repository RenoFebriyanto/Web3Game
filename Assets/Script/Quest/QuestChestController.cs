using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅✅✅ PERSISTENT FIX: QuestChestController yang SELALU subscribe
/// - NEVER unsubscribe on disable (persistent listener)
/// - Subscribe di Awake dan NEVER unsubscribe sampai destroy
/// - Crate progress works dari MANA SAJA (LevelP atau QuestP)
/// </summary>
public class QuestChestController : MonoBehaviour
{
    // ✅ CRITICAL: Add missing Instance property
    public static QuestChestController Instance { get; private set; }

    [Header("UI")]
    public Image[] crateImages;
    public Slider progressSlider;

    [Header("Particle Effects")]
    [Tooltip("Assign CrateShine particle untuk setiap crate")]
    public GameObject[] crateParticles;

    [Header("Crate Sprites")]
    public Sprite crateLocked;
    public Sprite crateReady;
    public Sprite crateClaimed;

    [Header("Thresholds")]
    [Tooltip("Threshold untuk setiap crate")]
    public int[] thresholds = new int[] { 1, 3, 5, 8 };

    [Header("Reward Icons")]
    public Sprite iconCoin;
    public Sprite iconShard;
    public Sprite iconEnergy;
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

    const string PREF_CLAIMED_COUNT = "Kulino_CrateClaimedCount_v1";
    const string PREF_CRATE_CLAIMED_PREFIX = "Kulino_CrateStatus_";

    int claimedCount = 0;
    bool[] claimedCrates;
    bool isSubscribed = false;

    void Awake()
    {
        // ✅ Singleton pattern with Instance property
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (crateImages.Length != thresholds.Length)
        {
            Debug.LogWarning($"[QuestChestController] Adjusting array sizes to match");
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
        
        SubscribeToQuestEventsPersistent();
        
        UpdateVisuals();

        Log($"QuestChestController initialized (countOnlyDailyQuests={countOnlyDailyQuests})");
        Log($"Initial claimedCount: {claimedCount}");
    }

    void OnDestroy()
    {
        UnsubscribeFromQuestEvents();
        
        // ✅ Clear singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void SubscribeToQuestEventsPersistent()
    {
        if (isSubscribed)
        {
            Log("Already subscribed - skipping");
            return;
        }

        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null! Will retry...");
            Invoke(nameof(RetrySubscribe), 0.5f);
            return;
        }

        QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);
        QuestManager.Instance.OnQuestClaimed.AddListener(OnQuestClaimedHandler);

        isSubscribed = true;

        Log("✅✅✅ PERSISTENT SUBSCRIPTION ACTIVE");
        Log("Will receive events even when panel is disabled!");
    }

    void RetrySubscribe()
    {
        Log("Retrying subscribe to QuestManager events...");
        SubscribeToQuestEventsPersistent();
    }

    void UnsubscribeFromQuestEvents()
    {
        if (!isSubscribed) return;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);
            Log("✓ Unsubscribed from QuestManager events (on destroy)");
        }

        isSubscribed = false;
    }

    void OnQuestClaimedHandler(string questId, QuestData questData)
    {
        Log($"========================================");
        Log($"EVENT RECEIVED: Quest claimed");
        Log($"  Quest ID: {questId}");
        Log($"  Quest Data: {(questData != null ? "OK" : "NULL")}");
        
        if (questData == null)
        {
            LogWarning($"OnQuestClaimedHandler: questData is NULL for {questId}");
            Log($"========================================");
            return;
        }

        Log($"  Quest Type: {(questData.isDaily ? "DAILY" : "WEEKLY")}");
        Log($"  Count Mode: {(countOnlyDailyQuests ? "DAILY ONLY" : "ALL QUESTS")}");

        if (countOnlyDailyQuests && !questData.isDaily)
        {
            Log($"❌ FILTERED OUT: Quest is WEEKLY but countOnlyDailyQuests=true");
            Log($"========================================");
            return;
        }

        string questType = questData.isDaily ? "DAILY" : "WEEKLY";
        Log($"✅ PASSED FILTER: Processing {questType} quest");

        int oldCount = claimedCount;
        claimedCount++;
        
        Log($"✅ CRATE PROGRESS INCREMENT:");
        Log($"  Before: {oldCount}");
        Log($"  After: {claimedCount}");
        
        SaveProgress();
        UpdateVisuals();

        Log($"✓✓✓ CRATE PROGRESS UPDATED SUCCESSFULLY");
        Log($"========================================");
    }

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
        Log($"✓ Saved progress: claimedCount={claimedCount}");
    }

    void UpdateVisuals()
    {
        int max = thresholds.Length > 0 ? thresholds[thresholds.Length - 1] : 1;

        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = max;
            progressSlider.value = Mathf.Clamp(claimedCount, 0, max);
            progressSlider.interactable = false;

            Log($"✓ Slider updated: {claimedCount}/{max}");
        }

        // ✅ Update crate visuals AND particles
        for (int i = 0; i < crateImages.Length && i < thresholds.Length; i++)
        {
            if (crateImages[i] == null) continue;

            // Determine crate state
            bool isLocked = claimedCount < thresholds[i];
            bool isReady = claimedCount >= thresholds[i] && !claimedCrates[i];
            bool isClaimed = claimedCrates[i];

            // Update sprite
            if (isClaimed)
            {
                crateImages[i].sprite = crateClaimed;
            }
            else if (isReady)
            {
                crateImages[i].sprite = crateReady;
            }
            else
            {
                crateImages[i].sprite = crateLocked;
            }

            // ✅ UPDATED: Particle ON untuk READY atau CLAIMED
            if (crateParticles != null && i < crateParticles.Length && crateParticles[i] != null)
            {
                bool shouldShowParticle = isReady || isClaimed; // ⭐ CHANGED
                crateParticles[i].SetActive(shouldShowParticle);

                if (enableDebugLogs)
                {
                    string state = isClaimed ? "CLAIMED ✨" : (isReady ? "READY ✨" : "LOCKED");
                    Log($"Crate {i}: {state}, Particle={shouldShowParticle}");
                }
            }
        }

        Log($"✓ Visuals updated: progress={claimedCount}/{max}");
    }

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

    // ✅ NEW: Reset crate progress (called from QuestResetManager)
    public void ResetCrateProgress()
    {
        Log("========================================");
        Log("🔄 RESETTING CRATE PROGRESS");
        Log("========================================");

        claimedCount = 0;
        
        for (int i = 0; i < claimedCrates.Length; i++)
        {
            claimedCrates[i] = false;
        }

        SaveProgress();
        UpdateVisuals();

        Log("✅ CRATE PROGRESS RESET COMPLETE");
        Log($"  Claimed count: 0");
        Log($"  All crates reset to locked");
        Log("========================================");
    }

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
        ResetCrateProgress();
    }

    [ContextMenu("Debug: Print Status")]
    void DebugPrintStatus()
    {
        Debug.Log("=== QUEST CHEST CONTROLLER STATUS ===");
        Debug.Log($"Claimed Count: {claimedCount}");
        Debug.Log($"Is Subscribed: {isSubscribed} (PERSISTENT)");
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