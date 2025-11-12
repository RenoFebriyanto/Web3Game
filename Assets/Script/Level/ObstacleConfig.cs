using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ✅ SIMPLIFIED Obstacle Config - untuk prefab yang sudah memiliki child positioning
/// Prefab spawn di X=0, child objects handle lane placement
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
    
    [Header("Spawn Settings")]
    [Tooltip("Selection weight (higher = more common)")]
    [Range(1, 100)]
    public int selectionWeight = 50;
    
    [Tooltip("Vertical height of obstacle (untuk spacing calculation)")]
    public float obstacleHeight = 5f;
    
    [Header("Preview")]
    public Texture2D previewImage;
    
    /// <summary>
    /// Auto-calculate free lanes from blocked lanes
    /// </summary>
    public void AutoCalculateFreeLanes(int totalLanes)
    {
        freeLanes.Clear();
        
        for (int i = 0; i < totalLanes; i++)
        {
            if (!blockedLanes.Contains(i))
            {
                freeLanes.Add(i);
            }
        }
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
        
        return true;
    }
}