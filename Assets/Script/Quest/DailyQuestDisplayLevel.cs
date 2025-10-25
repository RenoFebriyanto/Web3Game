using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Display daily quests di Level Panel (read-only view dengan quick claim).
/// Subscribe ke QuestManager yang sama dengan Quest Panel.
/// </summary>
public class DailyQuestDisplayLevel : MonoBehaviour
{
    [Header("UI Setup")]
    public GameObject questItemPrefab;      // Prefab QuestItem (sama dengan Quest Panel)
    public Transform contentDaily;          // Parent untuk spawn quest items
    public TMP_Text headerText;             // Optional: "Daily Quest" title

    [Header("Settings")]
    public int maxVisibleQuests = 3;        // Limit berapa quest yang ditampilkan

    // Runtime
    Dictionary<string, QuestItemUI> spawnedUI = new Dictionary<string, QuestItemUI>();

    void Start()
    {
        if (headerText != null)
        {
            headerText.text = "Daily Quest";
        }

        // Tunggu QuestManager ready
        Invoke(nameof(RefreshDisplay), 0.1f);
    }

    void OnEnable()
    {
        // Subscribe ke QuestManager events jika ada
        // (QuestManager belum punya event system, tapi kita bisa refresh manual)
        RefreshDisplay();
    }

    void OnDisable()
    {
        // Unsubscribe jika perlu
    }

    /// <summary>
    /// Refresh display - ambil data dari QuestManager
    /// </summary>
    public void RefreshDisplay()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[DailyQuestDisplayLevel] QuestManager.Instance is null!");
            return;
        }

        if (questItemPrefab == null || contentDaily == null)
        {
            Debug.LogWarning("[DailyQuestDisplayLevel] Prefab or content not assigned!");
            return;
        }

        // Clear existing UI
        ClearUI();

        // Get daily quests dari QuestManager
        var dailyQuests = GetDailyQuests();

        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Debug.Log("[DailyQuestDisplayLevel] No daily quests found");
            return;
        }

        // Limit jumlah quest yang ditampilkan
        int count = Mathf.Min(dailyQuests.Count, maxVisibleQuests);

        for (int i = 0; i < count; i++)
        {
            var questData = dailyQuests[i];
            if (questData == null) continue;

            CreateQuestUI(questData);
        }

        Debug.Log($"[DailyQuestDisplayLevel] Displayed {count} daily quests");
    }

    /// <summary>
    /// Get daily quests dari QuestManager (access dailyQuests list)
    /// </summary>
    List<QuestData> GetDailyQuests()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return null;

        // Access dailyQuests public field dari QuestManager
        return qm.dailyQuests;
    }

    /// <summary>
    /// Create quest UI item
    /// </summary>
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

        // Get progress model dari QuestManager
        var model = QuestManager.Instance.GetProgress(questData.questId);
        if (model == null)
        {
            model = new QuestProgressModel(questData.questId, 0, false);
        }

        // Setup UI (akan subscribe ke QuestManager)
        ui.Setup(questData, model, QuestManager.Instance);

        spawnedUI[questData.questId] = ui;
    }

    /// <summary>
    /// Clear semua spawned UI
    /// </summary>
    void ClearUI()
    {
        spawnedUI.Clear();

        if (contentDaily == null) return;

        for (int i = contentDaily.childCount - 1; i >= 0; i--)
        {
            Destroy(contentDaily.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Call this ketika player claim quest (untuk refresh display)
    /// </summary>
    public void OnQuestClaimed()
    {
        // Delay refresh sedikit agar QuestManager sempat update
        Invoke(nameof(RefreshDisplay), 0.1f);
    }

    // ==========================================
    // PUBLIC API untuk manual refresh
    // ==========================================

    /// <summary>
    /// Public method untuk refresh dari luar (misal dari button)
    /// </summary>
    public void ForceRefresh()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Update single quest UI (ketika progress berubah)
    /// </summary>
    public void UpdateQuest(string questId)
    {
        if (spawnedUI.TryGetValue(questId, out var ui))
        {
            var model = QuestManager.Instance?.GetProgress(questId);
            if (model != null)
            {
                ui.Refresh(model);
            }
        }
    }

    // ==========================================
    // CONTEXT MENU DEBUG
    // ==========================================

    [ContextMenu("Force Refresh Display")]
    void Context_ForceRefresh()
    {
        RefreshDisplay();
    }

    [ContextMenu("Print Daily Quests")]
    void Context_PrintQuests()
    {
        var dailyQuests = GetDailyQuests();
        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Debug.Log("No daily quests found");
            return;
        }

        Debug.Log($"=== DAILY QUESTS ({dailyQuests.Count}) ===");
        for (int i = 0; i < dailyQuests.Count; i++)
        {
            var q = dailyQuests[i];
            if (q == null) continue;

            var model = QuestManager.Instance?.GetProgress(q.questId);
            string status = model != null ? 
                (model.claimed ? "CLAIMED" : $"{model.progress}/{q.requiredAmount}") : 
                "NO_PROGRESS";

            Debug.Log($"{i + 1}. {q.title} [{q.questId}] - {status}");
        }
    }
}