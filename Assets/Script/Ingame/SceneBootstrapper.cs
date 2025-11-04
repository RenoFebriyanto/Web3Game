using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// FIXED: Proper bootstrapper that ensures EconomyManager spawns correctly
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SceneBootstrapper : MonoBehaviour
{
    const string ECONOMY_PREFAB_PATH = "EconomyManager";

    private static bool hasInitialized = false;
    private static string lastSceneName = "";

    void Awake()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Reset flag on scene change
        if (lastSceneName != currentScene)
        {
            Debug.Log($"[SceneBootstrapper] Scene changed: {lastSceneName} → {currentScene}");
            hasInitialized = false;
            lastSceneName = currentScene;
        }

        // ✅ FIX 1: Check PlayerEconomy.Instance FIRST (most important!)
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] PlayerEconomy already exists ✓");

            // ✅ FIX 2: DON'T destroy bootstrapper yet - let it finish
            Invoke(nameof(DestroyBootstrapper), 0.1f);
            return;
        }

        // ✅ FIX 3: Check if already initialized THIS scene
        if (hasInitialized)
        {
            Debug.Log("[SceneBootstrapper] Already initialized this session");
            Invoke(nameof(DestroyBootstrapper), 0.1f);
            return;
        }

        // ✅ FIX 4: Spawn EconomyManager
        Debug.Log("[SceneBootstrapper] Creating EconomyManager...");

        if (!CreateEconomyManager())
        {
            Debug.LogError("[SceneBootstrapper] ❌ FAILED to create EconomyManager!");
            return;
        }

        hasInitialized = true;

        // ✅ FIX 5: Destroy bootstrapper AFTER successful creation
        Invoke(nameof(DestroyBootstrapper), 0.2f);
    }

    bool CreateEconomyManager()
    {
        // ✅ Method 1: Load from Resources
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);

        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager"; // Remove "(Clone)"

            Debug.Log("[SceneBootstrapper] ✓ Created EconomyManager from Resources");

            // ✅ Verify PlayerEconomy component
            var playerEconomy = instance.GetComponent<PlayerEconomy>();
            if (playerEconomy == null)
            {
                Debug.LogError("[SceneBootstrapper] ❌ Prefab missing PlayerEconomy component!");
                Destroy(instance);
                return false;
            }

            // ✅ Wait for Awake to complete
            return true;
        }

        // ✅ Method 2: Fallback - Create from scratch
        Debug.LogWarning("[SceneBootstrapper] Prefab not found at Resources/EconomyManager!");
        Debug.LogWarning("[SceneBootstrapper] Creating fallback EconomyManager...");

        var fallbackGO = new GameObject("EconomyManager");
        var economy = fallbackGO.AddComponent<PlayerEconomy>();

        if (economy != null)
        {
            Debug.Log("[SceneBootstrapper] ✓ Created fallback EconomyManager");
            return true;
        }

        Debug.LogError("[SceneBootstrapper] ❌ Failed to create fallback!");
        return false;
    }

    void DestroyBootstrapper()
    {
        // ✅ Final verification before destroy
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] ✓ EconomyManager verified - Destroying bootstrapper");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("[SceneBootstrapper] ❌ PlayerEconomy.Instance still NULL after spawn!");

            // ✅ Last resort: Try one more time
            Debug.LogWarning("[SceneBootstrapper] Attempting emergency respawn...");
            CreateEconomyManager();

            // Destroy anyway to prevent loop
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Debug.Log("[SceneBootstrapper] Bootstrapper destroyed");
    }

    // ========================================
    // PUBLIC STATIC: Force check
    // ========================================

    public static void EnsureEconomyExists()
    {
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] EnsureEconomyExists: Already exists ✓");
            return;
        }

        Debug.LogWarning("[SceneBootstrapper] EnsureEconomyExists: Creating now...");

        // Reset flag
        hasInitialized = false;

        // Load from Resources
        var prefab = Resources.Load<GameObject>("EconomyManager");
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager";
            hasInitialized = true;
            Debug.Log("[SceneBootstrapper] ✓ Created via EnsureEconomyExists");
        }
        else
        {
            // Fallback
            var go = new GameObject("EconomyManager");
            go.AddComponent<PlayerEconomy>();
            hasInitialized = true;
            Debug.Log("[SceneBootstrapper] ✓ Created fallback via EnsureEconomyExists");
        }
    }

    // ========================================
    // RESET: For domain reload
    // ========================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        hasInitialized = false;
        lastSceneName = "";
        Debug.Log("[SceneBootstrapper] Static data reset (Domain Reload)");
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Debug: Force Create EconomyManager")]
    void Context_ForceCreate()
    {
        if (CreateEconomyManager())
        {
            Debug.Log("✓ Force create successful!");
        }
        else
        {
            Debug.LogError("✗ Force create failed!");
        }
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== SCENE BOOTSTRAPPER STATUS ===");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Last Scene: {lastSceneName}");
        Debug.Log($"Has Initialized: {hasInitialized}");
        Debug.Log($"PlayerEconomy.Instance: {(PlayerEconomy.Instance != null ? "EXISTS ✓" : "NULL ✗")}");

        if (PlayerEconomy.Instance != null)
        {
            Debug.Log($"  - GameObject: {PlayerEconomy.Instance.gameObject.name}");
            Debug.Log($"  - Coins: {PlayerEconomy.Instance.Coins}");
            Debug.Log($"  - Shards: {PlayerEconomy.Instance.Shards}");
        }

        Debug.Log("================================");
    }

    [ContextMenu("Debug: Reset Initialization Flag")]
    void Context_ResetFlag()
    {
        hasInitialized = false;
        Debug.Log("✓ Initialization flag reset");
    }
}