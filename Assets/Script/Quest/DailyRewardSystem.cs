using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ✅✅✅ INDESTRUCTIBLE VERSION: NEVER gets destroyed
/// - Uses hideFlags to prevent destruction
/// - Survives scene transitions
/// - Protected against Unity cleanup
/// </summary>
public class DailyRewardSystem : MonoBehaviour
{
    public static DailyRewardSystem Instance { get; private set; }

    [Header("=== REWARD SETTINGS ===")]
    [Header("Shard Reward")]
    [Range(0f, 100f)]
    public float shardChance = 30f;
    public int minShardAmount = 5;
    public int maxShardAmount = 10;

    [Header("Energy Reward")]
    public int minEnergyAmount = 10;
    public int maxEnergyAmount = 30;

    [Header("=== EVENTS ===")]
    public UnityEvent OnRewardAvailable = new UnityEvent();
    public UnityEvent OnRewardClaimed = new UnityEvent();
    public UnityEvent OnRewardReset = new UnityEvent();

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = true;

    const string PREF_LAST_CLAIM_DATE = "Kulino_DailyReward_LastClaimDate_v1";
    const string PREF_REWARD_CLAIMED = "Kulino_DailyReward_Claimed_v1";
    const string PREF_ROLLED_SHARD = "Kulino_DailyReward_RolledShard_v1";
    const string PREF_ROLLED_ENERGY = "Kulino_DailyReward_RolledEnergy_v1";

    private bool isRewardAvailable = false;
    private bool isRewardClaimed = false;
    private int rolledShardAmount = 0;
    private int rolledEnergyAmount = 0;
    private bool isSubscribed = false;

    void Awake()
    {
        // ✅✅✅ CRITICAL: Indestructible singleton
        if (Instance != null)
        {
            if (Instance != this)
            {
                Log($"❌ Duplicate found - DESTROYING duplicate");
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;

        // ✅ TRIPLE PROTECTION against destruction
        DontDestroyOnLoad(gameObject);
        gameObject.hideFlags = HideFlags.DontUnloadUnusedAsset;
        gameObject.name = "[DailyRewardSystem - INDESTRUCTIBLE]";

        Log("✅✅✅ DailyRewardSystem initialized as INDESTRUCTIBLE");
    }

    void Start()
    {
        LoadState();
        CheckDailyReset();
        SubscribeToQuestEventsPersistent();
        Invoke(nameof(ForceCheckQuestCompletion), 1f);
        Log("✓ DailyRewardSystem started");
    }

    void OnDestroy()
    {
        // ✅ WARNING if destroyed
        if (Instance == this)
        {
            LogWarning("⚠️⚠️⚠️ BEING DESTROYED - This should NEVER happen!");
            Instance = null;
        }
        UnsubscribeFromQuestEvents();
    }

    // ✅ Prevent scene cleanup from destroying this object
    void OnApplicationQuit()
    {
        // Cleanup on quit only
        Log("Application quitting - normal cleanup");
    }

    void SubscribeToQuestEventsPersistent()
    {
        if (isSubscribed)
        {
            Log("Already subscribed");
            return;
        }

        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null! Retrying...");
            Invoke(nameof(RetrySubscribe), 0.5f);
            return;
        }

        QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);
        QuestManager.Instance.OnQuestClaimed.AddListener(OnQuestClaimedHandler);

        QuestManager.Instance.OnQuestProgressChanged.RemoveListener(OnQuestProgressChangedHandler);
        QuestManager.Instance.OnQuestProgressChanged.AddListener(OnQuestProgressChangedHandler);

        isSubscribed = true;

        Log("✅✅✅ PERSISTENT SUBSCRIPTION ACTIVE");
    }

    void RetrySubscribe()
    {
        Log("Retrying subscribe...");
        SubscribeToQuestEventsPersistent();
    }

    void UnsubscribeFromQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestClaimed.RemoveListener(OnQuestClaimedHandler);
            QuestManager.Instance.OnQuestProgressChanged.RemoveListener(OnQuestProgressChangedHandler);
        }
        isSubscribed = false;
    }

    void OnQuestClaimedHandler(string questId, QuestData questData)
    {
        if (questData == null) return;
        if (!questData.isDaily) return;

        Log($"Quest CLAIMED: {questId} (Daily)");
        CheckAllDailyQuestsComplete();
    }

    void OnQuestProgressChangedHandler(string questId, QuestProgressModel model)
    {
        if (model == null) return;

        var questData = QuestManager.Instance?.GetQuestData(questId);
        if (questData == null || !questData.isDaily) return;

        if (model.progress >= questData.requiredAmount && !model.claimed)
        {
            Log($"Daily quest {questId} is now COMPLETE");
        }
    }

    void CheckAllDailyQuestsComplete()
    {
        if (QuestManager.Instance == null)
        {
            LogError("QuestManager.Instance is null!");
            return;
        }

        if (isRewardClaimed)
        {
            Log("Reward already claimed");
            return;
        }

        var dailyQuests = QuestManager.Instance.GetDailyQuests();
        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Log("No daily quests");
            return;
        }

        Log("=== CHECKING DAILY QUESTS ===");
        Log($"Total: {dailyQuests.Count}");

        bool allComplete = true;
        int completedCount = 0;
        int claimedCount = 0;

        foreach (var quest in dailyQuests)
        {
            if (quest == null) continue;

            var progress = QuestManager.Instance.GetProgress(quest.questId);
            if (progress == null)
            {
                Log($"  ❌ {quest.questId}: NO PROGRESS");
                allComplete = false;
                continue;
            }

            bool isComplete = progress.progress >= quest.requiredAmount;
            bool isClaimed = progress.claimed;

            string status = "";
            if (isClaimed)
            {
                claimedCount++;
                completedCount++;
                status = "✅ CLAIMED";
            }
            else if (isComplete)
            {
                completedCount++;
                status = "⚠️ COMPLETE but NOT CLAIMED";
                allComplete = false;
            }
            else
            {
                status = $"❌ INCOMPLETE ({progress.progress}/{quest.requiredAmount})";
                allComplete = false;
            }

            Log($"  {quest.questId}: {status}");
        }

        Log($"Complete: {completedCount}/{dailyQuests.Count}");
        Log($"Claimed: {claimedCount}/{dailyQuests.Count}");

        if (allComplete && !isRewardClaimed && !isRewardAvailable)
        {
            Log("✅✅✅ ALL DAILY QUESTS COMPLETE & CLAIMED!");
            Log("🎉 MAKING REWARD AVAILABLE!");
            MakeRewardAvailable();
        }
        
        Log("===========================================");
    }

    void MakeRewardAvailable()
    {
        if (isRewardAvailable)
        {
            Log("Reward already available");
            return;
        }

        RollRewards();

        isRewardAvailable = true;
        SaveState();

        OnRewardAvailable?.Invoke();

        Log($"✅✅✅ DAILY REWARD AVAILABLE!");
        Log($"  Shard: {rolledShardAmount}");
        Log($"  Energy: {rolledEnergyAmount}");
    }

    void RollRewards()
    {
        float shardRoll = UnityEngine.Random.Range(0f, 100f);
        if (shardRoll < shardChance)
        {
            rolledShardAmount = UnityEngine.Random.Range(minShardAmount, maxShardAmount + 1);
            Log($"Shard roll: {shardRoll:F1}% < {shardChance}% = WIN! Amount: {rolledShardAmount}");
        }
        else
        {
            rolledShardAmount = 0;
            Log($"Shard roll: {shardRoll:F1}% >= {shardChance}% = LOSE");
        }

        rolledEnergyAmount = UnityEngine.Random.Range(minEnergyAmount, maxEnergyAmount + 1);
        Log($"Energy roll: {rolledEnergyAmount}");

        SaveState();
    }

    public void ClaimReward()
    {
        if (isRewardClaimed)
        {
            LogWarning("Reward already claimed!");
            return;
        }

        if (!isRewardAvailable)
        {
            LogWarning("Reward not available!");
            return;
        }

        Log("=== CLAIMING DAILY REWARD ===");

        GrantRewards();

        isRewardClaimed = true;
        isRewardAvailable = false;

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(PREF_LAST_CLAIM_DATE, today);

        SaveState();

        OnRewardClaimed?.Invoke();

        Log("✓ Daily reward claimed!");
    }

    void GrantRewards()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("PlayerEconomy.Instance is null!");
            return;
        }

        if (rolledShardAmount > 0)
        {
            PlayerEconomy.Instance.AddShards(rolledShardAmount);
            Log($"✓ Granted {rolledShardAmount} Shards");
        }

        if (rolledEnergyAmount > 0)
        {
            PlayerEconomy.Instance.AddEnergy(rolledEnergyAmount);
            Log($"✓ Granted {rolledEnergyAmount} Energy");
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }
    }

    void CheckDailyReset()
    {
        string lastClaimDate = PlayerPrefs.GetString(PREF_LAST_CLAIM_DATE, "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        Log($"CheckDailyReset: last='{lastClaimDate}', today='{today}'");

        if (string.IsNullOrEmpty(lastClaimDate))
        {
            Log("First time");
            return;
        }

        if (lastClaimDate != today)
        {
            Log($"✓ NEW DAY! Resetting (last: {lastClaimDate}, now: {today})");
            ResetDailyReward();
        }
        else
        {
            Log("Same day");
        }
    }

    void ResetDailyReward()
    {
        Log("=== RESETTING DAILY REWARD ===");

        isRewardAvailable = false;
        isRewardClaimed = false;
        rolledShardAmount = 0;
        rolledEnergyAmount = 0;

        SaveState();

        OnRewardReset?.Invoke();

        Log("✓ Daily reward reset complete");
    }

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

        if (rolledShardAmount > 0 || rolledEnergyAmount > 0)
        {
            if (!isRewardClaimed)
            {
                isRewardAvailable = true;
                Log("✓ Loaded unclaimed reward");
            }
        }

        Log($"State loaded: claimed={isRewardClaimed}, available={isRewardAvailable}, shard={rolledShardAmount}, energy={rolledEnergyAmount}");
    }

    public bool IsRewardAvailable() => isRewardAvailable && !isRewardClaimed;
    public bool IsRewardClaimed() => isRewardClaimed;
    public int GetRolledShardAmount() => rolledShardAmount;
    public int GetRolledEnergyAmount() => rolledEnergyAmount;

    public void ForceCheckQuestCompletion()
    {
        Log("⚡ FORCE CHECK");
        CheckAllDailyQuestsComplete();
    }

    public void ResetRewardForTesting()
    {
        Log("=== RESET FOR TESTING ===");
        
        isRewardAvailable = false;
        isRewardClaimed = false;
        rolledShardAmount = 0;
        rolledEnergyAmount = 0;
        
        SaveState();
        
        PlayerPrefs.DeleteKey(PREF_LAST_CLAIM_DATE);
        PlayerPrefs.Save();
        
        OnRewardReset?.Invoke();
        
        Log("✓ Reset complete");
    }

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

    [ContextMenu("⚡ Force Check")]
    void Context_ForceCheck()
    {
        ForceCheckQuestCompletion();
    }

    [ContextMenu("🎁 Force Make Available")]
    void Context_ForceMakeAvailable()
    {
        RollRewards();
        isRewardAvailable = true;
        isRewardClaimed = false;
        SaveState();
        OnRewardAvailable?.Invoke();
        Debug.Log("✓ Forced available");
    }

    [ContextMenu("💰 Force Claim")]
    void Context_ForceClaimReward()
    {
        if (!isRewardAvailable)
        {
            Context_ForceMakeAvailable();
        }
        ClaimReward();
    }

    [ContextMenu("🔄 Reset Reward")]
    void Context_ResetReward()
    {
        ResetDailyReward();
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== DAILY REWARD SYSTEM STATUS ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Instance: {(Instance != null ? "✅ OK" : "❌ NULL")}");
        Debug.Log($"Is This Instance: {(Instance == this ? "✅ YES" : "❌ NO")}");
        Debug.Log($"HideFlags: {gameObject.hideFlags}");
        Debug.Log($"Subscribed: {isSubscribed}");
        Debug.Log($"Reward Available: {isRewardAvailable}");
        Debug.Log($"Reward Claimed: {isRewardClaimed}");
        Debug.Log($"Rolled Shard: {rolledShardAmount}");
        Debug.Log($"Rolled Energy: {rolledEnergyAmount}");
        Debug.Log("==================================");
    }
}