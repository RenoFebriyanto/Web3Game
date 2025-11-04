using UnityEngine;

/// <summary>
/// FINAL FIX: GameBootstrap yang TIDAK PERNAH dihancurkan
/// Buat GameObject baru di MainMenu: "GameBootstrap"
/// Attach script ini
/// Set execution order: -1000 (Edit > Project Settings > Script Execution Order)
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Enable to see debug logs")]
    public bool enableDebugLogs = true;

    void Awake()
    {
        // ✅ CRITICAL: Singleton pattern dengan PERSISTENT flag
        if (Instance != null && Instance != this)
        {
            Log($"Duplicate GameBootstrap found on '{gameObject.name}' - DESTROYING");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ✅ CRITICAL: Mark as persistent IMMEDIATELY
        DontDestroyOnLoad(gameObject);

        // Add unique tag to prevent destruction
        gameObject.tag = "PersistentBootstrap";
        gameObject.name = "[GameBootstrap - PERSISTENT]";

        Log("✓ GameBootstrap initialized and marked as DontDestroyOnLoad");

        // Ensure PlayerEconomy exists
        EnsurePlayerEconomy();
    }

    void EnsurePlayerEconomy()
    {
        if (PlayerEconomy.Instance != null)
        {
            Log("PlayerEconomy already exists");
            return;
        }

        Log("Creating PlayerEconomy...");

        // Try load from Resources
        var prefab = Resources.Load<GameObject>("EconomyManager");
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager";
            Log("✓ Created EconomyManager from Resources");
        }
        else
        {
            // Fallback: create empty GameObject
            var go = new GameObject("PlayerEconomy");
            go.AddComponent<PlayerEconomy>();
            DontDestroyOnLoad(go);
            Log("✓ Created fallback PlayerEconomy");
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            LogWarning("GameBootstrap is being destroyed! This should NOT happen!");
            Instance = null;
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[GameBootstrap] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[GameBootstrap] {message}");
    }

    // ========================================
    // CONTEXT MENU
    // ========================================

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== GAMEBOOTSTRAP STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"GameObject name: {gameObject.name}");
        Debug.Log($"Tag: {gameObject.tag}");
        Debug.Log($"DontDestroyOnLoad: YES (persistent)");
        Debug.Log($"PlayerEconomy: {(PlayerEconomy.Instance != null ? "OK" : "NULL")}");
        Debug.Log("============================");
    }
}