// QuestManager.cs
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("Data sources (assign quest ScriptableObjects)")]
    public List<QuestData> dailyQuests = new List<QuestData>();
    public List<QuestData> weeklyPool = new List<QuestData>();

    [Header("UI Prefabs & containers (assign)")]
    public GameObject questItemPrefab;    // prefab containing QuestItemUI
    public Transform dailyContent;        // ContentQuestDaily
    public Transform weeklyContent;       // ContentQuestWeekly (if you have)
    public int weeklyShowCount = 4;

    [Header("Chest / Progress")]
    public QuestChestController chestController;
    public int fullBarTotal = 10; // how much to fill the bar

    // runtime
    Dictionary<string, QuestProgressModel> progressMap = new Dictionary<string, QuestProgressModel>();
    Dictionary<string, QuestItemUI> uiMap = new Dictionary<string, QuestItemUI>();

    void Start()
    {
        // sanity
        if (questItemPrefab == null) Debug.LogError("[QuestManager] questItemPrefab not assigned.");
        PopulateDaily();
        PopulateWeekly();
        RefreshAllUI();
        UpdateChest();
        // subscribe chest claim to grant random reward
        if (chestController != null) chestController.OnChestClaimed += OnChestClaimed;
    }

    #region Persistence Keys
    string KeyProg(string id) => $"QuestProg_{id}";
    string KeyClaimed(string id) => $"QuestClaim_{id}";
    #endregion

    void PopulateDaily()
    {
        if (dailyContent == null) return;
        // clear
        foreach (Transform t in dailyContent) Destroy(t.gameObject);

        foreach (var d in dailyQuests)
        {
            if (d == null) continue;
            EnsureProgressEntry(d);
            var go = Instantiate(questItemPrefab, dailyContent);
            var ui = go.GetComponent<QuestItemUI>();
            if (ui != null)
            {
                ui.Setup(d, progressMap[d.questId], this);
                uiMap[d.questId] = ui;
            }
        }
    }

    void PopulateWeekly()
    {
        if (weeklyContent == null) return;
        foreach (Transform t in weeklyContent) Destroy(t.gameObject);

        // choose random weeklyShowCount from pool
        List<QuestData> pool = new List<QuestData>(weeklyPool);
        System.Random rnd = new System.Random();
        for (int i = 0; i < weeklyShowCount && pool.Count > 0; i++)
        {
            int idx = rnd.Next(pool.Count);
            var d = pool[idx];
            pool.RemoveAt(idx);
            if (d == null) continue;
            EnsureProgressEntry(d);
            var go = Instantiate(questItemPrefab, weeklyContent);
            var ui = go.GetComponent<QuestItemUI>();
            if (ui != null)
            {
                ui.Setup(d, progressMap[d.questId], this);
                uiMap[d.questId] = ui;
            }
        }
    }

    void EnsureProgressEntry(QuestData d)
    {
        if (d == null) return;
        if (string.IsNullOrEmpty(d.questId))
        {
            Debug.LogWarning("[QuestManager] QuestData has empty questId: " + d.name);
            return;
        }
        if (!progressMap.ContainsKey(d.questId))
        {
            int cur = PlayerPrefs.GetInt(KeyProg(d.questId), 0);
            bool claimed = PlayerPrefs.GetInt(KeyClaimed(d.questId), 0) == 1;
            var model = new QuestProgressModel(d.questId, cur, claimed);
            progressMap[d.questId] = model;
        }
    }

    public void AddProgress(string questId, int amount = 1)
    {
        if (!progressMap.ContainsKey(questId))
        {
            Debug.LogWarning("[QuestManager] AddProgress: questId not found: " + questId);
            return;
        }
        var p = progressMap[questId];
        p.current += amount;
        SaveProgress(questId);
        RefreshUI(questId);
        UpdateChest();
    }

    void SaveProgress(string id)
    {
        if (!progressMap.ContainsKey(id)) return;
        var p = progressMap[id];
        PlayerPrefs.SetInt(KeyProg(id), p.current);
        PlayerPrefs.SetInt(KeyClaimed(id), p.claimed ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ClaimQuest(string questId)
    {
        if (!progressMap.ContainsKey(questId))
        {
            Debug.LogWarning("[QuestManager] ClaimQuest: quest not tracked: " + questId);
            return;
        }
        var p = progressMap[questId];

        // find quest data (in daily or weekly list)
        QuestData data = FindQuestData(questId);
        if (data == null) { Debug.LogWarning("[QuestManager] ClaimQuest no QuestData found: " + questId); return; }

        if (p.claimed)
        {
            Debug.Log("[QuestManager] Quest already claimed: " + questId);
            return;
        }
        if (p.current < data.requiredCount)
        {
            Debug.Log("[QuestManager] Quest not completed yet: " + questId);
            return;
        }

        // grant reward
        switch (data.rewardType)
        {
            case QuestRewardType.Coin:
                PlayerEconomy.Instance.AddCoins(data.rewardAmount);
                break;
            case QuestRewardType.Energy:
                PlayerEconomy.Instance.AddEnergy(data.rewardAmount);
                break;
            case QuestRewardType.Shard:
                PlayerEconomy.Instance.AddShards(data.rewardAmount);
                break;
            case QuestRewardType.Booster:
                if (!string.IsNullOrEmpty(data.boosterItemId))
                {
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                    }
                    BoosterInventory.Instance.AddBooster(data.boosterItemId, data.rewardAmount);
                }
                break;
        }

        p.claimed = true;
        SaveProgress(questId);
        RefreshUI(questId);
        UpdateChest();
        Debug.Log($"[QuestManager] Claimed {questId} -> reward {data.rewardType} x{data.rewardAmount}");
    }

    QuestData FindQuestData(string questId)
    {
        foreach (var d in dailyQuests) if (d != null && d.questId == questId) return d;
        foreach (var d in weeklyPool) if (d != null && d.questId == questId) return d; // weekly currently chosen from pool
        return null;
    }

    void RefreshUI(string questId)
    {
        if (uiMap.TryGetValue(questId, out var ui))
        {
            ui.Refresh();
        }
    }

    void RefreshAllUI()
    {
        foreach (var kv in uiMap) kv.Value.Refresh();
    }

    void UpdateChest()
    {
        int claimedCount = 0;
        foreach (var kv in progressMap)
        {
            if (kv.Value.claimed) claimedCount++;
        }
        if (chestController != null)
            chestController.UpdateProgress(claimedCount, fullBarTotal);
    }

    void OnChestClaimed()
    {
        // sample: grant a random reward: coin 500..2000 or a booster
        int r = Random.Range(0, 3);
        if (r == 0) PlayerEconomy.Instance.AddCoins(Random.Range(500, 2001));
        else if (r == 1) PlayerEconomy.Instance.AddShards(Random.Range(5, 51));
        else
        {
            // booster random - pick first booster id from weekly pool or daily
            string boosterId = null;
            foreach (var d in weeklyPool)
                if (d != null && d.rewardType == QuestRewardType.Booster && !string.IsNullOrEmpty(d.boosterItemId)) { boosterId = d.boosterItemId; break; }
            if (boosterId == null)
                foreach (var d in dailyQuests)
                    if (d != null && d.rewardType == QuestRewardType.Booster && !string.IsNullOrEmpty(d.boosterItemId)) { boosterId = d.boosterItemId; break; }

            if (!string.IsNullOrEmpty(boosterId))
            {
                if (BoosterInventory.Instance == null)
                {
                    var go = new GameObject("BoosterInventory");
                    go.AddComponent<BoosterInventory>();
                }
                BoosterInventory.Instance.AddBooster(boosterId, 1);
            }
            else
            {
                PlayerEconomy.Instance.AddCoins(1000);
            }
        }
        Debug.Log("[QuestManager] Chest reward granted");
    }

    // Debug helper to reset all quest progress (useful in editor)
    [ContextMenu("ResetAllQuestProgress")]
    public void ResetAllQuestProgress()
    {
        foreach (var kv in progressMap)
        {
            kv.Value.current = 0;
            kv.Value.claimed = false;
            PlayerPrefs.DeleteKey(KeyProg(kv.Key));
            PlayerPrefs.DeleteKey(KeyClaimed(kv.Key));
        }
        PlayerPrefs.Save();
        RefreshAllUI();
        UpdateChest();
        Debug.Log("[QuestManager] Reset all quest progress");
    }
}
