using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Prefabs & UI")]
    public GameObject questItemPrefab;
    public Transform contentDaily;
    public Transform contentWeekly;

    [Header("Data")]
    public List<QuestData> dailyQuests = new List<QuestData>();
    public List<QuestData> weeklyPool = new List<QuestData>();
    public int weeklyVisibleCount = 3;

    // Persistence keys
    const string KEY_PROGRESS_JSON = "QuestProgress_v1";
    const string KEY_WEEKLY_ACTIVE = "QuestWeeklyActive_v1";
    const string KEY_LAST_DAILY_RESET = "QuestLastDaily_v1";
    const string KEY_LAST_WEEKLY_RESET = "QuestLastWeekly_v1";

    // Runtime
    Dictionary<string, QuestProgressModel> progressMap = new Dictionary<string, QuestProgressModel>();
    Dictionary<string, QuestItemUI> uiMap = new Dictionary<string, QuestItemUI>();
    List<string> activeWeeklyIds = new List<string>();

    // Event untuk crate system
    public event Action OnQuestClaimed;
    public int TotalClaimedToday { get; private set; }

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
        CheckAndResetDaily();
        CheckAndResetWeekly();
        LoadProgress();
        LoadActiveWeekly();
        PopulateAll();

        // Calculate total claimed
        TotalClaimedToday = GetTotalClaimedCount();
        Debug.Log("[QuestManager] Started, TotalClaimedToday: " + TotalClaimedToday);
    }

    #region Reset Checks

    void CheckAndResetDaily()
    {
        string last = PlayerPrefs.GetString(KEY_LAST_DAILY_RESET, "");
        DateTime lastDate;

        if (string.IsNullOrEmpty(last) || !DateTime.TryParse(last, out lastDate))
        {
            PlayerPrefs.SetString(KEY_LAST_DAILY_RESET, DateTime.Today.ToString("o"));
            PlayerPrefs.Save();
            return;
        }

        if (DateTime.Today > lastDate.Date)
        {
            ResetDailyInternal();
            PlayerPrefs.SetString(KEY_LAST_DAILY_RESET, DateTime.Today.ToString("o"));
            PlayerPrefs.Save();
            Debug.Log("[QuestManager] Auto daily reset triggered");
        }
    }

    void CheckAndResetWeekly()
    {
        string last = PlayerPrefs.GetString(KEY_LAST_WEEKLY_RESET, "");
        DateTime lastWeekStart;

        if (string.IsNullOrEmpty(last) || !DateTime.TryParse(last, out lastWeekStart))
        {
            PlayerPrefs.SetString(KEY_LAST_WEEKLY_RESET, GetStartOfWeek(DateTime.Today).ToString("o"));
            PlayerPrefs.Save();
            return;
        }

        DateTime curWeekStart = GetStartOfWeek(DateTime.Today);
        if (curWeekStart > lastWeekStart)
        {
            ResetWeeklyInternal();
            PlayerPrefs.SetString(KEY_LAST_WEEKLY_RESET, curWeekStart.ToString("o"));
            PlayerPrefs.Save();
            Debug.Log("[QuestManager] Auto weekly reset triggered");
        }
    }

    DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    #endregion

    #region Populate UI

    public void PopulateAll()
    {
        PopulateDaily();
        PopulateWeekly();
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
        if (questItemPrefab == null || contentDaily == null)
        {
            Debug.LogError("[QuestManager] Missing prefab or contentDaily!");
            return;
        }

        ClearChildren(contentDaily);
        uiMap.Clear();

        foreach (var d in dailyQuests)
        {
            if (d == null) continue;

            var go = Instantiate(questItemPrefab, contentDaily);
            go.name = "Quest_" + d.questId;

            var ui = go.GetComponent<QuestItemUI>();
            if (ui == null)
            {
                Debug.LogError("[QuestManager] Prefab missing QuestItemUI!");
                continue;
            }

            var model = GetOrCreateProgress(d.questId);
            ui.Setup(d, model, this);
            uiMap[d.questId] = ui;

            Debug.Log($"[QuestManager] Spawned daily: {d.title} ({d.questId})");
        }
    }

    void PopulateWeekly()
    {
        if (questItemPrefab == null || contentWeekly == null)
        {
            Debug.LogError("[QuestManager] Missing prefab or contentWeekly!");
            return;
        }

        ClearChildren(contentWeekly);

        foreach (var id in activeWeeklyIds)
        {
            var q = FindQuestDef(id);
            if (q == null) continue;

            var go = Instantiate(questItemPrefab, contentWeekly);
            go.name = "Quest_" + id;

            var ui = go.GetComponent<QuestItemUI>();
            if (ui == null) continue;

            var model = GetOrCreateProgress(id);
            ui.Setup(q, model, this);
            uiMap[id] = ui;

            Debug.Log($"[QuestManager] Spawned weekly: {q.title} ({id})");
        }

        if (activeWeeklyIds.Count == 0)
        {
            Debug.LogWarning("[QuestManager] No active weekly quests - run ResetWeekly!");
        }
    }

    #endregion

    #region Progress & Claim

    QuestProgressModel GetOrCreateProgress(string id)
    {
        if (progressMap.TryGetValue(id, out var p) && p != null) return p;
        p = new QuestProgressModel(id, 0, false);
        progressMap[id] = p;
        return p;
    }

    public QuestProgressModel GetProgressModel(string id)
    {
        return GetOrCreateProgress(id);
    }

    public void AddProgress(string questId, int amount)
    {
        var q = FindQuestDef(questId);
        if (q == null)
        {
            Debug.LogWarning($"[QuestManager] AddProgress: Unknown quest {questId}");
            return;
        }

        var model = GetOrCreateProgress(questId);
        if (model.claimed) return;

        model.progress = Mathf.Min(model.progress + amount, q.requiredAmount);
        SaveProgress();
        NotifyUI(questId);
        Debug.Log($"[QuestManager] Progress added to {questId}: {model.progress}/{q.requiredAmount}");
    }

    public void ClaimReward(string questId)
    {
        var q = FindQuestDef(questId);
        if (q == null)
        {
            Debug.LogWarning($"[QuestManager] Claim: Unknown quest {questId}");
            return;
        }

        var model = GetOrCreateProgress(questId);
        if (model.claimed || model.progress < q.requiredAmount)
        {
            Debug.LogWarning($"[QuestManager] Cannot claim {questId}: claimed={model.claimed}, progress={model.progress}/{q.requiredAmount}");
            return;
        }

        model.claimed = true;
        GrantQuestReward(q);

        TotalClaimedToday = GetTotalClaimedCount();  // Update total
        OnQuestClaimed?.Invoke();  // Trigger crate refresh

        SaveProgress();
        NotifyUI(questId);
        Debug.Log($"[QuestManager] Claimed {questId}!");
    }

    void GrantQuestReward(QuestData q)
    {
        switch (q.rewardType)
        {
            case QuestRewardType.Coin:
                PlayerEconomy.Instance?.AddCoins(q.rewardAmount);
                Debug.Log($"  → Granted {q.rewardAmount} coins");
                break;

            case QuestRewardType.Shard:
                PlayerEconomy.Instance?.AddShards(q.rewardAmount);
                Debug.Log($"  → Granted {q.rewardAmount} shards");
                break;

            case QuestRewardType.Energy:
                PlayerEconomy.Instance?.AddEnergy(q.rewardAmount);
                Debug.Log($"  → Granted {q.rewardAmount} energy");
                break;

            case QuestRewardType.Booster:
                if (!string.IsNullOrEmpty(q.rewardBoosterId))
                {
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                        DontDestroyOnLoad(go);
                    }

                    BoosterInventory.Instance?.AddBooster(q.rewardBoosterId, q.rewardAmount);
                    Debug.Log($"  → Granted {q.rewardAmount}x {q.rewardBoosterId}");
                }
                break;

            default:
                break;
        }
    }

    public int GetTotalClaimedCount()
    {
        // Hanya daily (default). Jika ingin include weekly, uncomment baris di bawah
        return progressMap.Values.Count(p => p.claimed && FindQuestDef(p.questId)?.isDaily == true);
        // return progressMap.Values.Count(p => p.claimed);  // Include all (daily + weekly)
    }

    #endregion

    #region Save/Load

    void SaveProgress()
    {
        var list = progressMap.Values.ToArray();
        var wrapper = new QuestProgressList { items = list };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(KEY_PROGRESS_JSON, json);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        progressMap.Clear();
        string json = PlayerPrefs.GetString(KEY_PROGRESS_JSON, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var wrapper = JsonUtility.FromJson<QuestProgressList>(json);
            if (wrapper?.items != null)
            {
                foreach (var p in wrapper.items)
                {
                    if (!string.IsNullOrEmpty(p.questId))
                    {
                        progressMap[p.questId] = p;
                    }
                }
            }
            Debug.Log($"[QuestManager] Loaded {progressMap.Count} quest progress");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QuestManager] Load failed: {ex.Message}");
        }
    }

    void SaveActiveWeekly()
    {
        string json = JsonUtility.ToJson(new WeeklyWrapper { items = activeWeeklyIds.ToArray() });
        PlayerPrefs.SetString(KEY_WEEKLY_ACTIVE, json);
        PlayerPrefs.Save();
    }

    void LoadActiveWeekly()
    {
        activeWeeklyIds.Clear();
        string json = PlayerPrefs.GetString(KEY_WEEKLY_ACTIVE, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var w = JsonUtility.FromJson<WeeklyWrapper>(json);
            if (w?.items != null)
            {
                activeWeeklyIds = new List<string>(w.items);
            }
            Debug.Log($"[QuestManager] Loaded {activeWeeklyIds.Count} active weekly");
        }
        catch
        {
            activeWeeklyIds.Clear();
        }
    }

    [Serializable]
    class WeeklyWrapper
    {
        public string[] items;
    }

    #endregion

    #region Reset Helpers

    void ResetDailyInternal()
    {
        foreach (var d in dailyQuests)
        {
            if (d == null || string.IsNullOrEmpty(d.questId)) continue;
            progressMap[d.questId] = new QuestProgressModel(d.questId, 0, false);
        }

        TotalClaimedToday = 0;
        OnQuestClaimed?.Invoke();  // Refresh crate
        SaveProgress();
        Debug.Log("[QuestManager] Daily quests reset (internal)");
    }

    void ResetWeeklyInternal()
    {
        foreach (var w in weeklyPool)
        {
            if (w == null) continue;
            progressMap[w.questId] = new QuestProgressModel(w.questId, 0, false);
        }

        PickRandomWeekly();
        SaveProgress();
        Debug.Log("[QuestManager] Weekly quests reset (internal)");
    }

    void PickRandomWeekly()
    {
        activeWeeklyIds.Clear();
        var candidates = weeklyPool.Where(w => w != null).ToList();
        candidates.Shuffle();  // Random shuffle (add extension if needed)

        int count = Mathf.Min(weeklyVisibleCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            activeWeeklyIds.Add(candidates[i].questId);
        }

        SaveActiveWeekly();
        Debug.Log($"[QuestManager] Picked {activeWeeklyIds.Count} weekly quests");
    }

    [ContextMenu("Reset Daily")]
    public void ResetDaily()
    {
        ResetDailyInternal();
        PopulateAll();
        Debug.Log("[QuestManager] Manual daily reset executed");
    }

    [ContextMenu("Reset Weekly")]
    public void ResetWeekly()
    {
        ResetWeeklyInternal();
        PopulateAll();
        Debug.Log("[QuestManager] Manual weekly reset executed");
    }

    [ContextMenu("Clear All Data")]
    public void ClearAllData()
    {
        progressMap.Clear();
        activeWeeklyIds.Clear();
        TotalClaimedToday = 0;

        PlayerPrefs.DeleteKey(KEY_PROGRESS_JSON);
        PlayerPrefs.DeleteKey(KEY_WEEKLY_ACTIVE);
        PlayerPrefs.DeleteKey(KEY_LAST_DAILY_RESET);
        PlayerPrefs.DeleteKey(KEY_LAST_WEEKLY_RESET);
        PlayerPrefs.Save();

        PopulateAll();
        Debug.Log("[QuestManager] All data cleared");
    }

    #endregion

    #region Utilities

    QuestData FindQuestDef(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        var f = dailyQuests.FirstOrDefault(x => x != null && x.questId == id);
        if (f != null) return f;

        f = weeklyPool.FirstOrDefault(x => x != null && x.questId == id);
        return f;
    }

    void NotifyUI(string questId)
    {
        if (uiMap.TryGetValue(questId, out var ui) && ui != null)
        {
            ui.OnManagerUpdated();
        }
    }

    #endregion
}

// Extension for shuffle
public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}