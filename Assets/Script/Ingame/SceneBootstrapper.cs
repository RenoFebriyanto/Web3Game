using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// FIXED: Proper scene change detection + auto-reset flag
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SceneBootstrapper : MonoBehaviour
{
    const string ECONOMY_PREFAB_PATH = "EconomyManager";

    // ✅ FIXED: Track last scene to detect changes
    private static string lastSceneName = "";
    private static bool hasInitialized = false;

    void Awake()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // ✅ JANGAN destroy GameBootstrap!
        if (gameObject.name.Contains("GameBootstrap"))
        {
            Debug.Log("[SceneBootstrapper] Skipping - this is GameBootstrap");
            return;
        }

        // ✅ RESET flag jika scene berubah (kembali ke MainMenu)
        if (lastSceneName != currentScene)
        {
            Debug.Log($"[SceneBootstrapper] Scene changed: {lastSceneName} → {currentScene}. Resetting flag.");
            hasInitialized = false;
            lastSceneName = currentScene;
        }

        // ✅ Check PlayerEconomy.Instance FIRST
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] PlayerEconomy already exists, skipping bootstrap");
            Destroy(gameObject);
            return;
        }

        // ✅ Check static flag (secondary)
        if (hasInitialized)
        {
            Debug.Log("[SceneBootstrapper] Already initialized this session, skipping");
            Destroy(gameObject);
            return;
        }

        // ✅ Load and instantiate EconomyManager
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);
        if (prefab != null)
        {
            Debug.Log("[SceneBootstrapper] Creating EconomyManager from Resources");
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager"; // Remove "(Clone)"
            hasInitialized = true;

            // Verify
            if (PlayerEconomy.Instance != null)
            {
                Debug.Log("[SceneBootstrapper] ✓ PlayerEconomy created successfully");
            }
            else
            {
                Debug.LogError("[SceneBootstrapper] ❌ PlayerEconomy NULL after instantiation!");
            }
        }
        else
        {
            Debug.LogError($"[SceneBootstrapper] ❌ Prefab not found at Resources/{ECONOMY_PREFAB_PATH}");
        }

        // Destroy bootstrapper
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Debug.Log("[SceneBootstrapper] Destroyed");
    }

    // ========================================
    // PUBLIC STATIC: Force re-check
    // ========================================

    public static void EnsureEconomyExists()
    {
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] EnsureEconomyExists: Already exists ✓");
            return;
        }

        Debug.LogWarning("[SceneBootstrapper] EnsureEconomyExists: Creating...");

        // Reset flag
        hasInitialized = false;

        // Load from Resources
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager";
            hasInitialized = true;
            Debug.Log("[SceneBootstrapper] ✓ Created via EnsureEconomyExists");
        }
        else
        {
            Debug.LogError("[SceneBootstrapper] ❌ Cannot load prefab!");
        }
    }

    // ✅ NEW: Manual reset (for testing)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        hasInitialized = false;
        lastSceneName = "";
        Debug.Log("[SceneBootstrapper] Static data reset (Domain Reload)");
    }
}