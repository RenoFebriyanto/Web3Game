using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ FINAL FIX: SceneBootstrapper - Lightweight & Safe
/// HANYA check PlayerEconomy existence, TIDAK create baru
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SceneBootstrapper : MonoBehaviour
{
    private static bool hasChecked = false;
    private static string lastSceneName = "";

    void Awake()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Reset flag on scene change
        if (lastSceneName != currentScene)
        {
            Debug.Log($"[SceneBootstrapper] Scene changed: {lastSceneName} → {currentScene}");
            hasChecked = false;
            lastSceneName = currentScene;
        }

        // ✅ CRITICAL: HANYA check, JANGAN create!
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[SceneBootstrapper] ✓ PlayerEconomy exists, all good");
            Destroy(gameObject); // Self-destruct
            return;
        }

        // ✅ If PlayerEconomy missing, just warn (jangan create)
        if (!hasChecked)
        {
            Debug.LogWarning("[SceneBootstrapper] ⚠️ PlayerEconomy not found!");
            Debug.LogWarning("[SceneBootstrapper] Make sure 'EconomyManager' prefab exists in MainMenu scene");
            hasChecked = true;
        }

        // Self-destruct after check
        Destroy(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        hasChecked = false;
        lastSceneName = "";
    }
}