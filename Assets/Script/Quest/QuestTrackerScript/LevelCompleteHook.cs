using UnityEngine;

/// <summary>
/// ✅✅✅ IMPROVED: Auto-connect ke LevelGameSession
/// Tracks level completions untuk quest "Selesaikan X level"
/// </summary>
public class LevelCompleteHook : MonoBehaviour
{
    [Header("Quest Setup")]
    [Tooltip("Quest Id untuk progress saat level complete (e.g., dailyquest4)")]
    public string questId = "dailyquest4"; // Default: "Selesaikan 5 Level"

    [Header("Auto-Connect")]
    [Tooltip("Auto-subscribe ke LevelGameSession saat Start?")]
    public bool autoConnect = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private LevelGameSession levelSession;
    private bool isSubscribed = false;

    void Start()
    {
        if (autoConnect)
        {
            ConnectToLevelSession();
        }
    }

    void OnEnable()
    {
        if (autoConnect && !isSubscribed)
        {
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
    /// ✅ Auto-find dan subscribe ke LevelGameSession
    /// </summary>
    void ConnectToLevelSession()
    {
        if (isSubscribed)
        {
            Log("Already subscribed");
            return;
        }

        // Find LevelGameSession
        levelSession = FindFirstObjectByType<LevelGameSession>();

        if (levelSession == null)
        {
            LogWarning("LevelGameSession not found in scene! Will retry in 0.5s...");
            Invoke(nameof(RetryConnect), 0.5f);
            return;
        }

        // Subscribe to OnLevelCompleted event
        levelSession.OnLevelCompleted.RemoveListener(OnLevelComplete);
        levelSession.OnLevelCompleted.AddListener(OnLevelComplete);

        isSubscribed = true;
        Log($"✓ Connected to LevelGameSession, subscribed to OnLevelCompleted");
    }

    void RetryConnect()
    {
        Log("Retrying connection to LevelGameSession...");
        ConnectToLevelSession();
    }

    void DisconnectFromLevelSession()
    {
        if (levelSession != null)
        {
            levelSession.OnLevelCompleted.RemoveListener(OnLevelComplete);
            Log("Disconnected from LevelGameSession");
        }

        isSubscribed = false;
        levelSession = null;
    }

    /// <summary>
    /// ✅ Called saat level complete (via event)
    /// </summary>
    void OnLevelComplete()
    {
        if (string.IsNullOrEmpty(questId))
        {
            LogWarning("questId is empty! Assign quest ID in Inspector.");
            return;
        }

        if (QuestManager.Instance == null)
        {
            LogError("QuestManager.Instance is null!");
            return;
        }

        // Add 1 progress ke quest
        QuestManager.Instance.AddProgress(questId, 1);
        Log($"✓ Level complete! Added progress to quest: {questId}");
    }

    /// <summary>
    /// PUBLIC API: Manual call untuk level complete
    /// </summary>
    public void TriggerLevelComplete()
    {
        OnLevelComplete();
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
        DisconnectFromLevelSession();
        ConnectToLevelSession();
        Debug.Log("✓ Force reconnected");
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== LEVEL COMPLETE HOOK STATUS ===");
        Debug.Log($"Quest ID: {questId}");
        Debug.Log($"Auto Connect: {autoConnect}");
        Debug.Log($"Is Subscribed: {isSubscribed}");
        Debug.Log($"LevelGameSession: {(levelSession != null ? "FOUND" : "NULL")}");
        Debug.Log($"QuestManager: {(QuestManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log("==================================");
    }
}