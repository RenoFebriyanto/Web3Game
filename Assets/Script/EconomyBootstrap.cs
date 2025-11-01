using UnityEngine;

/// <summary>
/// Ensures PlayerEconomy exists in any scene.
/// Attach to a persistent GameObject in MainMenu or as DontDestroyOnLoad.
/// </summary>
[DefaultExecutionOrder(-999)]
public class EconomyBootstrap : MonoBehaviour
{
    private static EconomyBootstrap instance;

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
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
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[EconomyBootstrap] PlayerEconomy already exists");
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
}