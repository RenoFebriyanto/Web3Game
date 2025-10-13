// QuestManager.cs
// Put this in Assets/Script/Quest/QuestManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central quest manager: load/save progress & claimed state, AddProgress, ClaimReward, ResetDaily.
/// Exposes events so UI (QuestItemUI, QuestChestController etc.) can update.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // Pref keys
    const string PREF_PROGRESS_PREFIX = "QProg_";   // + questId -> int
    const string PREF_CLAIMED_PREFIX = "QClaim_";   // + questId -> 1

    [Header("Quest lists (assign in inspector)")]
    public List<QuestData> dailyQuests = new List<QuestData>();
    public List<QuestData> weeklyQuests = new List<QuestData>();

    // runtime dictionaries
    Dictionary<string, int> progress = new Dictionary<string, int>();     // questId -> current amount
    HashSet<string> claimed = new HashSet<string>();                     // questId claimed (for current cycle)

    // events:
    // called whenever a quest progress changed -> (questId, newProgress, requiredAmount)
    public event Action<string, int, int> OnQuestProgressChanged;
    // called when a quest is claimed -> (questId)
    public event Action<string> OnQuestClaimed;
    // called when the total claimed count (for chest etc) changes -> (claimedCount)
    public event Action<int> OnClaimedCountChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[QuestManager] Duplicate instance found - destroying this.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadAll();
    }

    #region Public API

    /// <summary>
    /// Add progress to quest by id. If quest not found, no-op.
    /// </summary>
    public void AddProgress(string questId, int amount)
    {
        if (string.IsNullOrEmpty(questId) || amount == 0) return;
        var q = FindQuestById(questId);
        if (q == null)
        {
            // not configured - ignore quietly but log
            Debug.LogWarning($"[QuestManager] AddProgress: quest {questId} not found.");
            return;
        }

        // don't add progress if already claimed and it's a daily quest (claimed means done this cycle)
        if (claimed.Contains(questId) && q.isDaily)
        {
            // nothing to do
            return;
        }

        int cur = GetProgress(questId);
        long next = (long)cur + amount;
        if (next < 0) next = 0;
        int newVal = (int)Math.Min(next, int.MaxValue);
        progress[questId] = newVal;
        SaveProgress(questId, newVal);

        OnQuestProgressChanged?.Invoke(questId, newVal, q.requiredAmount);

        // if reached requirement auto-notify (UI can enable claim)
        if (newVal >= q.requiredAmount)
        {
            Debug.Log($"[QuestManager] Quest {questId} reached requirement ({newVal}/{q.requiredAmount}).");
        }
    }

    /// <summary>
    /// Claim reward for quest (if completed and not claimed). Returns true if success.
    /// </summary>
    public bool ClaimReward(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        var q = FindQuestById(questId);
        if (q == null) { Debug.LogWarning($"[QuestManager] ClaimReward: quest {questId} not found."); return false; }

        if (claimed.Contains(questId))
        {
            Debug.Log($"[QuestManager] ClaimReward: quest {questId} already claimed.");
            return false;
        }

        int cur = GetProgress(questId);
        if (cur < q.requiredAmount)
        {
            Debug.Log($"[QuestManager] ClaimReward: quest {questId} not complete ({cur}/{q.requiredAmount}).");
            return false;
        }

        // grant reward
        GrantReward(q);

        // mark claimed
        claimed.Add(questId);
        SaveClaimed(questId);
        OnQuestClaimed?.Invoke(questId);

        // notify change in claimed count (for chest)
        OnClaimedCountChanged?.Invoke(GetClaimedCount());

        Debug.Log($"[QuestManager] Quest {questId} claimed and reward granted.");

        return true;
    }

    /// <summary>
    /// Reset daily quests: clear progress for daily quests and clear claimed for daily ones.
    /// Call this at daily reset time.
    /// </summary>
    public void ResetDaily()
    {
        // clear progress and claimed for daily
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            progress[q.questId] = 0;
            PlayerPrefs.DeleteKey(PREF_PROGRESS_PREFIX + q.questId);
            PlayerPrefs.DeleteKey(PREF_CLAIMED_PREFIX + q.questId);
        }

        // remove daily claimed from in-memory set
        var toRemove = new List<string>();
        foreach (var id in claimed)
        {
            var q = FindQuestById(id);
            if (q != null && q.isDaily) toRemove.Add(id);
        }
        foreach (var r in toRemove) claimed.Remove(r);

        PlayerPrefs.Save();

        // notify UI to refresh
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            OnQuestProgressChanged?.Invoke(q.questId, 0, q.requiredAmount);
        }

        OnClaimedCountChanged?.Invoke(GetClaimedCount());
        Debug.Log("[QuestManager] ResetDaily executed.");
    }

    public int GetProgress(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return 0;
        if (progress.TryGetValue(questId, out var v)) return v;
        // fallback to saved
        int saved = PlayerPrefs.GetInt(PREF_PROGRESS_PREFIX + questId, 0);
        progress[questId] = saved;
        return saved;
    }

    public bool IsClaimed(string questId) => claimed.Contains(questId);

    public int GetClaimedCount() => claimed.Count;

    #endregion

    #region Persistence & load helpers

    void LoadAll()
    {
        progress.Clear();
        claimed.Clear();

        // load daily
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            int p = PlayerPrefs.GetInt(PREF_PROGRESS_PREFIX + q.questId, 0);
            progress[q.questId] = p;
            int c = PlayerPrefs.GetInt(PREF_CLAIMED_PREFIX + q.questId, 0);
            if (c != 0) claimed.Add(q.questId);
            // notify UI initial state
            OnQuestProgressChanged?.Invoke(q.questId, p, q.requiredAmount);
        }

        // load weekly
        foreach (var q in weeklyQuests)
        {
            if (q == null) continue;
            int p = PlayerPrefs.GetInt(PREF_PROGRESS_PREFIX + q.questId, 0);
            progress[q.questId] = p;
            int c = PlayerPrefs.GetInt(PREF_CLAIMED_PREFIX + q.questId, 0);
            if (c != 0) claimed.Add(q.questId);
            OnQuestProgressChanged?.Invoke(q.questId, p, q.requiredAmount);
        }

        OnClaimedCountChanged?.Invoke(GetClaimedCount());
        Debug.Log("[QuestManager] LoadAll finished.");
    }

    void SaveProgress(string questId, int value)
    {
        PlayerPrefs.SetInt(PREF_PROGRESS_PREFIX + questId, value);
        PlayerPrefs.Save();
    }

    void SaveClaimed(string questId)
    {
        PlayerPrefs.SetInt(PREF_CLAIMED_PREFIX + questId, 1);
        PlayerPrefs.Save();
    }

    QuestData FindQuestById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var q in dailyQuests) if (q != null && q.questId == id) return q;
        foreach (var q in weeklyQuests) if (q != null && q.questId == id) return q;
        return null;
    }

    #endregion

    #region Reward granting helpers

    void GrantReward(QuestData q)
    {
        if (q == null) return;
        switch (q.rewardType)
        {
            case QuestRewardType.Coin:
                PlayerEconomy.Instance?.AddCoins(q.rewardAmount);
                break;
            case QuestRewardType.Shard:
                PlayerEconomy.Instance?.AddShards(q.rewardAmount);
                break;
            case QuestRewardType.Energy:
                PlayerEconomy.Instance?.AddEnergy(q.rewardAmount);
                break;
            case QuestRewardType.Booster:
                if (!string.IsNullOrEmpty(q.rewardBoosterId))
                {
                    // ensure BoosterInventory exists
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                    }
                    BoosterInventory.Instance.AddBooster(q.rewardBoosterId, q.rewardAmount);
                }
                break;
            case QuestRewardType.None:
            default:
                break;
        }
    }

    #endregion
}
