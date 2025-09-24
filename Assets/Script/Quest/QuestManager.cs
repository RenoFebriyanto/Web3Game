using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Define quests in Inspector (or leave empty and default list will be used)")]
    public List<QuestDefinition> questDefinitions = new List<QuestDefinition>();

    // runtime states
    Dictionary<string, QuestState> states = new Dictionary<string, QuestState>();

    const string PREF_KEY = "Kulino_QuestStates_v1";
    const string PREF_LAST_DAILY_RESET = "Kulino_LastDailyReset";
    const string PREF_LAST_WEEKLY_RESET = "Kulino_LastWeeklyReset";

    public event Action<QuestState, QuestDefinition> OnQuestChanged; // UI subscribe to refresh individual row
    public event Action OnAllQuestsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (questDefinitions == null || questDefinitions.Count == 0)
        {
            CreateDefaultQuests();
        }

        LoadStates();
        EnsureStatesForDefinitions();
        TryDailyWeeklyResetIfNeeded();
    }

    void CreateDefaultQuests()
    {
        // contoh default (anda bisa ganti lewat inspector)
        questDefinitions = new List<QuestDefinition>() {
            new QuestDefinition { id="daily_login_3", title="Login 3 Hari", description="Login selama 3 hari berturut-turut", conditionType=QuestConditionType.LoginDays, targetValue=3, rewardType=QuestRewardType.Energy, rewardAmount=10, isDaily=true },
            new QuestDefinition { id="daily_play_5min", title="Main 5 Menit", description="Main 5 menit", conditionType=QuestConditionType.PlayTimeSeconds, targetValue=60*5, rewardType=QuestRewardType.Coins, rewardAmount=200, isDaily=true },
            new QuestDefinition { id="daily_coins_2000", title="Kumpulkan 2000 Coins", description="Kumpulkan 2000 coins dalam permainan", conditionType=QuestConditionType.CoinsCollected, targetValue=2000, rewardType=QuestRewardType.Coins, rewardAmount=500, isDaily=true },
            new QuestDefinition { id="weekly_distance_100", title="Jarak 100m", description="Tempuh 100 meter (mingguan)", conditionType=QuestConditionType.DistanceMeters, targetValue=100, rewardType=QuestRewardType.Shards, rewardAmount=10, isDaily=false },
        };
    }

    void EnsureStatesForDefinitions()
    {
        foreach (var d in questDefinitions)
        {
            if (!states.ContainsKey(d.id))
            {
                states[d.id] = new QuestState { id = d.id, progress = 0, completed = false, claimed = false, lastUpdatedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            }
        }
        SaveStates();
        OnAllQuestsChanged?.Invoke();
    }

    #region Persistence
    void LoadStates()
    {
        states.Clear();
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var arr = JsonUtility.FromJson<QuestStateArray>(json);
                if (arr != null && arr.items != null)
                {
                    foreach (var s in arr.items) states[s.id] = s;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[QuestManager] Failed parse quest states: " + e);
            }
        }
    }

    void SaveStates()
    {
        var arr = new QuestStateArray { items = states.Values.ToArray() };
        string json = JsonUtility.ToJson(arr);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    [Serializable]
    class QuestStateArray { public QuestState[] items; }
    #endregion

    #region Reset Daily/Weekly
    void TryDailyWeeklyResetIfNeeded()
    {
        // simple daily reset check using date
        string lastDaily = PlayerPrefs.GetString(PREF_LAST_DAILY_RESET, "");
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (lastDaily != today)
        {
            // perform daily reset for quests with isDaily==true
            foreach (var q in questDefinitions.Where(x => x.isDaily))
            {
                ResetQuestProgress(q.id);
            }
            PlayerPrefs.SetString(PREF_LAST_DAILY_RESET, today);
        }

        // weekly: use iso week number (or just do 7-day check)
        string lastWeekly = PlayerPrefs.GetString(PREF_LAST_WEEKLY_RESET, "");
        string curWeek = GetIsoWeekString(DateTime.UtcNow);
        if (lastWeekly != curWeek)
        {
            foreach (var q in questDefinitions.Where(x => !x.isDaily))
            {
                ResetQuestProgress(q.id);
            }
            PlayerPrefs.SetString(PREF_LAST_WEEKLY_RESET, curWeek);
        }
        PlayerPrefs.Save();
    }

    string GetIsoWeekString(DateTime dt)
    {
        var d = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dt, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return dt.Year + "-W" + d;
    }
    #endregion

    #region Public API for gameplay to update progress
    // Called from GameplayQuestTracker or direct from pickups / rocket
    public void AddProgress(string questId, long delta)
    {
        if (!states.ContainsKey(questId)) return;
        var s = states[questId];
        if (s.claimed || s.completed) return; // don't update if already completed/claimed
        s.progress += delta;
        s.lastUpdatedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var def = questDefinitions.FirstOrDefault(d => d.id == questId);
        if (def != null && s.progress >= def.targetValue)
        {
            s.completed = true;
        }

        states[questId] = s;
        SaveStates();
        OnQuestChanged?.Invoke(s, def);
        OnAllQuestsChanged?.Invoke();
    }

    // Generic: provide shortcuts to add to any quest type: the trackers decide which quests to update
    public void AddPlayTimeSeconds(float seconds)
    {
        foreach (var def in questDefinitions.Where(d => d.conditionType == QuestConditionType.PlayTimeSeconds))
            AddProgress(def.id, (long)Mathf.Ceil(seconds));
    }

    public void AddCoinsCollected(long amount)
    {
        foreach (var def in questDefinitions.Where(d => d.conditionType == QuestConditionType.CoinsCollected))
            AddProgress(def.id, amount);
    }

    public void AddDistanceMeters(float meters)
    {
        foreach (var def in questDefinitions.Where(d => d.conditionType == QuestConditionType.DistanceMeters))
            AddProgress(def.id, (long)Mathf.Ceil(meters));
    }

    // login accumulative days (call once/day from menu)
    public void RegisterLoginDay()
    {
        foreach (var def in questDefinitions.Where(d => d.conditionType == QuestConditionType.LoginDays))
            AddProgress(def.id, 1);
    }
    #endregion

    #region Claiming / rewards
    // Try claim a quest (called by UI). Returns true if successfully claimed.
    public bool TryClaim(string questId)
    {
        if (!states.ContainsKey(questId)) return false;
        var s = states[questId];
        var def = questDefinitions.FirstOrDefault(d => d.id == questId);
        if (def == null) return false;

        if (!s.completed) return false; // not ready
        if (s.claimed) return false;

        // grant reward via PlayerEconomy
        switch (def.rewardType)
        {
            case QuestRewardType.Coins:
                PlayerEconomy.Instance?.AddCoins(def.rewardAmount);
                break;
            case QuestRewardType.Shards:
                PlayerEconomy.Instance?.AddShards((int)def.rewardAmount);
                break;
            case QuestRewardType.Energy:
                PlayerEconomy.Instance?.AddEnergy((int)def.rewardAmount);
                break;
        }

        s.claimed = true;
        states[questId] = s;
        SaveStates();
        OnQuestChanged?.Invoke(s, def);
        OnAllQuestsChanged?.Invoke();
        Debug.Log($"[QuestManager] Claimed {questId} reward {def.rewardType}={def.rewardAmount}");
        return true;
    }
    #endregion

    #region Helpers
    public QuestDefinition GetDefinition(string id) => questDefinitions.FirstOrDefault(d => d.id == id);
    public QuestState GetState(string id) { if (states.ContainsKey(id)) return states[id]; return null; }
    public IEnumerable<(QuestDefinition, QuestState)> GetAll()
    {
        foreach (var d in questDefinitions)
        {
            states.TryGetValue(d.id, out var s);
            if (s == null) s = new QuestState { id = d.id, progress = 0, completed = false, claimed = false };
            yield return (d, s);
        }
    }

    public void ResetQuestProgress(string questId)
    {
        if (!states.ContainsKey(questId)) return;
        var s = states[questId];
        s.progress = 0;
        s.completed = false;
        s.claimed = false;
        s.lastUpdatedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        states[questId] = s;
        SaveStates();
        OnQuestChanged?.Invoke(s, GetDefinition(questId));
    }
    #endregion
}
