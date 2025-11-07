using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// System untuk daily reward yang diberikan setelah semua quest daily selesai.
/// - Random amount shard & energy
/// - Real-time daily reset (offline support)
/// - Integration dengan QuestManager
/// </summary>
public class DailyRewardSystem : MonoBehaviour
{
    public static DailyRewardSystem Instance { get; private set; }

    [Header("=== REWARD SETTINGS ===")]
    [Header("Shard Reward")]
    [Tooltip("Chance untuk dapat shard (0-100%)")]
    [Range(0f, 100f)]
    public float shardChance = 30f; // 30% chance

    [Tooltip("Min shard yang bisa didapat")]
    public int minShardAmount = 5;

    [Tooltip("Max shard yang bisa didapat")]
    public int maxShardAmount = 10;

    [Header("Energy Reward")]
    [Tooltip("Min energy yang bisa didapat")]
    public int minEnergyAmount = 10;

    [Tooltip("Max energy yang bisa didapat")]
    public int maxEnergyAmount = 30;

    [Header("=== EVENTS ===")]
    public UnityEvent OnRewardAvailable = new UnityEvent();
    public UnityEvent OnRewardClaimed = new UnityEvent();
    public UnityEvent OnRewardReset = new UnityEvent();

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = true;

    // Save keys
    const string PREF_LAST_CLAIM_DATE = "Kulino_DailyReward_LastClaimDate_v1";
    const string PREF_REWARD_CLAIMED = "Kulino_DailyReward_Claimed_v1";
    const string PREF_ROLLED_SHARD = "Kulino_DailyReward_RolledShard_v1";
    const string PREF_ROLLED_ENERGY = "Kulino_DailyReward_RolledEnergy_v1";

    // Runtime state
    private bool isRewardAvailable = false;
    private bool isRewardClaimed = false;
    private int rolledShardAmount = 0;
    private int rolledEnergyAmount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Load state & check for reset
        LoadState();
        CheckDailyReset();

        // Subscribe to quest events
        SubscribeToQuestEvents();

        Log("✓ DailyRewardSystem initialized");
    }

    void OnDestroy()
    {
        UnsubscribeFromQuestEvents();
    }

    // ========================================
    // QUEST EVENT SUBSCRIPTION
    // ========================================

    void SubscribeToQuestEvents()
    {
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null! Cannot subscribe to events.");
            return;
        }

        // Subscribe to quest claimed events
        QuestManager.Instance.OnQuestClaimed.AddListener(OnQuestClaimedHandler);

        Log("✓ Subscribed to QuestManager events");
    }

    void UnsubscribeFromQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);
        }
    }

    /// <summary>
    /// Called when any quest is claimed
    /// </summary>
    void OnQuestClaimedHandler(string questId, QuestData questData)
    {
        if (questData == null) return;

        // Only check daily quests
        if (!questData.isDaily) return;

        Log($"Daily quest claimed: {questId}");

        // Check if all daily quests are complete
        CheckAllDailyQuestsComplete();
    }

    // ========================================
    // DAILY QUEST COMPLETION CHECK
    // ========================================

    /// <summary>
    /// Check apakah semua daily quest sudah complete & claimed
    /// </summary>
    void CheckAllDailyQuestsComplete()
    {
        if (QuestManager.Instance == null)
        {
            LogError("QuestManager.Instance is null!");
            return;
        }

        var dailyQuests = QuestManager.Instance.GetDailyQuests();
        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Log("No daily quests found");
            return;
        }

        bool allComplete = true;
        int completedCount = 0;
        int totalCount = dailyQuests.Count;

        foreach (var quest in dailyQuests)
        {
            if (quest == null) continue;

            var progress = QuestManager.Instance.GetProgress(quest.questId);
            if (progress == null) continue;

            // Check if quest is complete AND claimed
            bool isComplete = progress.progress >= quest.requiredAmount;
            bool isClaimed = progress.claimed;

            if (isComplete && isClaimed)
            {
                completedCount++;
            }
            else
            {
                allComplete = false;
            }

            Log($"  Quest {quest.questId}: complete={isComplete}, claimed={isClaimed}");
        }

        Log($"Daily quest progress: {completedCount}/{totalCount} complete & claimed");

        if (allComplete && !isRewardClaimed)
        {
            Log("✓✓✓ ALL DAILY QUESTS COMPLETE! Reward available!");
            MakeRewardAvailable();
        }
    }

    /// <summary>
    /// Make reward available untuk di-claim
    /// </summary>
    void MakeRewardAvailable()
    {
        if (isRewardAvailable)
        {
            Log("Reward already available");
            return;
        }

        // Roll random rewards
        RollRewards();

        isRewardAvailable = true;
        SaveState();

        // Trigger event
        OnRewardAvailable?.Invoke();

        Log($"✓ Daily reward available! Shard: {rolledShardAmount}, Energy: {rolledEnergyAmount}");
    }

    // ========================================
    // REWARD ROLLING
    // ========================================

    /// <summary>
    /// Roll random reward amounts
    /// </summary>
    void RollRewards()
    {
        // Roll shard dengan chance
        float shardRoll = UnityEngine.Random.Range(0f, 100f);
        if (shardRoll < shardChance)
        {
            // Won! Get random shard amount
            rolledShardAmount = UnityEngine.Random.Range(minShardAmount, maxShardAmount + 1);
            Log($"Shard roll: {shardRoll:F1}% < {shardChance}% = WIN! Amount: {rolledShardAmount}");
        }
        else
        {
            rolledShardAmount = 0;
            Log($"Shard roll: {shardRoll:F1}% >= {shardChance}% = LOSE");
        }

        // Roll energy (always gets)
        rolledEnergyAmount = UnityEngine.Random.Range(minEnergyAmount, maxEnergyAmount + 1);
        Log($"Energy roll: {rolledEnergyAmount}");

        SaveState();
    }

    // ========================================
    // CLAIM REWARD
    // ========================================

    /// <summary>
    /// Claim reward dan grant ke player
    /// </summary>
    public void ClaimReward()
    {
        if (isRewardClaimed)
        {
            LogWarning("Reward already claimed today!");
            return;
        }

        if (!isRewardAvailable)
        {
            LogWarning("Reward not available yet! Complete all daily quests first.");
            return;
        }

        Log("=== CLAIMING DAILY REWARD ===");

        // Grant rewards
        GrantRewards();

        // Mark as claimed
        isRewardClaimed = true;
        isRewardAvailable = false;

        // Save claim date
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(PREF_LAST_CLAIM_DATE, today);

        SaveState();

        // Trigger event
        OnRewardClaimed?.Invoke();

        Log("✓ Daily reward claimed successfully!");
    }

    /// <summary>
    /// Grant rewards ke PlayerEconomy
    /// </summary>
    void GrantRewards()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("PlayerEconomy.Instance is null! Cannot grant rewards.");
            return;
        }

        // Grant shard (if any)
        if (rolledShardAmount > 0)
        {
            PlayerEconomy.Instance.AddShards(rolledShardAmount);
            Log($"✓ Granted {rolledShardAmount} Shards");
        }

        // Grant energy
        if (rolledEnergyAmount > 0)
        {
            PlayerEconomy.Instance.AddEnergy(rolledEnergyAmount);
            Log($"✓ Granted {rolledEnergyAmount} Energy");
        }

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }
    }

    // ========================================
    // DAILY RESET SYSTEM
    // ========================================

    /// <summary>
    /// Check apakah sudah waktunya reset (real-time, offline support)
    /// </summary>
    void CheckDailyReset()
    {
        string lastClaimDate = PlayerPrefs.GetString(PREF_LAST_CLAIM_DATE, "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        Log($"CheckDailyReset: lastClaimDate='{lastClaimDate}', today='{today}'");

        if (string.IsNullOrEmpty(lastClaimDate))
        {
            // First time
            Log("First time - no reset needed");
            return;
        }

        if (lastClaimDate != today)
        {
            // New day! Reset
            Log($"✓ NEW DAY DETECTED! Resetting daily reward (last: {lastClaimDate}, now: {today})");
            ResetDailyReward();
        }
        else
        {
            Log("Same day - no reset needed");
        }
    }

    /// <summary>
    /// Reset daily reward state
    /// </summary>
    void ResetDailyReward()
    {
        Log("=== RESETTING DAILY REWARD ===");

        isRewardAvailable = false;
        isRewardClaimed = false;
        rolledShardAmount = 0;
        rolledEnergyAmount = 0;

        SaveState();

        // Trigger event
        OnRewardReset?.Invoke();

        Log("✓ Daily reward reset complete");
    }

    // ========================================
    // SAVE / LOAD
    // ========================================

    void SaveState()
    {
        PlayerPrefs.SetInt(PREF_REWARD_CLAIMED, isRewardClaimed ? 1 : 0);
        PlayerPrefs.SetInt(PREF_ROLLED_SHARD, rolledShardAmount);
        PlayerPrefs.SetInt(PREF_ROLLED_ENERGY, rolledEnergyAmount);
        PlayerPrefs.Save();

        Log($"State saved: claimed={isRewardClaimed}, shard={rolledShardAmount}, energy={rolledEnergyAmount}");
    }

    void LoadState()
    {
        isRewardClaimed = PlayerPrefs.GetInt(PREF_REWARD_CLAIMED, 0) == 1;
        rolledShardAmount = PlayerPrefs.GetInt(PREF_ROLLED_SHARD, 0);
        rolledEnergyAmount = PlayerPrefs.GetInt(PREF_ROLLED_ENERGY, 0);

        // Check if reward was rolled but not claimed
        if (rolledShardAmount > 0 || rolledEnergyAmount > 0)
        {
            if (!isRewardClaimed)
            {
                isRewardAvailable = true;
                Log("✓ Loaded unclaimed reward from previous session");
            }
        }

        Log($"State loaded: claimed={isRewardClaimed}, available={isRewardAvailable}, shard={rolledShardAmount}, energy={rolledEnergyAmount}");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public bool IsRewardAvailable() => isRewardAvailable && !isRewardClaimed;
    public bool IsRewardClaimed() => isRewardClaimed;
    public int GetRolledShardAmount() => rolledShardAmount;
    public int GetRolledEnergyAmount() => rolledEnergyAmount;

    /// <summary>
    /// Force check quest completion (for manual refresh)
    /// </summary>
    public void ForceCheckQuestCompletion()
    {
        Log("Force checking quest completion...");
        CheckAllDailyQuestsComplete();
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[DailyRewardSystem] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[DailyRewardSystem] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[DailyRewardSystem] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Debug: Force Check Quest Completion")]
    void Context_ForceCheck()
    {
        ForceCheckQuestCompletion();
    }

    [ContextMenu("Debug: Force Make Reward Available")]
    void Context_ForceMakeAvailable()
    {
        RollRewards();
        isRewardAvailable = true;
        isRewardClaimed = false;
        SaveState();
        OnRewardAvailable?.Invoke();
        Debug.Log("✓ Forced reward available");
    }

    [ContextMenu("Debug: Force Claim Reward")]
    void Context_ForceClaimReward()
    {
        if (!isRewardAvailable)
        {
            Context_ForceMakeAvailable();
        }
        ClaimReward();
    }

    [ContextMenu("Debug: Reset Daily Reward")]
    void Context_ResetReward()
    {
        ResetDailyReward();
        Debug.Log("✓ Daily reward reset");
    }

    [ContextMenu("Debug: Clear Saved Data")]
    void Context_ClearSavedData()
    {
        PlayerPrefs.DeleteKey(PREF_LAST_CLAIM_DATE);
        PlayerPrefs.DeleteKey(PREF_REWARD_CLAIMED);
        PlayerPrefs.DeleteKey(PREF_ROLLED_SHARD);
        PlayerPrefs.DeleteKey(PREF_ROLLED_ENERGY);
        PlayerPrefs.Save();

        isRewardAvailable = false;
        isRewardClaimed = false;
        rolledShardAmount = 0;
        rolledEnergyAmount = 0;

        Debug.Log("✓ Cleared saved daily reward data");
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== DAILY REWARD STATUS ===");
        Debug.Log($"Reward Available: {isRewardAvailable}");
        Debug.Log($"Reward Claimed: {isRewardClaimed}");
        Debug.Log($"Rolled Shard: {rolledShardAmount}");
        Debug.Log($"Rolled Energy: {rolledEnergyAmount}");
        Debug.Log($"Last Claim Date: {PlayerPrefs.GetString(PREF_LAST_CLAIM_DATE, "NEVER")}");
        Debug.Log($"Today: {DateTime.Now:yyyy-MM-dd}");

        if (QuestManager.Instance != null)
        {
            var dailyQuests = QuestManager.Instance.GetDailyQuests();
            Debug.Log($"\nDaily Quests: {dailyQuests?.Count ?? 0}");

            if (dailyQuests != null)
            {
                int completed = 0;
                foreach (var q in dailyQuests)
                {
                    if (q == null) continue;
                    var p = QuestManager.Instance.GetProgress(q.questId);
                    if (p != null && p.claimed)
                    {
                        completed++;
                        Debug.Log($"  ✓ {q.questId}: CLAIMED");
                    }
                    else
                    {
                        Debug.Log($"  ✗ {q.questId}: {(p != null ? $"{p.progress}/{q.requiredAmount}" : "NO PROGRESS")}");
                    }
                }
                Debug.Log($"Completed & Claimed: {completed}/{dailyQuests.Count}");
            }
        }
        Debug.Log("===========================");
    }
}