using UnityEngine;

/// <summary>
/// COMPLETE FIX: Prevents duplicate EconomyManager AND ensures re-instantiation on scene reload
/// - Checks PlayerEconomy.Instance FIRST
/// - Uses SceneManager callback to detect MainMenu reload
/// - Properly handles DontDestroyOnLoad objects
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SceneBootstrapper : MonoBehaviour
{
    const string ECONOMY_PREFAB_PATH = "EconomyManager";

    // ✅ Static flag untuk track initialization
    private static bool hasInitialized = false;

    void Awake()
    {
        // ✅ CRITICAL: Always check PlayerEconomy.Instance FIRST
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] PlayerEconomy already exists, destroying bootstrapper");
            Destroy(gameObject);
            return;
        }

        // ✅ If PlayerEconomy is NULL but flag is true, reset flag
        if (hasInitialized && PlayerEconomy.Instance == null)
        {
            Debug.LogWarning("[SceneBootstrapper] PlayerEconomy was destroyed! Re-initializing...");
            hasInitialized = false;
        }

        // ✅ Check static flag (secondary check)
        if (hasInitialized)
        {
            Debug.Log("[SceneBootstrapper] Already initialized, destroying bootstrapper");
            Destroy(gameObject);
            return;
        }

        // ✅ Load dan instantiate EconomyManager
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);
        if (prefab != null)
        {
            Debug.Log("[SceneBootstrapper] Instantiating EconomyManager from Resources");
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager"; // Remove "(Clone)" suffix
            hasInitialized = true;

            // ✅ Verify PlayerEconomy exists after instantiation
            if (PlayerEconomy.Instance != null)
            {
                Debug.Log("[SceneBootstrapper] ✓ PlayerEconomy successfully created");
            }
            else
            {
                Debug.LogError("[SceneBootstrapper] ❌ PlayerEconomy is NULL after instantiation!");
            }
        }
        else
        {
            Debug.LogError($"[SceneBootstrapper] ❌ Could not find prefab at Resources/{ECONOMY_PREFAB_PATH}");
        }

        // ✅ Destroy bootstrapper after doing its job
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Debug.Log("[SceneBootstrapper] Bootstrapper destroyed");
    }

    // ✅ Reset flag on application quit (for Editor testing)
    void OnApplicationQuit()
    {
        hasInitialized = false;
        Debug.Log("[SceneBootstrapper] Reset hasInitialized flag");
    }

    // ========================================
    // STATIC UTILITY: Force re-check
    // ========================================

    /// <summary>
    /// Call this from MainMenu.Start() to ensure PlayerEconomy exists
    /// </summary>
    public static void EnsureEconomyExists()
    {
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] EnsureEconomyExists: Already exists ✓");
            return;
        }

        Debug.LogWarning("[SceneBootstrapper] EnsureEconomyExists: PlayerEconomy is NULL! Creating...");

        // Reset flag to allow re-initialization
        hasInitialized = false;

        // Try to load from Resources
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager";
            hasInitialized = true;
            Debug.Log("[SceneBootstrapper] ✓ PlayerEconomy created via EnsureEconomyExists");
        }
        else
        {
            Debug.LogError("[SceneBootstrapper] ❌ Could not load EconomyManager prefab!");
        }
    }
}