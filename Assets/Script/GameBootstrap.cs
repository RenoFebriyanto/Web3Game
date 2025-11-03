using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// COMPLETE FIX: GameBootstrap yang persistent dengan proper scene management
/// Attach ke GameObject "GameBootstrap" di MainMenu scene
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private static bool hasInitialized = false;
    private string lastSceneName = "";

    void Awake()
    {
        Log("=== GAMEBOOTSTRAP AWAKE ===");
        Log($"Current scene: {SceneManager.GetActiveScene().name}");
        Log($"hasInitialized: {hasInitialized}");
        Log($"Instance: {(Instance != null ? "EXISTS" : "NULL")}");

        // ✅ CRITICAL FIX: Proper singleton with scene tracking
        if (Instance != null && Instance != this)
        {
            LogWarning($"Duplicate GameBootstrap found! Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initialize managers
        if (!hasInitialized)
        {
            InitializeManagers();
            hasInitialized = true;
        }

        Log("✓ GameBootstrap initialized successfully");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"=== SCENE LOADED: {scene.name} ===");

        lastSceneName = scene.name;

        // ✅ CRITICAL: Re-verify all managers still exist
        VerifyManagers();

        // ✅ Refresh UI jika di MainMenu
        if (scene.name == "MainMenu")
        {
            Invoke(nameof(RefreshMainMenuUI), 0.2f);
        }
    }

    void InitializeManagers()
    {
        Log("Initializing managers...");

        // Check ShopManager
        var shopManager = GetComponent<ShopManager>();
        if (shopManager == null)
        {
            LogError("ShopManager component missing on GameBootstrap!");
        }
        else
        {
            Log("✓ ShopManager found");
        }

        // Check QuestManager
        var questManager = GetComponent<QuestManager>();
        if (questManager == null)
        {
            LogError("QuestManager component missing on GameBootstrap!");
        }
        else
        {
            Log("✓ QuestManager found");
        }

        // Check QuestChestController
        var chestController = GetComponent<QuestChestController>();
        if (chestController == null)
        {
            LogError("QuestChestController component missing on GameBootstrap!");
        }
        else
        {
            Log("✓ QuestChestController found");
        }
    }

    void VerifyManagers()
    {
        Log("Verifying managers...");

        if (this == null)
        {
            LogError("GameBootstrap THIS is NULL! (destroyed?)");
            return;
        }

        // Re-check components
        var shopManager = GetComponent<ShopManager>();
        var questManager = GetComponent<QuestManager>();
        var chestController = GetComponent<QuestChestController>();

        if (shopManager == null)
        {
            LogError("❌ ShopManager component MISSING after scene load!");
        }
        else
        {
            Log("✓ ShopManager still exists");
        }

        if (questManager == null)
        {
            LogError("❌ QuestManager component MISSING after scene load!");
        }
        else
        {
            Log("✓ QuestManager still exists");
        }

        if (chestController == null)
        {
            LogError("❌ QuestChestController component MISSING after scene load!");
        }
        else
        {
            Log("✓ QuestChestController still exists");
        }
    }

    void RefreshMainMenuUI()
    {
        Log("=== REFRESHING MAINMENU UI ===");

        // Find ShopManager
        var shopManager = GetComponent<ShopManager>();
        if (shopManager != null)
        {
            // Force repopulate shop
            shopManager.PopulateShop();
            Log("✓ Shop repopulated");
        }
        else
        {
            LogError("ShopManager not found for refresh!");
        }

        // Find QuestManager
        var questManager = GetComponent<QuestManager>();
        if (questManager != null)
        {
            // Quest UI auto-refreshes on Start, no need to force
            Log("✓ QuestManager ready");
        }
        else
        {
            LogError("QuestManager not found for refresh!");
        }
    }

    void OnDestroy()
    {
        Log("=== GAMEBOOTSTRAP DESTROYED ===");

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
            hasInitialized = false;
            LogWarning("GameBootstrap Instance cleared!");
        }
    }

    void OnApplicationQuit()
    {
        hasInitialized = false;
        Instance = null;
        Log("Application quit - reset flags");
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

    void LogError(string message)
    {
        Debug.LogError($"[GameBootstrap] {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("========================================");
        Debug.Log("   GAMEBOOTSTRAP STATUS");
        Debug.Log("========================================");
        Debug.Log($"Instance: {(Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"This GameObject: {gameObject.name}");
        Debug.Log($"Active: {gameObject.activeInHierarchy}");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Last Scene: {lastSceneName}");
        Debug.Log($"Has Initialized: {hasInitialized}");
        Debug.Log("");
        Debug.Log("Components:");
        Debug.Log($"  ShopManager: {(GetComponent<ShopManager>() != null ? "OK" : "MISSING")}");
        Debug.Log($"  QuestManager: {(GetComponent<QuestManager>() != null ? "OK" : "MISSING")}");
        Debug.Log($"  QuestChestController: {(GetComponent<QuestChestController>() != null ? "OK" : "MISSING")}");
        Debug.Log("========================================");
    }

    [ContextMenu("Debug: Force Verify")]
    void Context_ForceVerify()
    {
        VerifyManagers();
    }

    [ContextMenu("Debug: Force Refresh UI")]
    void Context_ForceRefresh()
    {
        RefreshMainMenuUI();
    }
}