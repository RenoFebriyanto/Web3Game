using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Obstacle Spawn Pattern dengan ID untuk matching dengan Coin Pattern
/// Pattern ini defines tata letak obstacle (planet) di lanes
/// </summary>
[CreateAssetMenu(fileName = "ObstaclePattern", menuName = "Kulino/Spawn Patterns/Obstacle Pattern")]
public class ObstacleSpawnPattern : ScriptableObject
{
    [Header("Pattern Identity")]
    [Tooltip("Unique ID untuk pattern ini (contoh: obs_left_right, obs_center, dll)")]
    public string patternId = "obs_pattern_1";
    
    [Tooltip("Display name untuk Inspector")]
    public string displayName = "Obstacle Pattern";
    
    [Header("Pattern Layout")]
    [Tooltip("Spawn points (x=lane offset dari center, y=vertical spacing multiplier)")]
    public List<Vector2> spawnPoints = new List<Vector2>();
    
    [Header("Spawn Settings")]
    [Tooltip("Selection weight (higher = more common)")]
    [Range(1, 100)]
    public int selectionWeight = 50;
    
    [Tooltip("Min/Max delay setelah pattern ini spawn (detik)")]
    public Vector2 delayRange = new Vector2(0.5f, 1.5f);
    
    [Header("Lane Blocking Info")]
    [Tooltip("Which lanes are blocked by this pattern (untuk coin spawn reference)")]
    public List<int> blockedLanes = new List<int>();
    
    [Header("Preview")]
    [Tooltip("Visual preview untuk editor (optional)")]
    public Texture2D previewTexture;
    
    /// <summary>
    /// Get random delay untuk pattern ini
    /// </summary>
    public float GetRandomDelay()
    {
        return Random.Range(delayRange.x, delayRange.y);
    }
    
    /// <summary>
    /// Get total vertical distance pattern ini (untuk spacing calculation)
    /// </summary>
    public float GetTotalVerticalDistance()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return 0f;
        return spawnPoints.Sum(p => p.y);
    }
    
    /// <summary>
    /// Check if pattern valid untuk spawn dari baseLane
    /// </summary>
    public bool IsValidForBaseLane(int baseLane, int totalLanes)
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return false;
        
        foreach (var point in spawnPoints)
        {
            int targetLane = baseLane + Mathf.RoundToInt(point.x);
            if (targetLane < 0 || targetLane >= totalLanes)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get all lanes yang digunakan pattern ini dari baseLane
    /// </summary>
    public HashSet<int> GetUsedLanes(int baseLane)
    {
        HashSet<int> lanes = new HashSet<int>();
        
        if (spawnPoints != null)
        {
            foreach (var point in spawnPoints)
            {
                int lane = baseLane + Mathf.RoundToInt(point.x);
                lanes.Add(lane);
            }
        }
        
        return lanes;
    }
    
    /// <summary>
    /// Get blocked lanes untuk pattern ini (lanes yang tidak boleh ada coin)
    /// </summary>
    public HashSet<int> GetBlockedLanes(int baseLane, int totalLanes)
    {
        HashSet<int> blocked = new HashSet<int>();
        
        // Method 1: Manual blocked lanes (relative to baseLane)
        if (blockedLanes != null && blockedLanes.Count > 0)
        {
            foreach (int offset in blockedLanes)
            {
                int lane = baseLane + offset;
                if (lane >= 0 && lane < totalLanes)
                    blocked.Add(lane);
            }
        }
        else
        {
            // Method 2: Auto-detect from spawn points
            blocked = GetUsedLanes(baseLane);
        }
        
        return blocked;
    }
    
    /// <summary>
    /// Get free lanes (lanes yang boleh spawn coin)
    /// </summary>
    public List<int> GetFreeLanes(int baseLane, int totalLanes)
    {
        HashSet<int> blockedSet = GetBlockedLanes(baseLane, totalLanes);
        List<int> free = new List<int>();
        
        for (int i = 0; i < totalLanes; i++)
        {
            if (!blockedSet.Contains(i))
                free.Add(i);
        }
        
        return free;
    }
}