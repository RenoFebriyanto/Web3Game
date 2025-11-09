using UnityEngine;

/// <summary>
/// ✅✅✅ IMPROVED v2: Coin grant tracker dengan MULTIPLE QUEST IDs support
/// Tracks coin gains untuk quest "Dapatkan X coin"
/// - Support multiple quest IDs (array)
/// - Auto-subscribe ke PlayerEconomy events
/// - Tracks only GAINS (not spending)
/// - Better logging & debugging
/// </summary>
public class CoinGrantTracker : MonoBehaviour
{
    [Header("Quest Setup")]
    [Tooltip("Quest IDs to progress when player gains coins (supports multiple quests)")]
    public string[] questIds = new string[] { "dailyquest2" }; // Default: ["Dapatkan 2000 coin"]
    // Example: ["dailyquest2", "wk_coin5k", "wk_coin10k", "wk_coin3k"]

    [Header("Tracking Settings")]
    [Tooltip("Track coin gains only? (ignore spending)")]
    public bool trackGainsOnly = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    [Tooltip("Log every coin change")]
    public bool logCoinChanges = false;

    private long lastCoins = -1;
    private long totalCoinsGained = 0;
    private bool isSubscribed = false;

    void Start()
    {
        Log("=== COIN GRANT TRACKER START ===");
        
        if (questIds == null || questIds.Length == 0)
        {
            LogError("❌ No quest IDs assigned! Add quest IDs in Inspector.");
            LogError("Example: dailyquest2, wk_coin5k, wk_coin10k (for 'Get X coins' quests)");
            return;
        }

        Log($"Tracking quest IDs: {string.Join(", ", questIds)}");
        Log($"Track gains only: {trackGainsOnly}");
        
        SubscribeToEconomy();
        
        Log("==============================");
    }

    void OnEnable()
    {
        SubscribeToEconomy();
    }

    void OnDisable()
    {
        UnsubscribeFromEconomy();
    }

    void OnDestroy()
    {
        UnsubscribeFromEconomy();
    }

    /// <summary>
    /// Subscribe ke PlayerEconomy events
    /// </summary>
    void SubscribeToEconomy()
    {
        if (isSubscribed)
        {
            return;
        }

        if (PlayerEconomy.Instance == null)
        {
            LogWarning("PlayerEconomy.Instance is null! Will retry...");
            Invoke(nameof(RetrySubscribe), 0.5f);
            return;
        }

        // Get initial coin count
        lastCoins = PlayerEconomy.Instance.Coins;
        
        // Subscribe to economy changed event
        PlayerEconomy.Instance.OnEconomyChanged -= OnEconomyChanged;
        PlayerEconomy.Instance.OnEconomyChanged += OnEconomyChanged;
        
        isSubscribed = true;
        
        Log($"✓ Subscribed to PlayerEconomy events");
        Log($"Initial coin count: {lastCoins:N0}");
    }

    void RetrySubscribe()
    {
        Log("Retrying subscribe to PlayerEconomy...");
        SubscribeToEconomy();
    }

    void UnsubscribeFromEconomy()
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.OnEconomyChanged -= OnEconomyChanged;
            Log("✓ Unsubscribed from PlayerEconomy events");
        }
        
        isSubscribed = false;
    }

    /// <summary>
    /// Called when economy changes (coins, shards, energy)
    /// </summary>
    void OnEconomyChanged()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("PlayerEconomy.Instance is null in OnEconomyChanged!");
            return;
        }

        long currentCoins = PlayerEconomy.Instance.Coins;
        
        // Initialize lastCoins on first change
        if (lastCoins < 0)
        {
            lastCoins = currentCoins;
            Log($"Initial coin count set: {currentCoins:N0}");
            return;
        }

        // Calculate delta
        long delta = currentCoins - lastCoins;
        
        if (logCoinChanges)
        {
            Log($"Coin change: {lastCoins:N0} -> {currentCoins:N0} (delta: {delta:+#;-#;0})");
        }

        // Only track gains (positive delta)
        if (delta > 0)
        {
            totalCoinsGained += delta;
            
            if (logCoinChanges)
            {
                Log($"💰 Coin GAINED: +{delta:N0} (total gained this session: {totalCoinsGained:N0})");
            }

            // Add progress to all configured quests
            AddProgressToQuests((int)delta);
        }
        else if (delta < 0 && !trackGainsOnly)
        {
            // Track spending if trackGainsOnly is false
            if (logCoinChanges)
            {
                Log($"💸 Coin SPENT: {delta:N0}");
            }
        }

        // Update lastCoins
        lastCoins = currentCoins;
    }

    /// <summary>
    /// Add progress to all configured quests
    /// </summary>
    void AddProgressToQuests(int amount)
    {
        if (questIds == null || questIds.Length == 0)
        {
            LogError("❌ No quest IDs configured!");
            return;
        }

        if (QuestManager.Instance == null)
        {
            LogError("❌ QuestManager.Instance is null!");
            return;
        }

        Log($"========================================");
        Log($"💰 COIN PROGRESS UPDATE: +{amount:N0} coins");

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
            QuestManager.Instance.AddProgress(questId, amount);
            successCount++;

            Log($"✅ Progress added to: {questId}");
            Log($"   Quest Title: {questData.title}");
            Log($"   Amount Added: +{amount:N0} coins");

            // Get current progress
            var progress = QuestManager.Instance.GetProgress(questId);
            if (progress != null)
            {
                Log($"   Current Progress: {progress.progress:N0}/{questData.requiredAmount:N0} coins");
                
                float percentage = (float)progress.progress / questData.requiredAmount * 100f;
                Log($"   Completion: {percentage:F1}%");
                
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
        
        Log($"✓ Updated {successCount}/{questIds.Length} quests");
        Log($"========================================");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Get total coins gained this session
    /// </summary>
    public long GetTotalCoinsGained()
    {
        return totalCoinsGained;
    }

    /// <summary>
    /// Reset total coins gained counter
    /// </summary>
    public void ResetTotalCoinsGained()
    {
        totalCoinsGained = 0;
        Log("✓ Total coins gained reset");
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

    /// <summary>
    /// Remove quest ID at runtime
    /// </summary>
    public void RemoveQuestId(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            LogError("Cannot remove empty quest ID");
            return;
        }

        var list = new System.Collections.Generic.List<string>(questIds);
        if (list.Remove(questId))
        {
            questIds = list.ToArray();
            Log($"✓ Removed quest ID: {questId}");
        }
        else
        {
            Log($"Quest ID '{questId}' not found");
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CoinGrantTracker] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[CoinGrantTracker] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[CoinGrantTracker] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Test: Simulate Gain 100 Coins")]
    void Context_SimulateGain100()
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddCoins(100);
            Debug.Log("✓ Simulated +100 coins");
        }
        else
        {
            Debug.LogError("❌ PlayerEconomy.Instance is null!");
        }
    }

    [ContextMenu("Test: Simulate Gain 1000 Coins")]
    void Context_SimulateGain1000()
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddCoins(1000);
            Debug.Log("✓ Simulated +1000 coins");
        }
        else
        {
            Debug.LogError("❌ PlayerEconomy.Instance is null!");
        }
    }

    [ContextMenu("Test: Simulate Gain 5000 Coins")]
    void Context_SimulateGain5000()
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddCoins(5000);
            Debug.Log("✓ Simulated +5000 coins");
        }
        else
        {
            Debug.LogError("❌ PlayerEconomy.Instance is null!");
        }
    }

    [ContextMenu("Test: Simulate Gain 10000 Coins")]
    void Context_SimulateGain10000()
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddCoins(10000);
            Debug.Log("✓ Simulated +10000 coins");
        }
        else
        {
            Debug.LogError("❌ PlayerEconomy.Instance is null!");
        }
    }

    [ContextMenu("Debug: Reset Total Gained")]
    void Context_ResetTotalGained()
    {
        ResetTotalCoinsGained();
        Debug.Log("✓ Total coins gained reset");
    }

    [ContextMenu("Debug: Force Resubscribe")]
    void Context_ForceResubscribe()
    {
        UnsubscribeFromEconomy();
        SubscribeToEconomy();
        Debug.Log("✓ Force resubscribed");
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== COIN GRANT TRACKER STATUS ===");
        Debug.Log($"Quest IDs: {(questIds != null ? string.Join(", ", questIds) : "NULL")}");
        Debug.Log($"Quest Count: {(questIds != null ? questIds.Length : 0)}");
        Debug.Log($"Track gains only: {trackGainsOnly}");
        Debug.Log($"Is subscribed: {isSubscribed}");
        Debug.Log($"Last coins: {lastCoins:N0}");
        Debug.Log($"Total gained (session): {totalCoinsGained:N0}");
        Debug.Log($"PlayerEconomy: {(PlayerEconomy.Instance != null ? "OK ✓" : "NULL ❌")}");
        Debug.Log($"QuestManager: {(QuestManager.Instance != null ? "OK ✓" : "NULL ❌")}");
        
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log($"\nCurrent economy:");
            Debug.Log($"  Coins: {PlayerEconomy.Instance.Coins:N0}");
        }
        
        if (QuestManager.Instance != null && questIds != null)
        {
            Debug.Log($"\nQuest Progress:");
            foreach (string questId in questIds)
            {
                if (string.IsNullOrEmpty(questId)) continue;
                
                var questData = QuestManager.Instance.GetQuestData(questId);
                var progress = QuestManager.Instance.GetProgress(questId);
                
                if (questData != null && progress != null)
                {
                    Debug.Log($"  {questId}: {progress.progress:N0}/{questData.requiredAmount:N0} coins {(progress.claimed ? "[CLAIMED]" : "")}");
                    
                    float percentage = (float)progress.progress / questData.requiredAmount * 100f;
                    Debug.Log($"    └─ Completion: {percentage:F1}%");
                }
                else
                {
                    Debug.Log($"  {questId}: NOT FOUND or NO PROGRESS");
                }
            }
        }
        
        Debug.Log("=================================");
    }

    [ContextMenu("Debug: Print Quest IDs")]
    void Context_PrintQuestIds()
    {
        Debug.Log("=== CONFIGURED QUEST IDs ===");
        if (questIds == null || questIds.Length == 0)
        {
            Debug.Log("❌ No quest IDs configured!");
        }
        else
        {
            Debug.Log($"Total: {questIds.Length} quest(s)");
            for (int i = 0; i < questIds.Length; i++)
            {
                Debug.Log($"  [{i}] {questIds[i]}");
            }
        }
        Debug.Log("============================");
    }
}