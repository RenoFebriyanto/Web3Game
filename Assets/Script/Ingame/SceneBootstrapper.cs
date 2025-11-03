using UnityEngine;

/// <summary>
/// FIXED: Prevents duplicate EconomyManager instantiation on scene reload
/// Only instantiates if PlayerEconomy.Instance is truly null
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SceneBootstrapper : MonoBehaviour
{
    const string ECONOMY_PREFAB_PATH = "EconomyManager";

    // ✅ STATIC flag untuk prevent duplicate instantiation
    private static bool hasInitialized = false;

    void Awake()
    {
        // ✅ CRITICAL FIX: Check if PlayerEconomy already exists FIRST
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] PlayerEconomy already exists, skipping instantiation");
            hasInitialized = true;
            return;
        }

        // ✅ Check static flag (secondary check)
        if (hasInitialized)
        {
            Debug.Log("[SceneBootstrapper] Already initialized (static flag), skipping");
            return;
        }

        // Load dan instantiate EconomyManager
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);
        if (prefab != null)
        {
            Debug.Log("[SceneBootstrapper] Instantiating EconomyManager prefab from Resources");
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager"; // Remove "(Clone)" suffix
            hasInitialized = true;
        }
        else
        {
            Debug.LogError($"[SceneBootstrapper] Could not find prefab at Resources/{ECONOMY_PREFAB_PATH}");
        }
    }

    // ✅ DO NOT reset flag on scene load (keep persistent)
    // Flag hanya di-reset saat aplikasi quit (untuk testing di Editor)

    void OnApplicationQuit()
    {
        hasInitialized = false;
    }
}