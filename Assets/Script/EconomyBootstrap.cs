using UnityEngine;

/// <summary>
/// FIXED: Ensures PlayerEconomy exists in any scene without duplication
/// </summary>
[DefaultExecutionOrder(-999)]
public class EconomyBootstrap : MonoBehaviour
{
    private static EconomyBootstrap instance;

    void Awake()
    {
        // ✅ CRITICAL: Check PlayerEconomy FIRST before doing anything
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
        DontDestroyOnLoad(gameObject);

        // Ensure PlayerEconomy exists
        EnsurePlayerEconomy();
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
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager";
            Debug.Log("[EconomyBootstrap] Created EconomyManager from Resources");
        }
        else
        {
            // Fallback: create empty GameObject with PlayerEconomy
            var go = new GameObject("PlayerEconomy");
            go.AddComponent<PlayerEconomy>();
            DontDestroyOnLoad(go);
            Debug.Log("[EconomyBootstrap] Created PlayerEconomy fallback");
        }
    }

    // ✅ NEW: Destroy bootstrap after ensuring economy exists
    void Start()
    {
        // Setelah PlayerEconomy created, bootstrap tidak diperlukan lagi
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[EconomyBootstrap] PlayerEconomy verified, destroying bootstrap");
            Destroy(gameObject);
        }
    }
}