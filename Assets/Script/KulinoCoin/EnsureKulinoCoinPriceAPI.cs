using UnityEngine;

/// <summary>
/// ✅ Auto-create KulinoCoinPriceAPI if not exists
/// Attach this to any GameObject in first scene (e.g., GameManager)
/// </summary>
[DefaultExecutionOrder(-200)]
public class EnsureKulinoCoinPriceAPI : MonoBehaviour
{
    void Awake()
    {
        if (KulinoCoinPriceAPI.Instance == null)
        {
            Debug.Log("[EnsureKC] Creating KulinoCoinPriceAPI...");
            
            var go = new GameObject("KulinoCoinPriceAPI");
            var api = go.AddComponent<KulinoCoinPriceAPI>();
            
            // ✅ Set default values
            api.kulinoCoinPriceIDR = 1500.0; // Default: Rp 1,500 per KC
            api.updateInterval = 300f; // Update every 5 minutes
            api.enableDebugLogs = true;
            
            DontDestroyOnLoad(go);
            
            Debug.Log("[EnsureKC] ✅ KulinoCoinPriceAPI created");
        }
        else
        {
            Debug.Log("[EnsureKC] ✓ KulinoCoinPriceAPI already exists");
        }
    }
}