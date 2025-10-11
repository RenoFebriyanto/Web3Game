using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages daily and weekly quests: spawn UI, persist progress/claimed, grant rewards.
/// Wire references in inspector:
///  - questItemPrefab = QuestItem prefab (contains QuestItemUI)
///  - contentDaily = ContentQuestDaily (Transform parent)
///  - dailyQuests = array of QuestData ScriptableObjects for daily quests
///  - weeklyPool = array of QuestData candidates for weekly (optional)
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Prefabs & UI")]
    public GameObject questItemPrefab;     // prefab with QuestItemUI
    public Transform contentDaily;         // ContentQuestDaily transform (where items will be instantiated)

    [Header("Data")]
    public QuestData[] dailyQuests;
    public QuestData[] weeklyPool;

    // runtime storage
    Dictionary<string, int> progress = new Dictionary<string, int>();
    HashSet<string> claimed = new HashSet<string>();
    Dictionary<string, QuestItemUI> spawnedUI = new Dictionary<string, QuestItemUI>();

    const string PREF_PREFIX_PROG = "QuestProg_";
    const string PREF_PREFIX_CLAIM = "QuestClaimed_";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // optionally DontDestroyOnLoad if you want persistent manager:
        // DontDestroyOnLoad(gameObject);

        LoadAll();
    }

    void Start()
    {
        PopulateDaily();
    }

    #region persistence
    void LoadAll()
    {
        progress.Clear();
        claimed.Clear();

        // load progress for all known quest ids (daily + weekly pool)
        IEnumerable<QuestData> all = GetAllQuestData();
        foreach (var q in all)
        {
            if (q == null || string.IsNullOrEmpty(q.questId)) continue;
            int p = PlayerPrefs.GetInt(PREF_PREFIX_PROG + q.questId, 0);
            progress[q.questId] = p;
            int c = PlayerPrefs.GetInt(PREF_PREFIX_CLAIM + q.questId, 0);
            if (c == 1) claimed.Add(q.questId);
        }
    }

    void SaveProgress(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        PlayerPrefs.SetInt(PREF_PREFIX_PROG + questId, GetProgress(questId));
        PlayerPrefs.Save();
    }

    void SaveClaimed(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        PlayerPrefs.SetInt(PREF_PREFIX_CLAIM + questId, IsClaimed(questId) ? 1 : 0);
        PlayerPrefs.Save();
    }
    #endregion

    IEnumerable<QuestData> GetAllQuestData()
    {
        if (dailyQuests != null) foreach (var q in dailyQuests) if (q != null) yield return q;
        if (weeklyPool != null) foreach (var q in weeklyPool) if (q != null) yield return q;
    }

    #region spawn UI
    public void PopulateDaily()
    {
        if (questItemPrefab == null) { Debug.LogWarning("[QuestManager] questItemPrefab missing"); return; }
        if (contentDaily == null) { Debug.LogWarning("[QuestManager] contentDaily missing"); return; }

        // clear children
        for (int i = contentDaily.childCount - 1; i >= 0; i--) DestroyImmediate(contentDaily.GetChild(i).gameObject);
        spawnedUI.Clear();

        if (dailyQuests == null) return;
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            var go = Instantiate(questItemPrefab, contentDaily);
            go.name = "Quest_" + q.questId;
            var ui = go.GetComponent<QuestItemUI>();
            if (ui != null)
            {
                ui.Setup(q, this);
                spawnedUI[q.questId] = ui;
            }
        }
    }
    #endregion

    #region public API (used by QuestItemUI)
    public int GetProgress(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return 0;
        if (progress.TryGetValue(questId, out var v)) return v;
        return 0;
    }

    public bool IsClaimed(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;
        return claimed.Contains(questId);
    }

    /// <summary>
    /// Add progress to quest. If quest reaches required amount, UI will reflect ready-to-claim.
    /// </summary>
    public void AddProgress(string questId, int amount)
    {
        if (string.IsNullOrEmpty(questId) || amount == 0) return;
        // find quest meta to get requiredAmount
        var q = FindQuestData(questId);
        if (q == null) return;

        int cur = GetProgress(questId);
        int nxt = Mathf.Clamp(cur + amount, 0, q.requiredAmount);
        progress[questId] = nxt;
        SaveProgress(questId);
        NotifyUI(questId);
    }

    /// <summary>
    /// Claim reward for questId if progress enough and not already claimed.
    /// Grants to PlayerEconomy or BoosterInventory depending on reward type.
    /// </summary>
    public void ClaimReward(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        var q = FindQuestData(questId);
        if (q == null) return;
        if (IsClaimed(questId)) return;
        if (GetProgress(questId) < q.requiredAmount) return;

        // grant
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

        // mark claimed
        claimed.Add(questId);
        SaveClaimed(questId);

        NotifyUI(questId);
    }
    #endregion

    void NotifyUI(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        if (spawnedUI.TryGetValue(questId, out var ui) && ui != null)
        {
            ui.OnManagerUpdated();
        }
    }

    QuestData FindQuestData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (dailyQuests != null) foreach (var q in dailyQuests) if (q != null && q.questId == id) return q;
        if (weeklyPool != null) foreach (var q in weeklyPool) if (q != null && q.questId == id) return q;
        return null;
    }

    #region dev / daily reset helpers
    [ContextMenu("ResetAllProgressAndClaims")]
    public void ResetAll()
    {
        // remove keys for all known quest ids
        foreach (var q in GetAllQuestData())
        {
            PlayerPrefs.DeleteKey(PREF_PREFIX_PROG + q.questId);
            PlayerPrefs.DeleteKey(PREF_PREFIX_CLAIM + q.questId);
        }
        PlayerPrefs.Save();
        LoadAll();
        foreach (var kv in spawnedUI) kv.Value.OnManagerUpdated();
    }

    [ContextMenu("ResetDailyToZero")]
    public void ResetDaily()
    {
        if (dailyQuests == null) return;
        foreach (var q in dailyQuests)
        {
            progress[q.questId] = 0;
            claimed.Remove(q.questId);
            SaveProgress(q.questId);
            SaveClaimed(q.questId);
            if (spawnedUI.TryGetValue(q.questId, out var ui)) ui.OnManagerUpdated();
        }
    }
    #endregion
}
