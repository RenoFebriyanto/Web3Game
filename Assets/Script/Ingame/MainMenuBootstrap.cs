using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// FIXED: Ensures PlayerEconomy exists + detects missing GameBootstrap
/// </summary>
[DefaultExecutionOrder(-900)]
public class MainMenuBootstrap : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebugLogs = true;

    void Awake()
    {
        Log("=== MAINMENU BOOTSTRAP AWAKE ===");

        // ✅ CRITICAL: Ensure PlayerEconomy exists
        EnsurePlayerEconomy();
    }

    void Start()
    {
        Log("MainMenuBootstrap Start");

        // ✅ Refresh UI
        Invoke(nameof(RefreshAllUI), 0.1f);

        // ✅ Check for missing GameBootstrap
        Invoke(nameof(CheckGameBootstrap), 0.2f);
    }

    void EnsurePlayerEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogWarning("PlayerEconomy.Instance is NULL! Creating...");
            SceneBootstrapper.EnsureEconomyExists();

            // Wait then verify
            Invoke(nameof(VerifyPlayerEconomy), 0.05f);
        }
        else
        {
            Log($"✓ PlayerEconomy exists (Coins: {PlayerEconomy.Instance.Coins})");
        }
    }

    void VerifyPlayerEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("❌ CRITICAL: PlayerEconomy STILL NULL!");
            LogError("Check Resources/EconomyManager.prefab!");
        }
        else
        {
            Log("✓ PlayerEconomy verified");
            RefreshAllUI();
        }
    }

    // ✅ NEW: Check if GameBootstrap exists in hierarchy
    void CheckGameBootstrap()
    {
        // Check jika ada GameObject bernama "GameBootstrap" atau "EconomyManager" di scene
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        bool foundBootstrap = false;
        bool foundEconomy = false;

        foreach (var obj in rootObjects)
        {
            if (obj.name.Contains("Bootstrap"))
            {
                foundBootstrap = true;
                Log($"Found bootstrap object: {obj.name}");
            }
            if (obj.name.Contains("Economy"))
            {
                foundEconomy = true;
                Log($"Found economy object: {obj.name}");
            }
        }

        if (!foundBootstrap)
        {
            LogWarning("⚠️ No Bootstrap GameObject found in MainMenu scene!");
            LogWarning("This is OK if PlayerEconomy exists in DontDestroyOnLoad");
        }

        // ✅ Check DontDestroyOnLoad objects
        if (PlayerEconomy.Instance != null)
        {
            Log($"✓ PlayerEconomy in DontDestroyOnLoad: {PlayerEconomy.Instance.gameObject.name}");
        }
    }

    void RefreshAllUI()
    {
        Log("Refreshing all UI...");

        var uiControllers = FindObjectsByType<EconomyUIController>(FindObjectsSortMode.None);

        if (uiControllers.Length == 0)
        {
            LogWarning("No EconomyUIController found!");
        }
        else
        {
            Log($"Found {uiControllers.Length} EconomyUIController(s)");

            foreach (var controller in uiControllers)
            {
                if (controller != null)
                {
                    controller.gameObject.SetActive(false);
                    controller.gameObject.SetActive(true);
                    Log($"✓ Refreshed {controller.gameObject.name}");
                }
            }
        }
    }

    void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[MainMenuBootstrap] {msg}");
    }

    void LogWarning(string msg)
    {
        Debug.LogWarning($"[MainMenuBootstrap] {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[MainMenuBootstrap] {msg}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("🔍 Debug: Check Status")]
    void Context_CheckStatus()
    {
        Debug.Log("=== MAINMENU BOOTSTRAP STATUS ===");
        Debug.Log($"PlayerEconomy.Instance: {(PlayerEconomy.Instance != null ? "EXISTS ✓" : "NULL ❌")}");

        if (PlayerEconomy.Instance != null)
        {
            Debug.Log($"  GameObject: {PlayerEconomy.Instance.gameObject.name}");
            Debug.Log($"  Scene: {PlayerEconomy.Instance.gameObject.scene.name}");
            Debug.Log($"  Coins: {PlayerEconomy.Instance.Coins}");
        }

        // Check scene root objects
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        Debug.Log($"Scene root objects: {rootObjects.Length}");
        foreach (var obj in rootObjects)
        {
            if (obj.name.Contains("Bootstrap") || obj.name.Contains("Economy"))
            {
                Debug.Log($"  - {obj.name}");
            }
        }

        Debug.Log("================================");
    }

    [ContextMenu("🔄 Force Refresh UI")]
    void Context_RefreshUI()
    {
        RefreshAllUI();
    }

    [ContextMenu("✅ Force Create Economy")]
    void Context_ForceCreate()
    {
        SceneBootstrapper.EnsureEconomyExists();
        Invoke(nameof(RefreshAllUI), 0.2f);
    }
}