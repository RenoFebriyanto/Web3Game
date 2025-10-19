using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// QuestManager: spawn UI based on Scriptable QuestData, persist progress & claimed,
/// provides API: AddProgress(questId,amount), ClaimReward(questId), ResetDaily()
/// Inspector fields:
///  - questItemPrefab -> prefab that contains QuestItemUI component
///  - contentDaily -> Transform for ContentQuestDaily
///  - contentWeekly -> Transform for ContentQuestWK
///  - dailyQuests -> list of QuestData for daily (fixed 5)
///  - weeklyPool -> list of candidate QuestData for weekly (pool)
///  - weeklyVisibleCount -> how many weekly to pick/display
/// </summary>
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

    // persistence keys
    const string KEY_PROGRESS_JSON = "QuestProgress_v1";
    const string KEY_WEEKLY_ACTIVE = "QuestWeeklyActive_v1";
    const string KEY_LAST_DAILY_RESET = "QuestLastDaily_v1";
    const string KEY_LAST_WEEKLY_RESET = "QuestLastWeekly_v1";

    // runtime
    Dictionary<string, QuestProgressModel> progressMap = new Dictionary<string, QuestProgressModel>();
    Dictionary<string, QuestItemUI> uiMap = new Dictionary<string, QuestItemUI>();
    List<string> activeWeeklyIds = new List<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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
    }

    #region Reset checks

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
            ResetDaily();
            PlayerPrefs.SetString(KEY_LAST_DAILY_RESET, DateTime.Today.ToString("o"));
            PlayerPrefs.Save();
        }
    }

    DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
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
            ResetWeekly();
            PlayerPrefs.SetString(KEY_LAST_WEEKLY_RESET, curWeekStart.ToString("o"));
            PlayerPrefs.Save();
        }
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
        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
    }

    void PopulateDaily()
    {
        if (questItemPrefab == null || contentDaily == null) return;
        ClearChildren(contentDaily);
        uiMap.Clear();
        foreach (var d in dailyQuests)
        {
            if (d == null) continue;
            var go = Instantiate(questItemPrefab, contentDaily);
            go.name = "Quest_" + d.questId;
            var ui = go.GetComponent<QuestItemUI>();
            if (ui == null) continue;
            var model = GetOrCreateProgress(d.questId);
            ui.Setup(d, model, this);
            uiMap[d.questId] = ui;
        }
    }

    void PopulateWeekly()
    {
        if (questItemPrefab == null || contentWeekly == null) return;
        ClearChildren(contentWeekly);
        // ensure active weekly picked
        if (activeWeeklyIds.Count == 0) PickRandomWeekly();
        foreach (var id in activeWeeklyIds)
        {
            var def = weeklyPool.FirstOrDefault(x => x != null && x.questId == id);
            if (def == null) continue;
            var go = Instantiate(questItemPrefab, contentWeekly);
            go.name = "Quest_" + def.questId;
            var ui = go.GetComponent<QuestItemUI>();
            if (ui == null) continue;
            var model = GetOrCreateProgress(def.questId);
            ui.Setup(def, model, this);
            uiMap[def.questId] = ui;
        }
    }

    void PickRandomWeekly()
    {
        activeWeeklyIds.Clear();
        var pool = weeklyPool.Where(x => x != null).ToList();
        if (pool.Count == 0) return;
        int count = Mathf.Min(weeklyVisibleCount, pool.Count);
        System.Random rnd = new System.Random();
        var picked = pool.OrderBy(x => rnd.Next()).Take(count).ToList();
        foreach (var p in picked) activeWeeklyIds.Add(p.questId);
        SaveActiveWeekly();
    }

    #endregion

    #region Progress & persistence

    QuestProgressModel GetOrCreateProgress(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return new QuestProgressModel("", 0, false);
        if (!progressMap.TryGetValue(questId, out var m))
        {
            m = new QuestProgressModel(questId, 0, false);
            progressMap[questId] = m;
        }
        return m;
    }

    public QuestProgressModel GetProgressModel(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        progressMap.TryGetValue(questId, out var m);
        return m;
    }

    public int GetProgress(string questId)
    {
        var m = GetProgressModel(questId);
        return m != null ? m.progress : 0;
    }

    public bool IsClaimed(string questId)
    {
        var m = GetProgressModel(questId);
        return m != null && m.claimed;
    }

    public void AddProgress(string questId, int amount)
    {
        if (string.IsNullOrEmpty(questId) || amount == 0) return;
        var def = FindQuestDef(questId);
        if (def == null) return;
        var model = GetOrCreateProgress(questId);
        if (model.claimed && def.isDaily) return; // daily already claimed this cycle -> ignore
        model.progress = Mathf.Clamp(model.progress + amount, 0, def.requiredAmount);
        SaveProgress();
        NotifyUI(questId);
    }

    public void ClaimReward(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        var def = FindQuestDef(questId);
        if (def == null) return;
        var model = GetOrCreateProgress(questId);
        if (model.claimed) return;
        if (model.progress < def.requiredAmount) return;

        // grant reward
        GrantReward(def);

        model.claimed = true;
        SaveProgress();
        NotifyUI(questId);
    }

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
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                        DontDestroyOnLoad(go);
                    }
                    BoosterInventory.Instance.AddBooster(q.rewardBoosterId, q.rewardAmount);
                }
                break;
            case QuestRewardType.None:
            default:
                break;
        }
    }

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
                        progressMap[p.questId] = p;
                }
            }
        }
        catch (Exception ex) { Debug.LogWarning("[QuestManager] LoadProgress error: " + ex.Message); }
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
            if (w?.items != null) activeWeeklyIds = new List<string>(w.items);
        }
        catch { activeWeeklyIds.Clear(); }
    }

    [Serializable]
    class WeeklyWrapper { public string[] items; }

    #endregion

    #region Reset helpers

    /// <summary>
    /// Reset daily: clear progress & claimed for all daily quests (called by DevQuestTester or daily roll)
    /// </summary>
    public void ResetDaily()
    {
        foreach (var d in dailyQuests)
        {
            if (d == null || string.IsNullOrEmpty(d.questId)) continue;
            progressMap[d.questId] = new QuestProgressModel(d.questId, 0, false);
        }
        SaveProgress();
        PopulateAll(); // refresh UI
        Debug.Log("[QuestManager] ResetDaily executed.");
    }

    void ResetWeekly()
    {
        // clear weekly progress for weekly pool
        foreach (var w in weeklyPool)
        {
            if (w == null) continue;
            progressMap[w.questId] = new QuestProgressModel(w.questId, 0, false);
        }
        // pick new subset
        PickRandomWeekly();
        SaveProgress();
        PopulateAll();
        Debug.Log("[QuestManager] ResetWeekly executed.");
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
            // update model reference
            ui.OnManagerUpdated();
        }
    }

    #endregion
}
