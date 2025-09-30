// PlanetPrefabRegistry.cs
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetPrefabRegistry", menuName = "Planet Prefabs/PlanetPrefabRegistry")]
public class PlanetPrefabRegistry : ScriptableObject
{
    [Tooltip("Assign planet prefabs here")]
    public GameObject[] prefabs;

    public GameObject GetRandomPrefab()
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        if (prefabs.Length == 1) return prefabs[0];
        return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
    }
}
