using UnityEngine;

[DefaultExecutionOrder(-1000)] // jalankan sangat awal
public class SceneBootstrapper : MonoBehaviour
{
    // Nama prefab di Resources (tanpa ekstensi)
    const string ECONOMY_PREFAB_PATH = "EconomyManager";

    void Awake()
    {
        // Jika PlayerEconomy sudah ada (mis. dari build sebelumnya), tidak perlu instantiate
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] PlayerEconomy already exists.");
            return;
        }

        // Cari prefab di Resources dan instantiate jika ada
        var prefab = Resources.Load<GameObject>(ECONOMY_PREFAB_PATH);
        if (prefab != null)
        {
            Debug.Log("[SceneBootstrapper] Instantiating EconomyManager prefab from Resources.");
            Instantiate(prefab);
            // PlayerEconomy.Awake() pada prefab akan dipanggil segera setelah Instantiate
        }
        else
        {
            Debug.LogError($"[SceneBootstrapper] Could not find prefab at Resources/{ECONOMY_PREFAB_PATH}. Please create EconomyManager prefab in Assets/Resources/");
        }
    }
}
