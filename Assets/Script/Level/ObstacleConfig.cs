using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ✅ UPDATED: ObstacleConfig dengan support untuk Dynamic Movement
/// FEATURES:
/// - Static obstacles (normal spawn)
/// - Dynamic obstacles (moving toward player - subway surf style)
/// - Auto-calculate free lanes
/// - Validation system
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
    [Tooltip("Apakah obstacle ini bergerak kearah player?")]
    public bool isDynamicObstacle = false;
    
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
    
    [Tooltip("Extra spacing setelah obstacle ini (untuk dynamic obstacles yang butuh ruang lebih)")]
    public float extraSpacing = 0f;
    
    [Header("Preview")]
    public Texture2D previewImage;
    
    /// <summary>
    /// Auto-calculate free lanes from blocked lanes
    /// </summary>
    [ContextMenu("Auto Calculate Free Lanes")]
    public void AutoCalculateFreeLanes()
    {
        int totalLanes = 3; // Default lane count
        
        // Try get from LanesManager
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
    }
    
    /// <summary>
    /// Check if lane is blocked
    /// </summary>
    public bool IsLaneBlocked(int lane)
    {
        return blockedLanes.Contains(lane);
    }
    
    /// <summary>
    /// Check if lane is free
    /// </summary>
    public bool IsLaneFree(int lane)
    {
        return freeLanes.Contains(lane);
    }
    
    /// <summary>
    /// Get total spacing (height + extra spacing)
    /// </summary>
    public float GetTotalSpacing()
    {
        return obstacleHeight + extraSpacing;
    }
    
    /// <summary>
    /// Validate config
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
        
        // Dynamic obstacle validation
        if (isDynamicObstacle)
        {
            if (movementLane < 0 || movementLane > 2)
            {
                Debug.LogError($"[{displayName}] Invalid movement lane: {movementLane}!");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Get info summary untuk debugging
    /// </summary>
    public string GetInfoSummary()
    {
        string info = $"<b>{displayName}</b> (ID: {obstacleId})\n";
        info += $"Blocked: [{string.Join(", ", blockedLanes)}]\n";
        info += $"Free: [{string.Join(", ", freeLanes)}]\n";
        info += $"Height: {obstacleHeight}m\n";
        
        if (isDynamicObstacle)
        {
            info += $"<color=yellow>⚡ DYNAMIC</color> - Lane {movementLane} - Speed x{dynamicSpeedMultiplier}\n";
        }
        
        return info;
    }
}