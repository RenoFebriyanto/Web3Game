using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Coin Spawn Pattern dengan Obstacle Pattern Reference
/// Pattern ini HARUS match dengan obstacle pattern ID
/// </summary>
[CreateAssetMenu(fileName = "CoinPattern", menuName = "Kulino/Spawn Patterns/Coin Pattern")]
public class CoinSpawnPattern : ScriptableObject
{
    [Header("Pattern Identity")]
    [Tooltip("Pattern name untuk display")]
    public string patternName = "Coin Pattern";
    
    [Tooltip("Obstacle Pattern ID yang compatible (contoh: obs_left_right)")]
    public string compatibleObstacleId = "obs_pattern_1";
    
    [Header("Pattern Layout")]
    [Tooltip("Spawn points (x=lane offset, y=vertical spacing multiplier)")]
    public List<Vector2> spawnPoints = new List<Vector2>();
    
    [Header("Spawn Settings")]
    [Tooltip("Selection weight (higher = more likely)")]
    [Range(1, 100)]
    public int selectionWeight = 50;
    
    [Tooltip("Delay range setelah pattern spawn (detik)")]
    public Vector2 delayRange = new Vector2(0.3f, 1.0f);
    
    [Header("Fragment Substitution")]
    [Tooltip("Chance untuk spawn fragment instead of coin (0-100%)")]
    [Range(0f, 100f)]
    public float fragmentSubstituteChance = 15f;
    
    [Header("Pattern Type")]
    [Tooltip("Apakah pattern ini untuk free lanes only?")]
    public bool spawnOnlyInFreeLanes = true;
    
    [Header("Preview")]
    public Texture2D previewTexture;
    
    public float GetRandomDelay()
    {
        return Random.Range(delayRange.x, delayRange.y);
    }
    
    public float GetTotalVerticalDistance()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return 0f;
        return spawnPoints.Sum(p => p.y);
    }
    
    /// <summary>
    /// Check if this pattern compatible dengan obstacle pattern ID
    /// </summary>
    public bool IsCompatibleWith(string obstaclePatternId)
    {
        if (string.IsNullOrEmpty(compatibleObstacleId))
            return true; // No restriction
        
        return compatibleObstacleId.Equals(obstaclePatternId, System.StringComparison.OrdinalIgnoreCase);
    }
}