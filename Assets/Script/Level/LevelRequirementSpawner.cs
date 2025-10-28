using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class LevelRequirementSpawner : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    public LevelGameSession session;
    public FragmentPrefabRegistry registry;
    public Transform spawnParent;

    [Header("Layout / lanes")]
    public int laneCount = 3;
    public float laneXOffset = 2.5f;
    public float startY = 8f;
    public float ySpacing = 0.8f;

    readonly string[] possibleCountNames = new[] { "count", "amount", "quantity", "qty" };
    readonly string[] possibleTypeNames = new[] { "type", "fragmentType", "fragment", "fragmentKey", "name" };
    readonly string[] possibleColorNames = new[] { "colorVariant", "variant", "color", "colorIndex", "variantIndex" };

    void Start()
    {
        if (session == null)
        {
            session = FindFirstObjectByType<LevelGameSession>();
        }

        if (session == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] No LevelGameSession found.");
            return;
        }

        if (registry == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] FragmentPrefabRegistry not assigned.");
        }

        if (spawnParent == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] spawnParent not assigned. Using this.transform.");
            spawnParent = this.transform;
        }

        // PENTING: Jangan spawn di Start, biarkan ImprovedGameplaySpawner yang handle
        // Kita hanya perlu memastikan level config sudah loaded
        if (session.currentLevel != null)
        {
            Debug.Log($"[LevelRequirementSpawner] Level '{session.currentLevel.id}' loaded, requirements ready for spawner");
        }
    }

    // Method ini TIDAK dipanggil otomatis lagi
    // ImprovedGameplaySpawner akan handle spawning berdasarkan level requirements
    public void SpawnForCurrentLevel()
    {
        // Method ini bisa dihapus atau dibiarkan kosong
        // Karena spawning sekarang handled by ImprovedGameplaySpawner
        Debug.Log("[LevelRequirementSpawner] SpawnForCurrentLevel called (deprecated, use ImprovedGameplaySpawner)");
    }

    int TryGetInt(object obj, string[] names, int defaultValue)
    {
        object v = GetMemberValueFlexible(obj, names);
        if (v == null) return defaultValue;
        if (v is int) return (int)v;
        if (v is long) return (int)(long)v;
        if (v is float) return Mathf.RoundToInt((float)v);
        if (v is string)
        {
            int parsed;
            return int.TryParse((string)v, out parsed) ? parsed : defaultValue;
        }
        try { return Convert.ToInt32(v); }
        catch { return defaultValue; }
    }

    object GetMemberValueFlexible(object obj, string[] candidateNames)
    {
        if (obj == null || candidateNames == null) return null;
        Type t = obj.GetType();
        foreach (var name in candidateNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f.GetValue(obj);
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return p.GetValue(obj);
        }
        return null;
    }
}