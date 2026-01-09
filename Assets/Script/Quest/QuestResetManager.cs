using System;
using UnityEngine;

/// <summary>
/// ✅✅✅ FIXED: QuestResetManager yang TIDAK MERUSAK QuestManager GameObject
/// - HARUS di GameObject TERPISAH (BUKAN di QuestManager GameObject!)
/// - DontDestroyOnLoad untuk persistent
/// - Auto-reset Daily/Weekly Quest & Crate Progress
/// 
/// CRITICAL SETUP:
/// 1. Buat GameObject baru: "[QuestResetManager - PERSISTENT]"
/// 2. Attach script INI ke GameObject tersebut
/// 3. JANGAN attach ke GameObject QuestManager!
/// </summary>
[DefaultExecutionOrder(-950)]
public class QuestResetManager : MonoBehaviour
{
    public static QuestResetManager Instance { get; private set; }

    [Header("⚠️ CRITICAL: Attach to SEPARATE GameObject!")]
    [Tooltip("JANGAN attach ke QuestManager GameObject!")]
    public bool acknowledgeWarning = false;

    [Header("Reset Intervals")]
    [Tooltip("Daily reset interval (jam)")]
    public int dailyResetHours = 24; // 1 hari

    [Tooltip("Weekly reset interval (jam)")]
    public int weeklyResetHours = 168; // 7 hari (7 x 24)

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // PlayerPrefs keys
    const string PREF_LAST_DAILY_RESET = "Kulino_LastDailyReset_v1";
    const string PREF_LAST_WEEKLY_RESET = "Kulino_LastWeeklyReset_v1";

    void Awake()
    {
        // ✅ FIX: Singleton pattern yang AMAN
        if (Instance != null)
        {
            if (Instance != this)
            {
                Log($"⚠️ Duplicate QuestResetManager found - destroying duplicate only");
                
                // ✅ CRITICAL: Hanya destroy SCRIPT ini, BUKAN GameObject!
                Destroy(this);
                return;
            }
        }

        Instance = this;
        
        // ✅ Mark GameObject as persistent
        DontDestroyOnLoad(gameObject);
        
        // ✅ Rename untuk identifikasi
        if (gameObject.name != "[QuestResetManager - PERSISTENT]")
        {
            gameObject.name = "[QuestResetManager - PERSISTENT]";
        }

        Log("✅ QuestResetManager initialized (on separate GameObject)");
        
        // ✅ Validate setup
        ValidateSetup();
    }

    void Start()
    {
        // Check resets on start
        CheckAndResetDaily();
        CheckAndResetWeekly();
    }

    void OnDestroy()
{
    if (Instance == this)
    {
        Instance = null;
        Log("QuestResetManager destroyed");
    }
}

    /// <summary>
    /// ✅ NEW: Validate setup untuk prevent common mistakes
    /// </summary>
    void ValidateSetup()
    {
        // Check jika attached ke QuestManager GameObject
        var questManager = GetComponent<QuestManager>();
        if (questManager != null)
        {
            LogError("❌❌❌ CRITICAL ERROR!");
            LogError("QuestResetManager is attached to QuestManager GameObject!");
            LogError("This will cause QuestManager to be destroyed on scene change!");
            LogError("");
            LogError("FIX:");
            LogError("1. Remove QuestResetManager from QuestManager GameObject");
            LogError("2. Create NEW GameObject: [QuestResetManager - PERSISTENT]");
            LogError("3. Attach QuestResetManager to that new GameObject");
            LogError("");
            
            #if UNITY_EDITOR
            // Pause editor untuk force user attention
            Debug.Break();
            #endif
        }
        else
        {
            Log("✅ Setup validation passed - QuestResetManager is on separate GameObject");
        }
    }

    /// <summary>
    /// Check dan reset daily quest jika sudah 24 jam
    /// </summary>
    public void CheckAndResetDaily()
    {
        string lastResetStr = PlayerPrefs.GetString(PREF_LAST_DAILY_RESET, "");

        if (string.IsNullOrEmpty(lastResetStr))
        {
            // First time - set current time
            SaveDailyResetTime();
            Log("✓ First time - daily reset time initialized");
            return;
        }

        try
        {
            DateTime lastReset = DateTime.Parse(lastResetStr);
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - lastReset;

            double hoursElapsed = elapsed.TotalHours;

            Log($"Daily: Last reset {hoursElapsed:F1} hours ago");

            if (hoursElapsed >= dailyResetHours)
            {
                Log($"✅ DAILY RESET TRIGGERED ({hoursElapsed:F1} hours elapsed)");
                ResetDaily();
                SaveDailyResetTime();
            }
            else
            {
                double hoursRemaining = dailyResetHours - hoursElapsed;
                Log($"Daily reset in {hoursRemaining:F1} hours");
            }
        }
        catch (Exception e)
        {
            LogError($"Failed to parse daily reset time: {e.Message}");
            SaveDailyResetTime();
        }
    }

    /// <summary>
    /// Check dan reset weekly quest + crate jika sudah 7 hari
    /// </summary>
    public void CheckAndResetWeekly()
    {
        string lastResetStr = PlayerPrefs.GetString(PREF_LAST_WEEKLY_RESET, "");

        if (string.IsNullOrEmpty(lastResetStr))
        {
            // First time - set current time
            SaveWeeklyResetTime();
            Log("✓ First time - weekly reset time initialized");
            return;
        }

        try
        {
            DateTime lastReset = DateTime.Parse(lastResetStr);
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - lastReset;

            double hoursElapsed = elapsed.TotalHours;

            Log($"Weekly: Last reset {hoursElapsed:F1} hours ago ({(hoursElapsed / 24):F1} days)");

            if (hoursElapsed >= weeklyResetHours)
            {
                Log($"✅ WEEKLY RESET TRIGGERED ({(hoursElapsed / 24):F1} days elapsed)");
                ResetWeekly();
                SaveWeeklyResetTime();
            }
            else
            {
                double hoursRemaining = weeklyResetHours - hoursElapsed;
                double daysRemaining = hoursRemaining / 24;
                Log($"Weekly reset in {daysRemaining:F1} days ({hoursRemaining:F1} hours)");
            }
        }
        catch (Exception e)
        {
            LogError($"Failed to parse weekly reset time: {e.Message}");
            SaveWeeklyResetTime();
        }
    }

    /// <summary>
    /// Reset daily quest
    /// </summary>
    void ResetDaily()
    {
        Log("========================================");
        Log("🔄 RESETTING DAILY QUEST");
        Log("========================================");

        // ✅ Wait for QuestManager jika belum ready
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null! Will retry in 1 second...");
            Invoke(nameof(RetryResetDaily), 1f);
            return;
        }

        QuestManager.Instance.ResetDaily();
        Log("✓ Daily quest reset via QuestManager");

        Log("========================================");
        Log("✅ DAILY RESET COMPLETE");
        Log("========================================");
    }

    void RetryResetDaily()
    {
        Log("Retrying daily reset...");
        ResetDaily();
    }

    /// <summary>
    /// Reset weekly quest + crate progress
    /// </summary>
    void ResetWeekly()
    {
        Log("========================================");
        Log("🔄 RESETTING WEEKLY QUEST & CRATE PROGRESS");
        Log("========================================");

        // ✅ Wait for QuestManager jika belum ready
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager.Instance is null! Will retry in 1 second...");
            Invoke(nameof(RetryResetWeekly), 1f);
            return;
        }

        // Reset weekly quest
        QuestManager.Instance.ResetWeekly();
        Log("✓ Weekly quest reset via QuestManager");

        // Reset crate progress
        if (QuestChestController.Instance != null)
        {
            QuestChestController.Instance.ResetCrateProgress();
            Log("✓ Crate progress reset via QuestChestController");
        }
        else
        {
            // Try to find in scene
            var crateController = FindFirstObjectByType<QuestChestController>();
            if (crateController != null)
            {
                crateController.ResetCrateProgress();
                Log("✓ Crate progress reset (found in scene)");
            }
            else
            {
                LogWarning("QuestChestController not found! Will try again later.");
            }
        }

        Log("========================================");
        Log("✅ WEEKLY RESET COMPLETE");
        Log("========================================");
    }

    void RetryResetWeekly()
    {
        Log("Retrying weekly reset...");
        ResetWeekly();
    }

    void SaveDailyResetTime()
    {
        string now = DateTime.Now.ToString("o"); // ISO 8601 format
        PlayerPrefs.SetString(PREF_LAST_DAILY_RESET, now);
        PlayerPrefs.Save();
        Log($"✓ Daily reset time saved: {now}");
    }

    void SaveWeeklyResetTime()
    {
        string now = DateTime.Now.ToString("o");
        PlayerPrefs.SetString(PREF_LAST_WEEKLY_RESET, now);
        PlayerPrefs.Save();
        Log($"✓ Weekly reset time saved: {now}");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Force reset daily (untuk testing)
    /// </summary>
    public void ForceResetDaily()
    {
        Log("⚠️ FORCE DAILY RESET");
        ResetDaily();
        SaveDailyResetTime();
    }

    /// <summary>
    /// Force reset weekly (untuk testing)
    /// </summary>
    public void ForceResetWeekly()
    {
        Log("⚠️ FORCE WEEKLY RESET");
        ResetWeekly();
        SaveWeeklyResetTime();
    }

    /// <summary>
    /// Get time until next daily reset
    /// </summary>
    public TimeSpan GetTimeUntilDailyReset()
    {
        string lastResetStr = PlayerPrefs.GetString(PREF_LAST_DAILY_RESET, "");
        if (string.IsNullOrEmpty(lastResetStr))
        {
            return TimeSpan.Zero;
        }

        try
        {
            DateTime lastReset = DateTime.Parse(lastResetStr);
            DateTime nextReset = lastReset.AddHours(dailyResetHours);
            TimeSpan remaining = nextReset - DateTime.Now;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Get time until next weekly reset
    /// </summary>
    public TimeSpan GetTimeUntilWeeklyReset()
    {
        string lastResetStr = PlayerPrefs.GetString(PREF_LAST_WEEKLY_RESET, "");
        if (string.IsNullOrEmpty(lastResetStr))
        {
            return TimeSpan.Zero;
        }

        try
        {
            DateTime lastReset = DateTime.Parse(lastResetStr);
            DateTime nextReset = lastReset.AddHours(weeklyResetHours);
            TimeSpan remaining = nextReset - DateTime.Now;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[QuestResetManager] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[QuestResetManager] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[QuestResetManager] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Force Reset Daily Now")]
    void Context_ForceResetDaily()
    {
        ForceResetDaily();
    }

    [ContextMenu("Force Reset Weekly Now")]
    void Context_ForceResetWeekly()
    {
        ForceResetWeekly();
    }

    [ContextMenu("Check Reset Status")]
    void Context_CheckStatus()
    {
        CheckAndResetDaily();
        CheckAndResetWeekly();
    }

    [ContextMenu("Print Reset Times")]
    void Context_PrintResetTimes()
    {
        Debug.Log("=== RESET TIMES ===");
        
        TimeSpan dailyRemaining = GetTimeUntilDailyReset();
        Debug.Log($"Daily reset in: {dailyRemaining.TotalHours:F1} hours");

        TimeSpan weeklyRemaining = GetTimeUntilWeeklyReset();
        Debug.Log($"Weekly reset in: {(weeklyRemaining.TotalHours / 24):F1} days");

        Debug.Log("===================");
    }

    [ContextMenu("Validate Setup")]
    void Context_ValidateSetup()
    {
        ValidateSetup();
    }

    [ContextMenu("Reset All Timers (Testing)")]
    void Context_ResetAllTimers()
    {
        PlayerPrefs.DeleteKey(PREF_LAST_DAILY_RESET);
        PlayerPrefs.DeleteKey(PREF_LAST_WEEKLY_RESET);
        PlayerPrefs.Save();
        Debug.Log("✓ All reset timers cleared");
    }
}