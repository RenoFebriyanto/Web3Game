using UnityEngine;

/// <summary>
/// MainMenu scene bootstrap - ensures PlayerEconomy exists
/// Attach this to a GameObject in MainMenu scene (e.g. Canvas or _GameManager)
/// </summary>
[DefaultExecutionOrder(-900)] // Run after SceneBootstrapper
public class MainMenuBootstrap : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebugLogs = true;

    void Awake()
    {
        Log("MainMenuBootstrap Awake");

        // ✅ CRITICAL: Ensure PlayerEconomy exists
        EnsurePlayerEconomy();

        // ✅ Refresh all UI
        RefreshAllUI();
    }

    void Start()
    {
        Log("MainMenuBootstrap Start");

        // ✅ Double-check after 1 frame
        Invoke(nameof(LateCheck), 0.1f);
    }

    void EnsurePlayerEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogWarning("PlayerEconomy.Instance is NULL! Calling EnsureEconomyExists...");
            SceneBootstrapper.EnsureEconomyExists();

            // Wait 1 frame then check again
            Invoke(nameof(VerifyPlayerEconomy), 0.05f);
        }
        else
        {
            Log("✓ PlayerEconomy exists");
        }
    }

    void VerifyPlayerEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("❌ CRITICAL: PlayerEconomy still NULL after EnsureEconomyExists!");
            LogError("Check if Resources/EconomyManager.prefab exists!");
        }
        else
        {
            Log("✓ PlayerEconomy verified after delay");
            RefreshAllUI();
        }
    }

    void LateCheck()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("❌ PlayerEconomy is NULL in Start! Layout will be broken!");
        }
        else
        {
            Log("✓ PlayerEconomy exists in Start");
        }
    }

    void RefreshAllUI()
    {
        Log("Refreshing all UI...");

        // Find all EconomyUIController components
        var uiControllers = FindObjectsByType<EconomyUIController>(FindObjectsSortMode.None);

        if (uiControllers.Length == 0)
        {
            LogWarning("No EconomyUIController found in scene!");
        }
        else
        {
            Log($"Found {uiControllers.Length} EconomyUIController(s), refreshing...");

            // Manually trigger refresh
            foreach (var controller in uiControllers)
            {
                if (controller != null)
                {
                    // Disable and re-enable to trigger OnEnable
                    controller.gameObject.SetActive(false);
                    controller.gameObject.SetActive(true);
                    Log($"✓ Refreshed {controller.gameObject.name}");
                }
            }
        }
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[MainMenuBootstrap] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[MainMenuBootstrap] ⚠️ {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[MainMenuBootstrap] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Force Refresh All UI")]
    void Context_RefreshAllUI()
    {
        RefreshAllUI();
    }

    [ContextMenu("Check PlayerEconomy Status")]
    void Context_CheckStatus()
    {
        Debug.Log("=== MAINMENU BOOTSTRAP STATUS ===");
        Debug.Log($"PlayerEconomy.Instance: {(PlayerEconomy.Instance != null ? "EXISTS ✓" : "NULL ❌")}");

        if (PlayerEconomy.Instance != null)
        {
            Debug.Log($"  Coins: {PlayerEconomy.Instance.Coins}");
            Debug.Log($"  Shards: {PlayerEconomy.Instance.Shards}");
            Debug.Log($"  Energy: {PlayerEconomy.Instance.Energy}/{PlayerEconomy.Instance.MaxEnergy}");
        }

        var uiControllers = FindObjectsByType<EconomyUIController>(FindObjectsSortMode.None);
        Debug.Log($"EconomyUIController count: {uiControllers.Length}");

        Debug.Log("================================");
    }

    [ContextMenu("Force Create PlayerEconomy")]
    void Context_ForceCreate()
    {
        SceneBootstrapper.EnsureEconomyExists();
        Invoke(nameof(RefreshAllUI), 0.1f);
    }
}