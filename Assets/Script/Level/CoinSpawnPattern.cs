using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a coin spawn pattern (straight, zigzag, gap, etc.)
/// Letakkan di: Assets/Script/Movement/CoinSpawnPattern.cs
/// </summary>
[CreateAssetMenu(fileName = "CoinPattern", menuName = "Kulino/Coin Spawn Pattern")]
public class CoinSpawnPattern : ScriptableObject
{
    [Header("Pattern Info")]
    public string patternName = "Straight Line";
    [TextArea(2, 4)]
    public string description = "Simple straight line pattern";

    [Header("Pattern Configuration")]
    [Tooltip("List of spawn points. X = lane offset (-1, 0, 1), Y = vertical spacing from previous")]
    public List<Vector2> spawnPoints = new List<Vector2>();

    [Header("Item Substitution (%)")]
    [Tooltip("Chance untuk spawn Fragment yang dibutuhkan level (0-100)")]
    [Range(0f, 100f)]
    public float fragmentSubstituteChance = 15f;

    [Tooltip("Chance untuk spawn Star bintang (0-100)")]
    [Range(0f, 100f)]
    public float starSubstituteChance = 5f;

    [Header("Spawn Timing")]
    [Tooltip("Minimum delay setelah pattern ini selesai (detik)")]
    public float minDelayAfter = 1f;

    [Tooltip("Maximum delay setelah pattern ini selesai (detik)")]
    public float maxDelayAfter = 3f;

    [Header("Pattern Weight")]
    [Tooltip("Probabilitas pattern ini dipilih (relative weight)")]
    [Range(1, 100)]
    public int selectionWeight = 10;

    /// <summary>
    /// Get total number of items in this pattern
    /// </summary>
    public int ItemCount => spawnPoints != null ? spawnPoints.Count : 0;

    /// <summary>
    /// Get random delay after this pattern
    /// </summary>
    public float GetRandomDelay()
    {
        return Random.Range(minDelayAfter, maxDelayAfter);
    }

    /// <summary>
    /// Validate pattern data
    /// </summary>
    void OnValidate()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[CoinSpawnPattern] {name}: No spawn points defined!");
        }

        // Clamp values
        fragmentSubstituteChance = Mathf.Clamp(fragmentSubstituteChance, 0f, 100f);
        starSubstituteChance = Mathf.Clamp(starSubstituteChance, 0f, 100f);

        if (fragmentSubstituteChance + starSubstituteChance > 100f)
        {
            Debug.LogWarning($"[CoinSpawnPattern] {name}: Total substitute chance > 100%! Coin spawn chance akan kecil.");
        }
    }
}