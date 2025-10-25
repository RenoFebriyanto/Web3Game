using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Shows a small, read-only copy of daily quests inside Level panel.
/// It uses the same QuestItem prefab and reads progress from QuestManager.Instance.
/// </summary>
public class DailyQuestDisplayLevel : MonoBehaviour
{
    [Header("UI Setup")]
    public GameObject questItemPrefab;      // Prefab QuestItem (sama dengan Quest Panel)
    public Transform contentDaily;          // Parent for spawned quest items (level panel)
    public TMP_Text headerText;             // Optional header text

    [Header("Settings")]
    public int maxVisibleQuests = 3;        // how many to show here

    // Runtime
    Dictionary<string, QuestItemUI> spawnedUI = new Dictionary<string, QuestItemUI>();

    void Start()
    {
        if (headerText != null) headerText.text = "Daily Quest";
        // Delay a little so QuestManager has created UI/spawned items
        Invoke(nameof(RefreshDisplay), 0.1f);
    }

    void OnEnable()
    {
        // in case quest progress changed while disabled
        Invoke(nameof(RefreshDisplay), 0.05f);
    }

    public void RefreshDisplay()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[DailyQuestDisplayLevel] QuestManager.Instance is null!");
            return;
        }
        if (questItemPrefab == null || contentDaily == null)
        {
            Debug.LogWarning("[DailyQuestDisplayLevel] Prefab/content not assigned!");
            return;
        }

        ClearUI();

        var dailyQuests = QuestManager.Instance.dailyQuests;
        if (dailyQuests == null || dailyQuests.Count == 0) return;

        int count = Mathf.Min(dailyQuests.Count, maxVisibleQuests);
        for (int i = 0; i < count; i++)
        {
            var questData = dailyQuests[i];
            if (questData == null) continue;
            CreateQuestUI(questData);
        }

        Debug.Log($"[DailyQuestDisplayLevel] Displayed {count} daily quests");
    }

    void CreateQuestUI(QuestData questData)
    {
        var go = Instantiate(questItemPrefab, contentDaily);
        go.name = "Quest_" + questData.questId;

        var ui = go.GetComponent<QuestItemUI>();
        if (ui == null)
        {
            Debug.LogWarning("[DailyQuestDisplayLevel] questItemPrefab missing QuestItemUI!");
            Destroy(go);
            return;
        }

        var model = QuestManager.Instance.GetProgress(questData.questId);
        if (model == null) model = new QuestProgressModel(questData.questId, 0, false);

        ui.Setup(questData, model, QuestManager.Instance);

        spawnedUI[questData.questId] = ui;
    }

    void ClearUI()
    {
        spawnedUI.Clear();
        if (contentDaily == null) return;
        for (int i = contentDaily.childCount - 1; i >= 0; i--) Destroy(contentDaily.GetChild(i).gameObject);
    }

    public void OnQuestClaimed()
    {
        // small delay so QuestManager finishes Save/Update
        Invoke(nameof(RefreshDisplay), 0.08f);
    }

    public void ForceRefresh() => RefreshDisplay();

    public void UpdateQuest(string questId)
    {
        if (spawnedUI.TryGetValue(questId, out var ui))
        {
            var model = QuestManager.Instance?.GetProgress(questId);
            if (model != null) ui.Refresh(model);
        }
    }
}
