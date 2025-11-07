using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// FIXED: Display daily quests di Level Panel dengan proper layout & sync
/// Uses QuestItemCompact prefab untuk tampilan yang lebih kecil
/// Auto-sync dengan QuestManager dan QuestP panel
/// </summary>
public class DailyQuestDisplayLevel : MonoBehaviour
{
    [Header("✅ SETUP REQUIRED")]
    [Tooltip("Drag QuestItemCompact prefab (bukan QuestItem biasa!)")]
    public GameObject questItemCompactPrefab;

    [Tooltip("Drag ContentDaily dari ScrollView")]
    public Transform contentDaily;

    [Header("📝 Optional Header")]
    public TMP_Text headerText;

    [Header("⚙️ Settings")]
    [Tooltip("Berapa quest yang ditampilkan")]
    public int maxVisibleQuests = 3;

    [Tooltip("Auto-refresh interval (detik)")]
    public float autoRefreshInterval = 1f;

    [Header("📐 Layout Settings (PENTING!)")]
    [Tooltip("Spacing antar quest items (vertical)")]
    public float itemSpacing = 10f;

    [Tooltip("Padding ContentDaily (top, bottom)")]
    public float contentPaddingTop = 10f;
    public float contentPaddingBottom = 10f;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = false;

    // Runtime
    private Dictionary<string, QuestItemCompact> spawnedUI = new Dictionary<string, QuestItemCompact>();
    private float lastRefreshTime = 0f;

    void Start()
    {
        if (headerText != null)
        {
            headerText.text = "Daily Quest";
        }

        // Setup layout
        SetupContentLayout();

        // Initial refresh (dengan delay agar QuestManager ready)
        Invoke(nameof(RefreshDisplay), 0.2f);
    }

    void Update()
    {
        // Auto-refresh untuk sinkronisasi real-time
        if (Time.time - lastRefreshTime >= autoRefreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshDisplay();
        }
    }

    void OnEnable()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Setup ContentDaily layout (VerticalLayoutGroup)
    /// </summary>
    void SetupContentLayout()
    {
        if (contentDaily == null)
        {
            LogError("contentDaily is NULL! Assign it in Inspector!");
            return;
        }

        // Setup VerticalLayoutGroup
        var vlg = contentDaily.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = contentDaily.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        }

        vlg.spacing = itemSpacing;
        vlg.padding.top = Mathf.RoundToInt(contentPaddingTop);
        vlg.padding.bottom = Mathf.RoundToInt(contentPaddingBottom);
        vlg.padding.left = 5;
        vlg.padding.right = 5;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Setup ContentSizeFitter
        var csf = contentDaily.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (csf == null)
        {
            csf = contentDaily.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        }

        csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;

        Log("✓ ContentDaily layout setup complete");
    }

    /// <summary>
    /// Main refresh method - update semua quest display
    /// </summary>
    public void RefreshDisplay()
    {
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null!");
            return;
        }

        if (questItemCompactPrefab == null || contentDaily == null)
        {
            LogError("questItemCompactPrefab or contentDaily not assigned!");
            return;
        }

        // Get daily quests
        var dailyQuests = GetDailyQuests();

        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Log("No daily quests found");
            ClearUI();
            return;
        }

        // Track which quests should be displayed
        HashSet<string> currentQuestIds = new HashSet<string>();

        // Limit jumlah quest
        int count = Mathf.Min(dailyQuests.Count, maxVisibleQuests);

        for (int i = 0; i < count; i++)
        {
            var questData = dailyQuests[i];
            if (questData == null) continue;

            currentQuestIds.Add(questData.questId);

            // Update or create UI
            if (spawnedUI.ContainsKey(questData.questId))
            {
                // Already exists - just refresh
                UpdateExistingQuestUI(questData);
            }
            else
            {
                // Create new
                CreateQuestUI(questData);
            }
        }

        // Remove quest UI yang tidak ada di list
        RemoveObsoleteQuestUI(currentQuestIds);

        Log($"✓ Displayed {count} daily quests");
    }

    /// <summary>
    /// Get daily quests dari QuestManager
    /// </summary>
    List<QuestData> GetDailyQuests()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return null;

        return qm.dailyQuests;
    }

    /// <summary>
    /// Create new quest UI item
    /// </summary>
    void CreateQuestUI(QuestData questData)
    {
        var go = Instantiate(questItemCompactPrefab, contentDaily);
        go.name = "QuestCompact_" + questData.questId;

        var ui = go.GetComponent<QuestItemCompact>();
        if (ui == null)
        {
            LogError("questItemCompactPrefab missing QuestItemCompact component!");
            Destroy(go);
            return;
        }

        // Get progress model
        var model = QuestManager.Instance.GetProgress(questData.questId);
        if (model == null)
        {
            model = new QuestProgressModel(questData.questId, 0, false);
        }

        // Setup UI
        ui.Setup(questData, model, QuestManager.Instance);

        spawnedUI[questData.questId] = ui;

        Log($"✓ Created UI for quest: {questData.questId}");
    }

    /// <summary>
    /// Update existing quest UI
    /// </summary>
    void UpdateExistingQuestUI(QuestData questData)
    {
        if (!spawnedUI.TryGetValue(questData.questId, out var ui)) return;

        var model = QuestManager.Instance.GetProgress(questData.questId);
        if (model != null)
        {
            ui.Refresh(model);
        }
    }

    /// <summary>
    /// Remove quest UI yang tidak ada di current list
    /// </summary>
    void RemoveObsoleteQuestUI(HashSet<string> currentQuestIds)
    {
        List<string> toRemove = new List<string>();

        foreach (var kv in spawnedUI)
        {
            if (!currentQuestIds.Contains(kv.Key))
            {
                toRemove.Add(kv.Key);
            }
        }

        foreach (var questId in toRemove)
        {
            if (spawnedUI.TryGetValue(questId, out var ui))
            {
                if (ui != null && ui.gameObject != null)
                {
                    Destroy(ui.gameObject);
                }
                spawnedUI.Remove(questId);
                Log($"✓ Removed obsolete quest UI: {questId}");
            }
        }
    }

    /// <summary>
    /// Clear all spawned UI
    /// </summary>
    void ClearUI()
    {
        foreach (var kv in spawnedUI)
        {
            if (kv.Value != null && kv.Value.gameObject != null)
            {
                Destroy(kv.Value.gameObject);
            }
        }
        spawnedUI.Clear();

        Log("✓ Cleared all quest UI");
    }

    /// <summary>
    /// Called when player claims quest (untuk force refresh)
    /// </summary>
    public void OnQuestClaimed()
    {
        Invoke(nameof(RefreshDisplay), 0.1f);
    }

    /// <summary>
    /// Update single quest UI
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

    // ========================================
    // PUBLIC API
    // ========================================

    public void ForceRefresh()
    {
        RefreshDisplay();
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[DailyQuestDisplayLevel] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[DailyQuestDisplayLevel] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[DailyQuestDisplayLevel] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU
    // ========================================

    [ContextMenu("Force Refresh Display")]
    void Context_ForceRefresh()
    {
        RefreshDisplay();
    }

    [ContextMenu("Clear All UI")]
    void Context_ClearUI()
    {
        ClearUI();
    }

    [ContextMenu("Print Quest Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== DAILY QUEST DISPLAY STATUS ===");
        Debug.Log($"Spawned UI count: {spawnedUI.Count}");

        var dailyQuests = GetDailyQuests();
        if (dailyQuests != null)
        {
            Debug.Log($"Available daily quests: {dailyQuests.Count}");
            foreach (var q in dailyQuests)
            {
                if (q == null) continue;
                var model = QuestManager.Instance?.GetProgress(q.questId);
                string status = model != null ?
                    $"{model.progress}/{q.requiredAmount} {(model.claimed ? "[CLAIMED]" : "")}" :
                    "NO_PROGRESS";
                Debug.Log($"  - {q.questId}: {status}");
            }
        }
        Debug.Log("==================================");
    }
}