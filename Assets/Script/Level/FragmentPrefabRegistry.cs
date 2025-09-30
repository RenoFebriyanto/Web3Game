using System;
using UnityEngine;

[Serializable]
public struct FragmentPrefabEntry
{
    public FragmentType type;
    [Tooltip("variant index, 0..2")]
    public int variant;
    public GameObject prefab;
}

[CreateAssetMenu(menuName = "Kulino/FragmentPrefabRegistry", fileName = "FragmentPrefabRegistry")]
public class FragmentPrefabRegistry : ScriptableObject
{
    public FragmentPrefabEntry[] entries;

    public GameObject GetPrefab(FragmentType type, int variant)
    {
        // try exact match
        foreach (var e in entries)
        {
            if (e.type == type && e.variant == variant && e.prefab != null) return e.prefab;
        }
        // fallback: any variant of same type
        foreach (var e in entries)
        {
            if (e.type == type && e.prefab != null) return e.prefab;
        }
        // no prefab found
        Debug.LogWarning($"[FragmentPrefabRegistry] Missing prefab for {type} variant {variant}");
        return null;
    }
}
