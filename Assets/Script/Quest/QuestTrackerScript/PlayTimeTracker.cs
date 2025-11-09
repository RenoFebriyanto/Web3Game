using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ✅✅✅ IMPROVED: Playtime tracker dengan better precision
/// - Tracks playtime per minute untuk quest
/// - Persistent timer (saves on pause/quit)
/// - Better debugging
/// </summary>
public class PlayTimeTracker : MonoBehaviour
{
    [Header("Quest Setup")]
    [Tooltip("Quest Ids to progress per minute (e.g. dailyquest1, dailyquest3, dailyquest5)")]
    public List<string> questIdsToProgressPerMinute = new List<string>();

    [Header("Tracking Settings")]
    [Tooltip("If false, stops tracking when app not focused")]
    public bool trackWhenInactive = false;

    [Tooltip("Save interval untuk persistent timer (detik)")]
    public float saveInterval = 10f;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    [Tooltip("Show minute updates in console")]
    public bool logMinuteUpdates = true;

    // Timer state
    private float elapsed = 0f;
    private float lastSaveTime = 0f;
    private int totalMinutesTracked = 0;

    // Save key
    private const string PREF_ELAPSED_TIME = "Kulino_PlayTime_Elapsed_v1";

    void Start()
    {
        Log("=== PLAYTIME TRACKER START ===");
        
        if (questIdsToProgressPerMinute == null || questIdsToProgressPerMinute.Count == 0)
        {
            LogWarning("⚠️ No quest IDs assigned! Add quest IDs in Inspector.");
            LogWarning("Example: dailyquest1, dailyquest3, dailyquest5 (for playtime quests)");
        }
        else
        {
            Log($"Tracking quest IDs: {string.Join(", ", questIdsToProgressPerMinute)}");
        }

        // Load saved time
        LoadElapsedTime();
        
        Log($"Track when inactive: {trackWhenInactive}");
        Log($"Save interval: {saveInterval}s");
        Log("=============================");
    }

    void Update()
    {
        // Check if should track
        if (!trackWhenInactive && !Application.isFocused)
        {
            return;
        }

        // Update elapsed time (use unscaled for accuracy)
        elapsed += Time.unscaledDeltaTime;
        
        // Check if a minute has passed
        if (elapsed >= 60f)
        {
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            elapsed -= minutes * 60f; // Keep remainder
            
            totalMinutesTracked += minutes;
            
            if (logMinuteUpdates)
            {
                Log($"⏱️ {minutes} minute(s) passed! Total tracked: {totalMinutesTracked} minutes");
            }
            
            // Add progress to all configured quests
            AddProgressToQuests(minutes);
        }

        // Periodic save
        lastSaveTime += Time.unscaledDeltaTime;
        if (lastSaveTime >= saveInterval)
        {
            SaveElapsedTime();
            lastSaveTime = 0f;
        }
    }

    /// <summary>
    /// Add progress ke semua quest yang dikonfigurasi
    /// </summary>
    void AddProgressToQuests(int minutes)
    {
        if (QuestManager.Instance == null)
        {
            LogError("❌ QuestManager.Instance is null! Cannot add progress.");
            return;
        }

        if (questIdsToProgressPerMinute == null || questIdsToProgressPerMinute.Count == 0)
        {
            LogWarning("⚠️ No quest IDs configured");
            return;
        }

        Log($"========================================");
        Log($"📊 ADDING PLAYTIME PROGRESS: {minutes} minute(s)");

        int successCount = 0;
        foreach (var questId in questIdsToProgressPerMinute)
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

            // Add progress (minutes)
            QuestManager.Instance.AddProgress(questId, minutes);
            successCount++;

            Log($"✅ Progress added to: {questId}");
            Log($"   Quest title: {questData.title}");

            // Get current progress
            var progress = QuestManager.Instance.GetProgress(questId);
            if (progress != null)
            {
                Log($"   Current progress: {progress.progress}/{questData.requiredAmount} minutes");
                
                if (progress.progress >= questData.requiredAmount && !progress.claimed)
                {
                    Log($"   🎉 QUEST COMPLETE! Ready to claim!");
                }
                else if (progress.claimed)
                {
                    Log($"   ✓ Quest already claimed");
                }
            }
        }

        Log($"✓ Updated {successCount}/{questIdsToProgressPerMinute.Count} quests");
        Log($"========================================");
    }

    /// <summary>
    /// Save elapsed time to PlayerPrefs (persistent)
    /// </summary>
    void SaveElapsedTime()
    {
        PlayerPrefs.SetFloat(PREF_ELAPSED_TIME, elapsed);
        PlayerPrefs.Save();
        
        if (enableDebugLogs && logMinuteUpdates)
        {
            Log($"💾 Saved elapsed time: {elapsed:F1}s");
        }
    }

    /// <summary>
    /// Load elapsed time from PlayerPrefs
    /// </summary>
    void LoadElapsedTime()
    {
        elapsed = PlayerPrefs.GetFloat(PREF_ELAPSED_TIME, 0f);
        Log($"✓ Loaded elapsed time: {elapsed:F1}s");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App paused/backgrounded - save state
            SaveElapsedTime();
            Log("📱 App paused - saved elapsed time");
        }
        else
        {
            // App resumed
            Log("📱 App resumed");
        }
    }

    void OnApplicationQuit()
    {
        // Save on quit
        SaveElapsedTime();
        Log("🚪 App quit - saved elapsed time");
    }

    void OnDisable()
    {
        SaveElapsedTime();
    }

    void OnDestroy()
    {
        SaveElapsedTime();
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Get current elapsed seconds
    /// </summary>
    public float GetElapsedSeconds()
    {
        return elapsed;
    }

    /// <summary>
    /// Get total minutes tracked
    /// </summary>
    public int GetTotalMinutesTracked()
    {
        return totalMinutesTracked;
    }

    /// <summary>
    /// Reset elapsed time
    /// </summary>
    public void ResetElapsedTime()
    {
        elapsed = 0f;
        totalMinutesTracked = 0;
        SaveElapsedTime();
        Log("✓ Elapsed time reset");
    }

    /// <summary>
    /// Add quest ID at runtime
    /// </summary>
    public void AddQuestId(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            LogError("Cannot add empty quest ID");
            return;
        }

        if (questIdsToProgressPerMinute.Contains(questId))
        {
            Log($"Quest ID '{questId}' already exists");
            return;
        }

        questIdsToProgressPerMinute.Add(questId);
        Log($"✓ Added quest ID: {questId}");
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[PlayTimeTracker] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[PlayTimeTracker] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[PlayTimeTracker] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Test: Simulate 1 Minute")]
    void Context_Simulate1Minute()
    {
        Debug.Log("=== SIMULATING 1 MINUTE ===");
        AddProgressToQuests(1);
        totalMinutesTracked++;
        Debug.Log($"Total minutes tracked: {totalMinutesTracked}");
    }

    [ContextMenu("Test: Simulate 5 Minutes")]
    void Context_Simulate5Minutes()
    {
        Debug.Log("=== SIMULATING 5 MINUTES ===");
        AddProgressToQuests(5);
        totalMinutesTracked += 5;
        Debug.Log($"Total minutes tracked: {totalMinutesTracked}");
    }

    [ContextMenu("Test: Simulate 30 Minutes")]
    void Context_Simulate30Minutes()
    {
        Debug.Log("=== SIMULATING 30 MINUTES ===");
        AddProgressToQuests(30);
        totalMinutesTracked += 30;
        Debug.Log($"Total minutes tracked: {totalMinutesTracked}");
    }

    [ContextMenu("Debug: Reset Elapsed Time")]
    void Context_ResetElapsedTime()
    {
        ResetElapsedTime();
        Debug.Log("✓ Elapsed time reset");
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== PLAYTIME TRACKER STATUS ===");
        Debug.Log($"Quest IDs: {(questIdsToProgressPerMinute != null ? string.Join(", ", questIdsToProgressPerMinute) : "NULL")}");
        Debug.Log($"Track when inactive: {trackWhenInactive}");
        Debug.Log($"Elapsed seconds: {elapsed:F1}s");
        Debug.Log($"Total minutes tracked: {totalMinutesTracked}");
        Debug.Log($"Time until next minute: {(60f - elapsed):F1}s");
        Debug.Log($"QuestManager: {(QuestManager.Instance != null ? "OK ✓" : "NULL ❌")}");
        
        if (QuestManager.Instance != null && questIdsToProgressPerMinute != null)
        {
            Debug.Log("\nQuest Progress:");
            foreach (var questId in questIdsToProgressPerMinute)
            {
                if (string.IsNullOrEmpty(questId)) continue;
                
                var questData = QuestManager.Instance.GetQuestData(questId);
                var progress = QuestManager.Instance.GetProgress(questId);
                
                if (questData != null && progress != null)
                {
                    Debug.Log($"  {questId}: {progress.progress}/{questData.requiredAmount} min {(progress.claimed ? "[CLAIMED]" : "")}");
                }
                else
                {
                    Debug.Log($"  {questId}: NOT FOUND or NO PROGRESS");
                }
            }
        }
        
        Debug.Log("================================");
    }
}