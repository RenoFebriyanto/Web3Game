using UnityEngine;

/// <summary>
/// ✅✅✅ BOOTSTRAP untuk DailyRewardSystem
/// - Create DailyRewardSystem HANYA JIKA belum ada
/// - Self-destruct setelah create
/// - Taruh di MainMenu scene saja
/// </summary>
[DefaultExecutionOrder(-900)]
public class DailyRewardBootstrap : MonoBehaviour
{
    private static bool hasCreated = false;

    void Awake()
    {
        Debug.Log("[DailyRewardBootstrap] Awake");

        // ✅ Check if DailyRewardSystem already exists
        if (DailyRewardSystem.Instance != null)
        {
            Debug.Log("[DailyRewardBootstrap] DailyRewardSystem already exists - destroying bootstrap");
            Destroy(gameObject);
            return;
        }

        // ✅ Check if we already created one (prevent double creation)
        if (hasCreated)
        {
            Debug.Log("[DailyRewardBootstrap] Already created DailyRewardSystem in this session - destroying bootstrap");
            Destroy(gameObject);
            return;
        }

        // ✅ Create DailyRewardSystem
        CreateDailyRewardSystem();

        // ✅ Mark as created
        hasCreated = true;

        // ✅ Self-destruct
        Destroy(gameObject);
    }

    void CreateDailyRewardSystem()
    {
        Debug.Log("[DailyRewardBootstrap] Creating DailyRewardSystem...");

        // Create new GameObject
        GameObject go = new GameObject("[DailyRewardSystem - PERSISTENT]");

        // Add component
        go.AddComponent<DailyRewardSystem>();

        // Don't destroy on load (already handled in DailyRewardSystem.Awake)
        // DontDestroyOnLoad(go); // Tidak perlu, sudah di script

        Debug.Log("[DailyRewardBootstrap] ✓ DailyRewardSystem created successfully");
    }

    void OnDestroy()
    {
        Debug.Log("[DailyRewardBootstrap] Bootstrap destroyed");
    }

    /// <summary>
    /// Reset static flag (untuk testing)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        hasCreated = false;
        Debug.Log("[DailyRewardBootstrap] Static reset");
    }
}