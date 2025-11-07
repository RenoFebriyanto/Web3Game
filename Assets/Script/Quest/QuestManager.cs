using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UPDATED: QuestManager dengan Event System untuk sinkronisasi real-time
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // ========================================
    // EVENTS untuk sinkronisasi UI
    // ========================================
    [System.Serializable]
    public class QuestProgressEvent : UnityEvent<string, QuestProgressModel> { }

    [System.Serializable]
    public class QuestClaimedEvent : UnityEvent<string, QuestData> { }

    [Header("Events")]
    public QuestProgressEvent OnQuestProgressChanged = new QuestProgressEvent();
    public QuestClaimedEvent OnQuestClaimed = new QuestClaimedEvent();
    public UnityEvent OnQuestsRefreshed = new UnityEvent();

    // Prefab & UI (assign in Inspector)
    [Header("Prefabs & UI")]
    public GameObject questItemPrefab;
    public Transform contentDaily;
    public Transform contentWeekly;

    [Header("Data")]
    public List<QuestData> dailyQuests = new List<QuestData>();
    public List<QuestData> weeklyPool = new List<QuestData>();
    public int weeklyVisibleCount = 3;

    const string PREF_KEY = "QuestProgress_v1";

    Dictionary<string, QuestProgressModel> progressMap = new Dictionary<string, QuestProgressModel>();
    Dictionary<string, QuestItemUI> spawnedUI = new Dictionary<string, QuestItemUI>();
    System.Random rnd = new System.Random();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize events if null
        if (OnQuestProgressChanged == null) OnQuestProgressChanged = new QuestProgressEvent();
        if (OnQuestClaimed == null) OnQuestClaimed = new QuestClaimedEvent();
        if (OnQuestsRefreshed == null) OnQuestsRefreshed = new UnityEvent();
    }

    void Start()
    {
        LoadProgress();
        PopulateUI();
    }

    #region Populate UI
    void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    void PopulateUI()
    {
        if (contentDaily == null || questItemPrefab == null)
        {
            Debug.LogWarning("[QuestManager] UI refs missing");
            return;
        }

        ClearChildren(contentDaily);
        spawnedUI.Clear();

        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            CreateQuestUI(q, contentDaily);
        }

        ClearChildren(contentWeekly);
        var chosen = new List<QuestData>();
        var pool = new List<QuestData>(weeklyPool);
        for (int i = 0; i < weeklyVisibleCount && pool.Count > 0; i++)
        {
            int idx = rnd.Next(pool.Count);
            chosen.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        foreach (var q in chosen) CreateQuestUI(q, contentWeekly);

        // ✅ Notify all listeners
        OnQuestsRefreshed?.Invoke();
    }

    QuestItemUI CreateQuestUI(QuestData q, Transform parent)
    {
        var go = Instantiate(questItemPrefab, parent);
        go.name = "Quest_" + q.questId;
        var ui = go.GetComponent<QuestItemUI>();
        if (ui == null)
        {
            Debug.LogWarning("[QuestManager] questItemPrefab missing QuestItemUI");
            return null;
        }

        if (!progressMap.ContainsKey(q.questId))
            progressMap[q.questId] = new QuestProgressModel(q.questId, 0, false);

        var model = progressMap[q.questId];
        ui.Setup(q, model, this);
        spawnedUI[q.questId] = ui;
        return ui;
    }
    #endregion

    #region Save / Load
    void SaveProgress()
    {
        var list = new QuestProgressList();
        list.items = new QuestProgressModel[progressMap.Count];
        int i = 0;
        foreach (var kv in progressMap) list.items[i++] = kv.Value;
        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        progressMap.Clear();
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var list = JsonUtility.FromJson<QuestProgressList>(json);
            if (list?.items != null)
            {
                foreach (var m in list.items) progressMap[m.questId] = m;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[QuestManager] Failed to parse progress: " + e.Message);
        }
    }
    #endregion

    #region Progress API
    /// <summary>
    /// ✅ UPDATED: Add progress dengan event notification
    /// </summary>
    public void AddProgress(string questId, int amount)
    {
        if (string.IsNullOrEmpty(questId) || amount <= 0) return;

        QuestData qdata = FindQuestDataById(questId);
        if (qdata == null) return;

        if (!progressMap.TryGetValue(questId, out var model))
        {
            model = new QuestProgressModel(questId, 0, false);
            progressMap[questId] = model;
        }

        if (model.claimed) return;

        int oldProgress = model.progress;
        model.progress = Mathf.Clamp(model.progress + amount, 0, qdata.requiredAmount);

        // Only save & notify if changed
        if (model.progress != oldProgress)
        {
            SaveProgress();
            UpdateUIForQuest(questId);

            // ✅ Notify listeners
            OnQuestProgressChanged?.Invoke(questId, model);

            Debug.Log($"[QuestManager] Progress updated: {questId} = {model.progress}/{qdata.requiredAmount}");
        }
    }

    QuestData FindQuestDataById(string questId)
    {
        foreach (var d in dailyQuests)
            if (d != null && d.questId == questId) return d;

        foreach (var w in weeklyPool)
            if (w != null && w.questId == questId) return w;

        if (spawnedUI.ContainsKey(questId))
        {
            var ui = spawnedUI[questId];
            if (ui != null) return ui.questData;
        }
        return null;
    }

    void UpdateUIForQuest(string questId)
    {
        if (spawnedUI.TryGetValue(questId, out var ui))
        {
            if (progressMap.TryGetValue(questId, out var model))
            {
                ui.Refresh(model);
            }
        }
    }
    #endregion

    #region Claiming & Rewards
    /// <summary>
    /// ✅ UPDATED: Claim quest dengan event notification
    /// </summary>
    public void ClaimQuest(string questId)
    {
        if (!progressMap.TryGetValue(questId, out var model)) return;
        if (model.claimed) return;

        var qdata = FindQuestDataById(questId);
        if (qdata == null) return;
        if (model.progress < qdata.requiredAmount) return;

        // Grant reward
        switch (qdata.rewardType)
        {
            case QuestRewardType.Coin:
                PlayerEconomy.Instance?.AddCoins(qdata.rewardAmount);
                break;
            case QuestRewardType.Shard:
                PlayerEconomy.Instance?.AddShards(qdata.rewardAmount);
                break;
            case QuestRewardType.Energy:
                PlayerEconomy.Instance?.AddEnergy(qdata.rewardAmount);
                break;
            case QuestRewardType.Booster:
                if (!string.IsNullOrEmpty(qdata.rewardBoosterId))
                {
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                        DontDestroyOnLoad(go);
                    }
                    BoosterInventory.Instance?.AddBooster(qdata.rewardBoosterId, qdata.rewardAmount);
                }
                break;
        }

        model.claimed = true;
        SaveProgress();
        UpdateUIForQuest(questId);

        // ✅ Notify all listeners (CRITICAL untuk sinkronisasi)
        OnQuestClaimed?.Invoke(questId, qdata);

        Debug.Log($"[QuestManager] Quest claimed: {questId} - Event broadcast to all listeners");

        // Update chest controller
        var chest = FindFirstObjectByType<QuestChestController>();
        chest?.OnQuestClaimed(qdata);
    }
    #endregion

    #region Reset helpers
    public void ResetDaily()
    {
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            progressMap[q.questId] = new QuestProgressModel(q.questId, 0, false);
            if (spawnedUI.TryGetValue(q.questId, out var ui))
                ui.Refresh(progressMap[q.questId]);
        }
        SaveProgress();
        OnQuestsRefreshed?.Invoke();
        Debug.Log("[QuestManager] ResetDaily executed");
    }

    public void ResetWeekly()
    {
        foreach (var w in weeklyPool)
        {
            if (w == null) continue;
            progressMap[w.questId] = new QuestProgressModel(w.questId, 0, false);
            if (spawnedUI.TryGetValue(w.questId, out var ui))
                ui.Refresh(progressMap[w.questId]);
        }
        PopulateUI();
        SaveProgress();
        OnQuestsRefreshed?.Invoke();
        Debug.Log("[QuestManager] ResetWeekly executed");
    }
    #endregion

    #region Utility
    public QuestProgressModel GetProgress(string questId)
    {
        progressMap.TryGetValue(questId, out var m);
        return m;
    }

    /// <summary>
    /// ✅ NEW: Get quest data by ID (untuk external access)
    /// </summary>
    public QuestData GetQuestData(string questId)
    {
        return FindQuestDataById(questId);
    }

    /// <summary>
    /// ✅ NEW: Get all daily quests (untuk external display)
    /// </summary>
    public List<QuestData> GetDailyQuests()
    {
        return new List<QuestData>(dailyQuests);
    }
    #endregion
}