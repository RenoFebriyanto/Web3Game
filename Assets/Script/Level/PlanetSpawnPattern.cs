using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject untuk define planet spawn patterns
/// Mirip dengan CoinSpawnPattern tapi untuk planets
/// Letakkan di: Assets/Script/Level/PlanetSpawnPattern.cs
/// </summary>
[CreateAssetMenu(fileName = "PlanetPattern", menuName = "Kulino/Planet Spawn Pattern")]
public class PlanetSpawnPattern : ScriptableObject
{
    [Header("Pattern Info")]
    public string patternName = "Single Planet";
    [TextArea(2, 4)]
    public string description = "Simple single planet pattern";

    [Header("Pattern Configuration")]
    [Tooltip("List of spawn points. X = lane index (0,1,2), Y = vertical spacing from previous")]
    public List<Vector2> spawnPoints = new List<Vector2>();

    [Header("Pattern Timing")]
    [Tooltip("Minimum delay setelah pattern ini selesai (detik)")]
    public float minDelayAfter = 0.8f;

    [Tooltip("Maximum delay setelah pattern ini selesai (detik)")]
    public float maxDelayAfter = 2.0f;

    [Header("Pattern Weight")]
    [Tooltip("Probabilitas pattern ini dipilih (relative weight)")]
    [Range(1, 100)]
    public int selectionWeight = 10;

    [Header("Safety Settings")]
    [Tooltip("Minimum free lanes required (default: 1 - always leave escape route)")]
    [Range(1, 3)]
    public int minFreeLanes = 1;

    /// <summary>
    /// Get lanes yang akan di-block oleh pattern ini
    /// </summary>
    public HashSet<int> GetBlockedLanes(int baseLane, int totalLanes)
    {
        HashSet<int> blocked = new HashSet<int>();
        
        foreach (var point in spawnPoints)
        {
            int lane = baseLane + Mathf.RoundToInt(point.x);
            if (lane >= 0 && lane < totalLanes)
            {
                blocked.Add(lane);
            }
        }
        
        return blocked;
    }

    /// <summary>
    /// Check if pattern valid untuk spawn di base lane
    /// </summary>
    public bool IsValidForBaseLane(int baseLane, int totalLanes)
    {
        var blocked = GetBlockedLanes(baseLane, totalLanes);
        int freeLanes = totalLanes - blocked.Count;
        return freeLanes >= minFreeLanes;
    }

    /// <summary>
    /// Get random delay after this pattern
    /// </summary>
    public float GetRandomDelay()
    {
        return Random.Range(minDelayAfter, maxDelayAfter);
    }

    /// <summary>
    /// Get total vertical distance of pattern
    /// </summary>
    public float GetTotalVerticalDistance()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return 0f;
        
        float total = 0f;
        foreach (var point in spawnPoints)
        {
            total += point.y;
        }
        return total;
    }

    void OnValidate()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[PlanetSpawnPattern] {name}: No spawn points defined!");
        }

        minDelayAfter = Mathf.Max(0.5f, minDelayAfter);
        maxDelayAfter = Mathf.Max(minDelayAfter, maxDelayAfter);
    }
}