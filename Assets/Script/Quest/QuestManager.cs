// QuestManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton manager untuk quest (daily + weekly).
/// - Assign daftar QuestData untuk daily (fixed) dan weeklyPool (pool random).
/// - Assign prefab QuestItemUI dan content containers (Daily/Weekly) di Inspector.
/// - Persist progress ke PlayerPrefs (json).
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // --- Inspector ---
    [Header("Prefabs & UI")]
    public QuestItemUI questItemPrefab;        // prefab UI item (assign prefab QuestItem)
    public Transform contentDaily;            // ContentQuestDaily (where daily entries will be instantiated)
    public Transform contentWeekly;           // ContentQuestWeekly (optional - if you have weekly column)
    [Tooltip("Jika weeklyVisibleCount > 0 maka akan menampilkan random X dari weeklyPool")]
    public int weeklyVisibleCount = 3;

    [Header("Quest lists (assign in inspector)")]
    public List<QuestData> dailyQuests = new List<QuestData>();
    public List<QuestData> weeklyPool = new List<QuestData>();

    // --- persistence ---
    const string PREF_KEY = "QuestProgress_v1";

    // runtime
    Dictionary<string, QuestProgressModel> progressMap = new Dictionary<string, QuestProgressModel>();
    // map UI item instances for quick refresh
    Dictionary<string, QuestItemUI> uiMap = new Dictionary<string, QuestItemUI>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        LoadProgress();
        PopulateAll();
    }

    #region Populate UI
    public void PopulateAll()
    {
        PopulateDaily();
        PopulateWeeklyInitial();
    }

    void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    void PopulateDaily()
    {
        if (questItemPrefab == null || contentDaily == null) return;

        ClearChildren(contentDaily);
        uiMap.Clear();

        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            var go = Instantiate(questItemPrefab.gameObject, contentDaily);
            var ui = go.GetComponent<QuestItemUI>();
            var model = GetOrCreateProgress(q.questId);
            ui.Setup(q, model, this);
            uiMap[q.questId] = ui;
        }
    }

    void PopulateWeeklyInitial()
    {
        if (questItemPrefab == null || contentWeekly == null) return;

        ClearChildren(contentWeekly);

        // pick random unique entries from weeklyPool
        var pool = weeklyPool.Where(x => x != null).ToList();
        var pick = new List<QuestData>();

        if (weeklyVisibleCount <= 0 || pool.Count == 0) return;

        var rnd = new System.Random();
        var indices = Enumerable.Range(0, pool.Count).OrderBy(x => rnd.Next()).Take(Math.Min(weeklyVisibleCount, pool.Count)).ToList();
        foreach (var i in indices) pick.Add(pool[i]);

        foreach (var q in pick)
        {
            var go = Instantiate(questItemPrefab.gameObject, contentWeekly);
            var ui = go.GetComponent<QuestItemUI>();
            var model = GetOrCreateProgress(q.questId);
            ui.Setup(q, model, this);
            uiMap[q.questId] = ui;
        }
    }

    // When a weekly quest is completed and claimed, we can spawn a new random one to replace it
    void ReplaceWeeklyWithRandom(string oldQuestId)
    {
        if (contentWeekly == null || weeklyPool == null || weeklyPool.Count == 0) return;

        // find the UI element of oldQuestId and remove it
        if (uiMap.TryGetValue(oldQuestId, out var oldUi))
        {
            Destroy(oldUi.gameObject);
            uiMap.Remove(oldQuestId);
        }

        // pick a random quest from weeklyPool that's not currently shown
        var shownIds = new HashSet<string>(uiMap.Keys);
        var candidates = weeklyPool.Where(d => d != null && !shownIds.Contains(d.questId)).ToList();
        if (candidates.Count == 0) return;

        var rnd = UnityEngine.Random.Range(0, candidates.Count);
        var pick = candidates[rnd];

        var go = Instantiate(questItemPrefab.gameObject, contentWeekly);
        var ui = go.GetComponent<QuestItemUI>();
        var model = GetOrCreateProgress(pick.questId);
        ui.Setup(pick, model, this);
        uiMap[pick.questId] = ui;
    }
    #endregion

    #region Progress model handling + persistence
    QuestProgressModel GetOrCreateProgress(string questId)
    {
        if (string.IsNullOrEmpty(questId)) throw new ArgumentException("questId empty");
        if (!progressMap.TryGetValue(questId, out var m))
        {
            m = new QuestProgressModel(questId, 0, false);
            progressMap[questId] = m;
        }
        return m;
    }

    void SaveProgress()
    {
        var list = progressMap.Values.ToList();
        var wrapper = new QuestProgressList { items = list };
        var json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[QuestManager] Saved progress (" + list.Count + ")");
    }

    void LoadProgress()
    {
        progressMap.Clear();
        var json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var wrapper = JsonUtility.FromJson<QuestProgressList>(json);
            if (wrapper != null && wrapper.items != null)
            {
                foreach (var p in wrapper.items)
                {
                    if (!string.IsNullOrEmpty(p.questId)) progressMap[p.questId] = p;
                }
            }
            Debug.Log("[QuestManager] Loaded progress count=" + progressMap.Count);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[QuestManager] Failed to load progress: " + ex);
        }
    }

    [Serializable]
    class QuestProgressList { public List<QuestProgressModel> items = new List<QuestProgressModel>(); }
    #endregion

    #region Public API (AddProgress / Claim)
    /// <summary>
    /// Add progress to quest (call from game systems when player did something).
    /// </summary>
    public void AddProgress(string questId, int amount)
    {
        if (string.IsNullOrEmpty(questId) || amount == 0) return;
        var model = GetOrCreateProgress(questId);

        // find quest definition (search in daily+weeklyPool)
        var def = FindQuestData(questId);
        if (def == null)
        {
            Debug.LogWarning("[QuestManager] AddProgress: quest definition not found for id=" + questId);
            return;
        }

        if (model.claimed)
        {
            // already claimed -> ignore
            return;
        }

        model.current = Mathf.Clamp(model.current + amount, 0, def.requiredAmount);
        UpdateUIForQuest(questId, def, model);

        if (model.current >= def.requiredAmount)
        {
            Debug.Log($"[QuestManager] Quest {questId} ready to claim");
        }

        SaveProgress();
    }

    /// <summary>
    /// Player requests to claim reward for questId. Returns true if success.
    /// </summary>
    public bool ClaimReward(string questId)
    {
        if (!progressMap.TryGetValue(questId, out var model)) return false;
        if (model.claimed) return false;

        var def = FindQuestData(questId);
        if (def == null) return false;
        if (model.current < def.requiredAmount) return false;

        // give reward
        switch (def.rewardType)
        {
            case QuestRewardType.Coin:
                PlayerEconomy.Instance?.AddCoins(def.rewardAmount);
                break;
            case QuestRewardType.Shard:
                PlayerEconomy.Instance?.AddShards(def.rewardAmount);
                break;
            case QuestRewardType.Energy:
                PlayerEconomy.Instance?.AddEnergy(def.rewardAmount);
                break;
            case QuestRewardType.Booster:
                if (!string.IsNullOrEmpty(def.rewardBoosterId))
                {
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                    }
                    BoosterInventory.Instance.AddBooster(def.rewardBoosterId, def.rewardAmount);
                }
                break;
            default:
                break;
        }

        model.claimed = true;
        UpdateUIForQuest(questId, def, model);
        SaveProgress();

        // If this was a weekly quest, replace it with a new random one
        if (!def.isDaily)
        {
            ReplaceWeeklyWithRandom(questId);
        }

        Debug.Log($"[QuestManager] Claimed {questId} -> {def.rewardType} x{def.rewardAmount}");
        return true;
    }
    #endregion

    #region Helpers + UI refresh
    QuestData FindQuestData(string questId)
    {
        var d = dailyQuests.FirstOrDefault(x => x != null && x.questId == questId);
        if (d != null) return d;
        d = weeklyPool.FirstOrDefault(x => x != null && x.questId == questId);
        return d;
    }

    void UpdateUIForQuest(string questId, QuestData def, QuestProgressModel model)
    {
        if (uiMap.TryGetValue(questId, out var ui))
        {
            ui.Refresh(def, model, this);
        }
    }

    /// <summary>
    /// Refresh all UI (call after loading / reset)
    /// </summary>
    public void RefreshAllUI()
    {
        foreach (var kv in uiMap)
        {
            var id = kv.Key;
            var ui = kv.Value;
            var def = FindQuestData(id);
            var model = GetOrCreateProgress(id);
            if (def != null) ui.Refresh(def, model, this);
        }
    }
    #endregion

    #region Dev / Reset functions
    /// <summary>
    /// Reset all daily quests progress (call from dev UI). Keeps weekly unchanged.
    /// </summary>
    public void ResetDailyNow()
    {
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            progressMap[q.questId] = new QuestProgressModel(q.questId, 0, false);
        }
        SaveProgress();
        RefreshAllUI();
        ResetDailyNow();
        Debug.Log("[QuestManager] Daily quests reset");
    }

    /// <summary>
    /// Completely clear saved quest progress (dev).
    /// </summary>
    public void ClearAllProgress()
    {
        progressMap.Clear();
        PlayerPrefs.DeleteKey(PREF_KEY);
        RefreshAllUI();
        Debug.Log("[QuestManager] Cleared all quest progress");
    }
    #endregion
}
