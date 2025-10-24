using UnityEngine;

/// <summary>
/// Dev helper untuk testing quest system.
/// Tambahkan GameObject ini di scene dan assign QuestChestController.
/// Panggil method ini dari Button.onClick atau manual di Inspector.
/// </summary>
public class DevQuestTester : MonoBehaviour
{
    [Header("Quest Testing")]
    [Tooltip("Quest ID untuk testing AddProgress")]
    public string testQuestId;
    public int addAmount = 1;

    [Header("Crate Testing (optional)")]
    [Tooltip("Assign QuestChestController untuk crate reset")]
    public QuestChestController crateController;

    void Start()
    {
        // Auto-find QuestChestController jika tidak di-assign
        if (crateController == null)
        {
            crateController = FindObjectOfType<QuestChestController>();
        }
    }

    // ========================================
    // QUEST METHODS
    // ========================================

    /// <summary>
    /// Tambah progress ke quest tertentu
    /// </summary>
    public void AddProgressNow()
    {
        if (!string.IsNullOrEmpty(testQuestId))
        {
            QuestManager.Instance?.AddProgress(testQuestId, addAmount);
            Debug.Log($"[DevQuestTester] Added {addAmount} progress to quest '{testQuestId}'");
        }
        else
        {
            Debug.LogWarning("[DevQuestTester] testQuestId is empty!");
        }
    }

    /// <summary>
    /// Reset semua daily quest (progress & claimed status)
    /// </summary>
    public void ResetDailyNow()
    {
        QuestManager.Instance?.ResetDaily();
        Debug.Log("[DevQuestTester] Daily quests reset!");
    }

    /// <summary>
    /// Reset semua weekly quest (progress & claimed status)
    /// </summary>
    public void ResetWeeklyNow()
    {
        QuestManager.Instance?.ResetWeekly();
        Debug.Log("[DevQuestTester] Weekly quests reset!");
    }

    // ========================================
    // CRATE METHODS (NEW!)
    // ========================================

    /// <summary>
    /// Reset semua crate progress (claimedCount & crate status)
    /// </summary>
    public void ResetCratesNow()
    {
        if (crateController == null)
        {
            Debug.LogWarning("[DevQuestTester] crateController is not assigned! Trying to find...");
            crateController = FindObjectOfType<QuestChestController>();
        }

        if (crateController != null)
        {
            // Call context menu method via reflection (karena ContextMenu tidak bisa dipanggil langsung)
            var method = crateController.GetType().GetMethod("DebugResetCrates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(crateController, null);
                Debug.Log("[DevQuestTester] Crate progress reset!");
            }
            else
            {
                Debug.LogError("[DevQuestTester] DebugResetCrates method not found!");
            }
        }
        else
        {
            Debug.LogError("[DevQuestTester] QuestChestController not found in scene!");
        }
    }

    /// <summary>
    /// Clear saved crate progress dari PlayerPrefs
    /// </summary>
    public void ClearCrateSaveNow()
    {
        if (crateController == null)
        {
            Debug.LogWarning("[DevQuestTester] crateController is not assigned! Trying to find...");
            crateController = FindObjectOfType<QuestChestController>();
        }

        if (crateController != null)
        {
            // Call context menu method via reflection
            var method = crateController.GetType().GetMethod("DebugClearSavedProgress",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(crateController, null);
                Debug.Log("[DevQuestTester] Crate saved progress cleared from PlayerPrefs!");
            }
            else
            {
                Debug.LogError("[DevQuestTester] DebugClearSavedProgress method not found!");
            }
        }
        else
        {
            Debug.LogError("[DevQuestTester] QuestChestController not found in scene!");
        }
    }

    /// <summary>
    /// Simulasi claim 1 quest (crate progress++)
    /// </summary>
    public void SimulateQuestClaimNow()
    {
        if (crateController == null)
        {
            Debug.LogWarning("[DevQuestTester] crateController is not assigned! Trying to find...");
            crateController = FindObjectOfType<QuestChestController>();
        }

        if (crateController != null)
        {
            // Call DebugAddProgress method via reflection
            var method = crateController.GetType().GetMethod("DebugAddProgress",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(crateController, null);
                Debug.Log("[DevQuestTester] Simulated quest claim (crate progress +1)");
            }
            else
            {
                Debug.LogError("[DevQuestTester] DebugAddProgress method not found!");
            }
        }
        else
        {
            Debug.LogError("[DevQuestTester] QuestChestController not found in scene!");
        }
    }

    // ========================================
    // COMBINED METHODS
    // ========================================

    /// <summary>
    /// Reset SEMUA (quest + crate)
    /// </summary>
    public void ResetAllNow()
    {
        ResetDailyNow();
        ResetWeeklyNow();
        ResetCratesNow();
        Debug.Log("[DevQuestTester] ===== RESET ALL (Quest + Crate) =====");
    }

    /// <summary>
    /// Clear SEMUA saved data (PlayerPrefs)
    /// </summary>
    public void ClearAllSavedDataNow()
    {
        // Clear quest progress
        PlayerPrefs.DeleteKey("QuestProgress_v1");

        // Clear crate progress
        ClearCrateSaveNow();

        PlayerPrefs.Save();

        Debug.Log("[DevQuestTester] ===== CLEARED ALL SAVED DATA =====");
        Debug.Log("[DevQuestTester] Restart Play Mode to see fresh state!");
    }

    // ========================================
    // CONTEXT MENU (untuk quick access)
    // ========================================

    [ContextMenu("Add Quest Progress")]
    void Context_AddProgress() => AddProgressNow();

    [ContextMenu("Reset Daily Quests")]
    void Context_ResetDaily() => ResetDailyNow();

    [ContextMenu("Reset Weekly Quests")]
    void Context_ResetWeekly() => ResetWeeklyNow();

    [ContextMenu("Reset Crate Progress")]
    void Context_ResetCrates() => ResetCratesNow();

    [ContextMenu("Simulate Quest Claim (Crate +1)")]
    void Context_SimulateClaim() => SimulateQuestClaimNow();

    [ContextMenu("===== RESET ALL =====")]
    void Context_ResetAll() => ResetAllNow();

    [ContextMenu("===== CLEAR ALL SAVED DATA =====")]
    void Context_ClearAll() => ClearAllSavedDataNow();
}