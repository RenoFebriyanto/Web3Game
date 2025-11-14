using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ✅ FIXED: ObstacleConfig dengan validation & force serialization
/// </summary>
[CreateAssetMenu(fileName = "ObstacleConfig", menuName = "Kulino/Spawn Patterns/Obstacle Config")]
public class ObstacleConfig : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Unique ID untuk matching dengan coin pattern")]
    public string obstacleId = "obs_1";
    
    [Tooltip("Display name")]
    public string displayName = "Obstacle 1";
    
    [Header("Prefab")]
    [Tooltip("Obstacle prefab (with pre-positioned child planets)")]
    public GameObject prefab;
    
    [Header("Lane Blocking Info")]
    [Tooltip("Which lanes are BLOCKED by this obstacle (0=left, 1=center, 2=right)")]
    public List<int> blockedLanes = new List<int>();
    
    [Tooltip("Which lanes are FREE for coin spawning")]
    public List<int> freeLanes = new List<int>();
    
    [Header("🚂 DYNAMIC MOVEMENT (Subway Surf Style)")]
    [SerializeField] // ✅ CRITICAL: Ensure serialization
    [Tooltip("Apakah obstacle ini bergerak kearah player?")]
    private bool _isDynamicObstacle = false;
    
    // ✅ NEW: Public property with validation
    public bool isDynamicObstacle
    {
        get { return _isDynamicObstacle; }
        set 
        { 
            _isDynamicObstacle = value;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
    
    [Tooltip("Lane yang akan digunakan untuk movement (0=left, 1=center, 2=right)")]
    public int movementLane = 1;
    
    [Tooltip("Speed multiplier untuk dynamic obstacle (relatif terhadap base speed)")]
    [Range(0.5f, 3f)]
    public float dynamicSpeedMultiplier = 1.5f;
    
    [Tooltip("Delay sebelum mulai bergerak (detik)")]
    public float movementStartDelay = 0.5f;
    
    [Header("Spawn Settings")]
    [Tooltip("Selection weight (higher = more common)")]
    [Range(1, 100)]
    public int selectionWeight = 50;
    
    [Tooltip("Vertical height of obstacle (untuk spacing calculation)")]
    public float obstacleHeight = 5f;
    
    [Tooltip("Extra spacing setelah obstacle ini (2.5 = safe, 4 = dynamic)")]
    public float extraSpacing = 2.5f;
    
    [Header("Preview")]
    public Texture2D previewImage;
    
    [ContextMenu("Auto Calculate Free Lanes")]
    public void AutoCalculateFreeLanes()
    {
        int totalLanes = 3;
        
        if (LanesManager.Instance != null)
        {
            totalLanes = LanesManager.Instance.laneCount;
        }
        
        freeLanes.Clear();
        
        for (int i = 0; i < totalLanes; i++)
        {
            if (!blockedLanes.Contains(i))
            {
                freeLanes.Add(i);
            }
        }
        
        Debug.Log($"[{displayName}] Auto-calculated free lanes: [{string.Join(", ", freeLanes)}]");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
    
    public bool IsLaneBlocked(int lane)
    {
        return blockedLanes.Contains(lane);
    }
    
    public bool IsLaneFree(int lane)
    {
        return freeLanes.Contains(lane);
    }
    
    public float GetTotalSpacing()
    {
        return obstacleHeight + extraSpacing;
    }
    
    /// <summary>
    /// ✅ FIXED: Validate dengan proper dynamic check
    /// </summary>
    public bool IsValid()
    {
        if (prefab == null)
        {
            Debug.LogError($"[{displayName}] Prefab is null!");
            return false;
        }
        
        if (string.IsNullOrEmpty(obstacleId))
        {
            Debug.LogError($"[{displayName}] Obstacle ID is empty!");
            return false;
        }
        
        if (blockedLanes.Count == 0 && freeLanes.Count == 0)
        {
            Debug.LogWarning($"[{displayName}] No blocked/free lanes defined!");
        }
        
        // ✅ Log dynamic status
        if (_isDynamicObstacle)
        {
            if (movementLane < 0 || movementLane > 2)
            {
                Debug.LogError($"[{displayName}] Invalid movement lane: {movementLane}!");
                return false;
            }
            
            Debug.Log($"[{displayName}] ⚡ DYNAMIC OBSTACLE - Lane {movementLane}");
        }
        else
        {
            Debug.Log($"[{displayName}] 🔵 STATIC OBSTACLE - No movement");
        }
        
        return true;
    }
    
    public string GetInfoSummary()
    {
        string info = $"<b>{displayName}</b> (ID: {obstacleId})\n";
        info += $"Blocked: [{string.Join(", ", blockedLanes)}]\n";
        info += $"Free: [{string.Join(", ", freeLanes)}]\n";
        info += $"Height: {obstacleHeight}m + Extra: {extraSpacing}m\n";
        
        if (_isDynamicObstacle)
        {
            info += $"<color=yellow>⚡ DYNAMIC</color> - Lane {movementLane} - Speed x{dynamicSpeedMultiplier}\n";
        }
        else
        {
            info += $"<color=cyan>🔵 STATIC</color> - No movement\n";
        }
        
        return info;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// ✅ NEW: Force validate saat save
    /// </summary>
    void OnValidate()
    {
        // Auto-mark dirty saat ada perubahan
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    [ContextMenu("Force Save Asset")]
    void ForceSave()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[{displayName}] ✓ Asset saved! isDynamic={_isDynamicObstacle}");
    }
    
    [ContextMenu("Debug: Print Config")]
    void DebugPrintConfig()
    {
        Debug.Log("========================================");
        Debug.Log($"Obstacle: {displayName}");
        Debug.Log($"ID: {obstacleId}");
        Debug.Log($"isDynamicObstacle: {_isDynamicObstacle}");
        Debug.Log($"Movement Lane: {movementLane}");
        Debug.Log($"Prefab: {(prefab != null ? prefab.name : "NULL")}");
        Debug.Log("========================================");
    }
    #endif
    
    #if UNITY_EDITOR
[ContextMenu("Debug: Check Prefab Components")]
void DebugCheckPrefabComponents()
{
    if (prefab == null)
    {
        Debug.LogError($"[{displayName}] Prefab is NULL!");
        return;
    }
    
    Debug.Log("========================================");
    Debug.Log($"Checking prefab: {prefab.name}");
    
    // Check for DynamicObstacleMover
    var dynamicMover = prefab.GetComponent<DynamicObstacleMover>();
    if (dynamicMover != null)
    {
        Debug.LogWarning($"⚠️ PREFAB HAS DynamicObstacleMover component!");
        Debug.LogWarning($"  → This will override config settings!");
        Debug.LogWarning($"  → REMOVE this component from prefab!");
    }
    else
    {
        Debug.Log("✓ Prefab is clean (no DynamicObstacleMover)");
    }
    
    // Check for movers
    var planetMover = prefab.GetComponent<PlanetMover>();
    var coinMover = prefab.GetComponent<CoinMover>();
    var fragmentMover = prefab.GetComponent<FragmentMover>();
    
    Debug.Log($"PlanetMover: {(planetMover != null ? "YES" : "NO")}");
    Debug.Log($"CoinMover: {(coinMover != null ? "YES" : "NO")}");
    Debug.Log($"FragmentMover: {(fragmentMover != null ? "YES" : "NO")}");
    
    Debug.Log("========================================");
}

[ContextMenu("Auto-Fix: Remove DynamicMover from Prefab")]
void AutoFixRemoveDynamicMover()
{
    if (prefab == null)
    {
        Debug.LogError($"[{displayName}] Prefab is NULL!");
        return;
    }
    
    var dynamicMover = prefab.GetComponent<DynamicObstacleMover>();
    
    if (dynamicMover != null)
    {
        Debug.Log($"Removing DynamicObstacleMover from prefab: {prefab.name}");
        DestroyImmediate(dynamicMover);
        
        UnityEditor.EditorUtility.SetDirty(prefab);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log("✓ Component removed and prefab saved!");
    }
    else
    {
        Debug.Log("✓ Prefab already clean!");
    }
}
#endif
}