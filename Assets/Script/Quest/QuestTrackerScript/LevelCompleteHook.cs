using UnityEngine;

/// <summary>
/// ✅✅✅ IMPROVED v2: Multi-scene support dengan retry mechanism
/// Tracks level completions untuk quest "Selesaikan X level"
/// - Auto-retry jika LevelGameSession belum ready
/// - Support multiple quest IDs
/// - Better error handling
/// </summary>
public class LevelCompleteHook : MonoBehaviour
{
    [Header("Quest Setup")]
    [Tooltip("Quest Ids untuk progress saat level complete (bisa multiple)")]
    public string[] questIds = new string[] { "dailyquest4" }; // Array untuk support multiple quests

    [Header("Auto-Connect")]
    [Tooltip("Auto-subscribe ke LevelGameSession saat Start?")]
    public bool autoConnect = true;

    [Tooltip("Max retry attempts untuk connect")]
    public int maxRetryAttempts = 10;

    [Tooltip("Retry interval (detik)")]
    public float retryInterval = 0.5f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private LevelGameSession levelSession;
    private bool isSubscribed = false;
    private int retryCount = 0;

    void Start()
    {
        Log("=== LEVEL COMPLETE HOOK START ===");
        
        if (questIds == null || questIds.Length == 0)
        {
            LogError("❌ No quest IDs assigned! Add quest IDs in Inspector.");
            LogError("Example: dailyquest4 (for 'Complete 5 levels' quest)");
            return;
        }

        Log($"Configured Quest IDs: {string.Join(", ", questIds)}");

        if (autoConnect)
        {
            ConnectToLevelSession();
        }
    }

    void OnEnable()
    {
        if (autoConnect && !isSubscribed)
        {
            // Reset retry count on enable
            retryCount = 0;
            ConnectToLevelSession();
        }
    }

    void OnDisable()
    {
        DisconnectFromLevelSession();
    }

    void OnDestroy()
    {
        DisconnectFromLevelSession();
    }

    /// <summary>
    /// ✅ Auto-find dan subscribe ke LevelGameSession dengan retry
    /// </summary>
    void ConnectToLevelSession()
    {
        if (isSubscribed)
        {
            Log("Already subscribed - skipping");
            return;
        }

        Log($"Attempting connection... (attempt {retryCount + 1}/{maxRetryAttempts})");

        // Find LevelGameSession
        levelSession = FindFirstObjectByType<LevelGameSession>();

        if (levelSession == null)
        {
            retryCount++;
            
            if (retryCount >= maxRetryAttempts)
            {
                LogError($"❌ Failed to find LevelGameSession after {maxRetryAttempts} attempts!");
                LogError("Make sure LevelGameSession exists in the scene.");
                LogError("This script should be in the same scene as LevelGameSession (usually Gameplay scene).");
                return;
            }

            Log($"LevelGameSession not found. Retrying in {retryInterval}s... ({retryCount}/{maxRetryAttempts})");
            Invoke(nameof(RetryConnect), retryInterval);
            return;
        }

        // ✅ Found! Subscribe to event
        levelSession.OnLevelCompleted.RemoveListener(OnLevelComplete);
        levelSession.OnLevelCompleted.AddListener(OnLevelComplete);

        isSubscribed = true;
        retryCount = 0; // Reset retry count
        
        Log($"✅✅✅ CONNECTED TO LEVELGAMESESSION!");
        Log($"Subscribed to OnLevelCompleted event");
        Log($"Tracking quest IDs: {string.Join(", ", questIds)}");
    }

    void RetryConnect()
    {
        Log($"⏳ Retrying connection... (attempt {retryCount + 1}/{maxRetryAttempts})");
        ConnectToLevelSession();
    }

    void DisconnectFromLevelSession()
    {
        if (levelSession != null)
        {
            levelSession.OnLevelCompleted.RemoveListener(OnLevelComplete);
            Log("✓ Disconnected from LevelGameSession");
        }

        isSubscribed = false;
        levelSession = null;
    }

    /// <summary>
    /// ✅ Called saat level complete (via event)
    /// </summary>
    void OnLevelComplete()
    {
        Log("========================================");
        Log("🎉 LEVEL COMPLETE EVENT RECEIVED!");
        
        if (questIds == null || questIds.Length == 0)
        {
            LogError("❌ No quest IDs configured!");
            Log("========================================");
            return;
        }

        if (QuestManager.Instance == null)
        {
            LogError("❌ QuestManager.Instance is null!");
            Log("========================================");
            return;
        }

        // ✅ Add progress ke SEMUA quest IDs yang dikonfigurasi
        int successCount = 0;
        foreach (string questId in questIds)
        {
            if (string.IsNullOrEmpty(questId))
            {
                LogWarning($"⚠️ Skipping empty quest ID");
                continue;
            }

            // Check if quest exists
            var questData = QuestManager.Instance.GetQuestData(questId);
            if (questData == null)
            {
                LogWarning($"⚠️ Quest '{questId}' not found in QuestManager!");
                continue;
            }

            // Add progress
            QuestManager.Instance.AddProgress(questId, 1);
            successCount++;
            
            Log($"✅ Progress added to quest: {questId}");
            Log($"   Quest title: {questData.title}");
            
            // Get current progress
            var progress = QuestManager.Instance.GetProgress(questId);
            if (progress != null)
            {
                Log($"   Current progress: {progress.progress}/{questData.requiredAmount}");
                Log($"   Claimed: {progress.claimed}");
            }
        }

        Log($"✓✓✓ LEVEL COMPLETE PROCESSING DONE!");
        Log($"Successfully updated {successCount}/{questIds.Length} quests");
        Log("========================================");
    }

    /// <summary>
    /// PUBLIC API: Manual trigger untuk testing
    /// </summary>
    public void TriggerLevelComplete()
    {
        Log("⚠️ Manual trigger called");
        OnLevelComplete();
    }

    /// <summary>
    /// PUBLIC API: Add quest ID at runtime
    /// </summary>
    public void AddQuestId(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            LogError("Cannot add empty quest ID");
            return;
        }

        // Convert to list, add, convert back to array
        var list = new System.Collections.Generic.List<string>(questIds);
        if (!list.Contains(questId))
        {
            list.Add(questId);
            questIds = list.ToArray();
            Log($"✓ Added quest ID: {questId}");
        }
        else
        {
            Log($"Quest ID '{questId}' already exists");
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[LevelCompleteHook] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[LevelCompleteHook] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[LevelCompleteHook] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Test: Trigger Level Complete")]
    void Context_TestLevelComplete()
    {
        TriggerLevelComplete();
        Debug.Log("✓ Manually triggered level complete");
    }

    [ContextMenu("Debug: Force Reconnect")]
    void Context_ForceReconnect()
    {
        retryCount = 0;
        DisconnectFromLevelSession();
        ConnectToLevelSession();
        Debug.Log("✓ Force reconnected");
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== LEVEL COMPLETE HOOK STATUS ===");
        Debug.Log($"Quest IDs: {(questIds != null ? string.Join(", ", questIds) : "NULL")}");
        Debug.Log($"Auto Connect: {autoConnect}");
        Debug.Log($"Is Subscribed: {isSubscribed}");
        Debug.Log($"Retry Count: {retryCount}/{maxRetryAttempts}");
        Debug.Log($"LevelGameSession: {(levelSession != null ? "FOUND ✓" : "NULL ❌")}");
        Debug.Log($"QuestManager: {(QuestManager.Instance != null ? "OK ✓" : "NULL ❌")}");
        
        if (QuestManager.Instance != null && questIds != null)
        {
            Debug.Log("\nQuest Status:");
            foreach (string questId in questIds)
            {
                if (string.IsNullOrEmpty(questId)) continue;
                
                var questData = QuestManager.Instance.GetQuestData(questId);
                var progress = QuestManager.Instance.GetProgress(questId);
                
                if (questData != null && progress != null)
                {
                    Debug.Log($"  {questId}: {progress.progress}/{questData.requiredAmount} {(progress.claimed ? "[CLAIMED]" : "")}");
                }
                else
                {
                    Debug.Log($"  {questId}: NOT FOUND or NO PROGRESS");
                }
            }
        }
        
        Debug.Log("==================================");
    }

    [ContextMenu("Debug: Simulate 5 Level Completions")]
    void Context_Simulate5Levels()
    {
        Debug.Log("=== SIMULATING 5 LEVEL COMPLETIONS ===");
        for (int i = 1; i <= 5; i++)
        {
            Debug.Log($"\n--- Level {i} Complete ---");
            TriggerLevelComplete();
        }
        Debug.Log("\n=== SIMULATION COMPLETE ===");
    }
}