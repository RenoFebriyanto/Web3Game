using UnityEngine;

/// <summary>
/// UPDATED: Ensures PlayerEconomy exists in any scene without duplication
/// Works together with SceneBootstrapper for redundancy
/// </summary>
[DefaultExecutionOrder(-999)]
public class EconomyBootstrap : MonoBehaviour
{
    private static EconomyBootstrap instance;

    void Awake()
    {
        // ✅ CRITICAL: Check PlayerEconomy FIRST
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[EconomyBootstrap] PlayerEconomy already exists, destroying bootstrap");
            Destroy(gameObject);
            return;
        }

        // Singleton pattern for bootstrap itself
        if (instance != null && instance != this)
        {
            Debug.Log("[EconomyBootstrap] Duplicate bootstrap detected, destroying");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // ✅ DO NOT use DontDestroyOnLoad for bootstrap
        // Let it die after creating PlayerEconomy

        // Ensure PlayerEconomy exists
        EnsurePlayerEconomy();

        // ✅ Destroy bootstrap after creation
        Destroy(gameObject);
    }

    void EnsurePlayerEconomy()
    {
        // ✅ Double-check before creating
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[EconomyBootstrap] PlayerEconomy already exists (double-check)");
            return;
        }

        // Try to load from Resources
        var prefab = Resources.Load<GameObject>("EconomyManager");
        if (prefab != null)
        {
            var obj = Instantiate(prefab);
            obj.name = "EconomyManager";
            Debug.Log("[EconomyBootstrap] ✓ Created EconomyManager from Resources");
        }
        else
        {
            // Fallback: create empty GameObject with PlayerEconomy
            var go = new GameObject("PlayerEconomy");
            go.AddComponent<PlayerEconomy>();
            DontDestroyOnLoad(go);
            Debug.Log("[EconomyBootstrap] ✓ Created PlayerEconomy fallback");
        }
    }

    void OnDestroy()
    {
        Debug.Log("[EconomyBootstrap] Bootstrap destroyed");
    }
}