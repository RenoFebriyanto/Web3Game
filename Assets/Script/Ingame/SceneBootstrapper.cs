using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SceneBootstrapper : MonoBehaviour
{
    const string ECONOMY_PREFAB_PATH = "EconomyManager";

    // ✅ STATIC flag untuk prevent duplicate instantiation
    private static bool hasInitialized = false;

    void Awake()
    {
        // ✅ Cek apakah sudah pernah di-initialize
        if (hasInitialized && PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] EconomyManager already initialized, skipping");
            return;
        }

        // Jika PlayerEconomy sudah ada (dari scene lain), skip
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] PlayerEconomy already exists");
            hasInitialized = true;
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

    // ✅ Reset flag saat aplikasi quit (untuk testing di Editor)
    void OnApplicationQuit()
    {
        hasInitialized = false;
    }
}