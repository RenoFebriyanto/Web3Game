using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // Prefab & UI (assign in Inspector)
    [Header("Prefabs & UI")]
    public GameObject questItemPrefab;        // QuestItem prefab (must contain QuestItemUI)
    public Transform contentDaily;            // ContentQuestDaily (where items will be parented)
    public Transform contentWeekly;           // ContentQuestWK

    [Header("Data")]
    public List<QuestData> dailyQuests = new List<QuestData>(); // fixed daily list (5)
    public List<QuestData> weeklyPool = new List<QuestData>();  // pool of weekly candidates
    public int weeklyVisibleCount = 3;        // how many weekly shown at once

    // persistence key
    const string PREF_KEY = "QuestProgress_v1";

    // runtime state
    Dictionary<string, QuestProgressModel> progressMap = new Dictionary<string, QuestProgressModel>();
    Dictionary<string, QuestItemUI> spawnedUI = new Dictionary<string, QuestItemUI>();
    System.Random rnd = new System.Random();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        LoadProgress();
        PopulateUI();
    }

    #region Populate UI
    void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    void PopulateUI()
    {
        // daily
        if (contentDaily == null || questItemPrefab == null) { Debug.LogWarning("[QuestManager] UI refs missing"); return; }

        ClearChildren(contentDaily);
        spawnedUI.Clear();

        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            CreateQuestUI(q, contentDaily);
        }

        // weekly: pick random visible entries from pool (don't duplicate)
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
    }

    QuestItemUI CreateQuestUI(QuestData q, Transform parent)
    {
        var go = Instantiate(questItemPrefab, parent);
        go.name = "Quest_" + q.questId;
        var ui = go.GetComponent<QuestItemUI>();
        if (ui == null) { Debug.LogWarning("[QuestManager] questItemPrefab missing QuestItemUI"); return null; }
        // ensure model exists
        if (!progressMap.ContainsKey(q.questId)) progressMap[q.questId] = new QuestProgressModel(q.questId, 0, false);
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

    #region Progress API (call these from trackers)
    // Add progress (amount can be 1 or more). If quest reaches requiredAmount, UI will show ready.
    public void AddProgress(string questId, int amount)
    {
        if (string.IsNullOrEmpty(questId) || amount <= 0) return;
        // find quest data (daily or weekly)
        QuestData qdata = FindQuestDataById(questId);
        if (qdata == null) return;
        if (!progressMap.TryGetValue(questId, out var model))
        {
            model = new QuestProgressModel(questId, 0, false);
            progressMap[questId] = model;
        }
        if (model.claimed) return; // already claimed
        model.progress = Mathf.Clamp(model.progress + amount, 0, qdata.requiredAmount);
        SaveProgress();
        UpdateUIForQuest(questId);
    }

    QuestData FindQuestDataById(string questId)
    {
        foreach (var d in dailyQuests) if (d != null && d.questId == questId) return d;
        foreach (var w in weeklyPool) if (w != null && w.questId == questId) return w;
        // also check currently spawned weekly (if from pool but not in weeklyPool list)
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
    // called by QuestItemUI when player presses Claim button
    public void ClaimQuest(string questId)
    {
        if (!progressMap.TryGetValue(questId, out var model)) return;
        if (model.claimed) return;
        var qdata = FindQuestDataById(questId);
        if (qdata == null) return;
        if (model.progress < qdata.requiredAmount) return;

        // grant reward
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
            case QuestRewardType.None:
            default:
                break;
        }

        model.claimed = true;
        SaveProgress();
        UpdateUIForQuest(questId);

        // optional: inform chest controller to advance progress etc.
        var chest = FindObjectOfType<QuestChestController>();
        chest?.OnQuestClaimed(qdata);
    }
    #endregion

    #region Reset helpers (called by tester or daily reset job)
    public void ResetDaily()
    {
        // reset progress and claimed only for daily quests
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;
            progressMap[q.questId] = new QuestProgressModel(q.questId, 0, false);
            if (spawnedUI.TryGetValue(q.questId, out var ui)) ui.Refresh(progressMap[q.questId]);
        }
        SaveProgress();
        Debug.Log("[QuestManager] ResetDaily executed");
    }

    public void ResetWeekly()
    {
        // mark weekly claimed and progress cleared (for simplicity)
        foreach (var w in weeklyPool)
        {
            if (w == null) continue;
            progressMap[w.questId] = new QuestProgressModel(w.questId, 0, false);
            if (spawnedUI.TryGetValue(w.questId, out var ui)) ui.Refresh(progressMap[w.questId]);
        }
        // repopulate weekly (randomize)
        PopulateUI();
        SaveProgress();
        Debug.Log("[QuestManager] ResetWeekly executed");
    }
    #endregion

    #region Utility (editor/test helpers)
    // get progress (for UI/testing)
    public QuestProgressModel GetProgress(string questId)
    {
        progressMap.TryGetValue(questId, out var m);
        return m;
    }
    #endregion
}
