using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ✅ SIMPLIFIED Coin Pattern - spawn berdasarkan lane index
/// </summary>
[CreateAssetMenu(fileName = "CoinPattern", menuName = "Kulino/Spawn Patterns/Coin Pattern")]
public class CoinPattern : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Pattern name")]
    public string patternName = "Coin Pattern";
    
    [Tooltip("Compatible obstacle ID (leave empty for any)")]
    public string compatibleObstacleId = "";
    
    [Header("Pattern Layout")]
    [Tooltip("Which lanes to spawn coins (0=left, 1=center, 2=right)")]
    public List<int> spawnLanes = new List<int>();
    
    [Tooltip("Number of coins per lane (vertical)")]
    public int coinsPerLane = 5;
    
    [Tooltip("Vertical spacing between coins")]
    public float verticalSpacing = 1f;
    
    [Header("Spawn Settings")]
    [Tooltip("Selection weight")]
    [Range(1, 100)]
    public int selectionWeight = 50;
    
    [Tooltip("Only spawn in FREE lanes?")]
    public bool onlySpawnInFreeLanes = true;
    
    [Header("Fragment Substitution")]
    [Tooltip("Chance to spawn fragment instead of coin (0-100%)")]
    [Range(0f, 100f)]
    public float fragmentChance = 15f;
    
    [Header("Preview")]
    public Texture2D previewImage;
    
    /// <summary>
    /// Check if compatible dengan obstacle
    /// </summary>
    public bool IsCompatibleWith(string obstacleId)
    {
        if (string.IsNullOrEmpty(compatibleObstacleId))
            return true; // Match any
        
        return compatibleObstacleId.Equals(obstacleId, System.StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Get valid spawn lanes (filtered by free lanes if needed)
    /// </summary>
    public List<int> GetValidSpawnLanes(List<int> freeLanes)
    {
        if (!onlySpawnInFreeLanes)
            return new List<int>(spawnLanes);
        
        List<int> validLanes = new List<int>();
        
        foreach (int lane in spawnLanes)
        {
            if (freeLanes.Contains(lane))
            {
                validLanes.Add(lane);
            }
        }
        
        return validLanes;
    }
    
    /// <summary>
    /// Calculate total pattern height
    /// </summary>
    public float GetTotalHeight()
    {
        return coinsPerLane * verticalSpacing;
    }
    
    /// <summary>
    /// Validate pattern
    /// </summary>
    public bool IsValid()
    {
        if (spawnLanes == null || spawnLanes.Count == 0)
        {
            Debug.LogError($"[{patternName}] No spawn lanes defined!");
            return false;
        }
        
        if (coinsPerLane <= 0)
        {
            Debug.LogError($"[{patternName}] Coins per lane must be > 0!");
            return false;
        }
        
        return true;
    }
}