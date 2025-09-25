// Assets/Script/Spawners/CrystalSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class CrystalSpawner : MonoBehaviour
{
    public GameObject crystalPrefab;
    public float spawnY = 12f;
    public float crystalSpacing = 0.8f;
    public int maxCrystalsInPattern = 5;
    public float worldScrollSpeed = 3f;
    public float spawnProbability = 0.3f; // 30% chance to spawn crystals

    [Header("Spawn Patterns")]
    public int weightSingle = 40;
    public int weightLine = 30;
    public int weightTriangle = 20;
    public int weightDiamond = 10;

    private System.Random rng = new System.Random();

    void Start()
    {
        if (crystalPrefab == null)
            Debug.LogError("[CrystalSpawner] crystalPrefab missing");
    }

    // Called by other spawners or game systems
    public void TrySpawnCrystals()
    {
        if (Random.value <= spawnProbability)
        {
            int laneIndex = Random.Range(0, LanesManager.Instance.laneCount);
            SpawnPatternAtLane(laneIndex);
        }
    }

    public void SpawnPatternAtLane(int laneIndex)
    {
        int pattern = PickPattern();
        switch (pattern)
        {
            case 0: SpawnSingle(laneIndex); break;
            case 1: SpawnLine(laneIndex); break;
            case 2: SpawnTriangle(laneIndex); break;
            case 3: SpawnDiamond(laneIndex); break;
            default: SpawnSingle(laneIndex); break;
        }
    }

    private int PickPattern()
    {
        List<int> pool = new List<int>();
        for (int i = 0; i < weightSingle; i++) pool.Add(0);
        for (int i = 0; i < weightLine; i++) pool.Add(1);
        for (int i = 0; i < weightTriangle; i++) pool.Add(2);
        for (int i = 0; i < weightDiamond; i++) pool.Add(3);
        return pool[rng.Next(pool.Count)];
    }

    private void SpawnCrystalAt(Vector3 pos, int value = 1)
    {
        var crystal = Instantiate(crystalPrefab, pos, Quaternion.identity);
        var mover = crystal.GetComponent<CrystalMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);

        var col = crystal.GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        var collectible = crystal.GetComponent<CrystalCollectible>();
        if (collectible != null) collectible.amount = value;
    }

    private void SpawnSingle(int laneIndex)
    {
        float x = LanesManager.Instance.LaneToWorldX(laneIndex);
        SpawnCrystalAt(new Vector3(x, spawnY, 0f));
    }

    private void SpawnLine(int laneIndex)
    {
        int count = rng.Next(2, 4);
        float x = LanesManager.Instance.LaneToWorldX(laneIndex);
        for (int i = 0; i < count; i++)
        {
            SpawnCrystalAt(new Vector3(x, spawnY - i * crystalSpacing, 0f));
        }
    }

    private void SpawnTriangle(int laneIndex)
    {
        // Center crystal
        float centerX = LanesManager.Instance.LaneToWorldX(laneIndex);
        SpawnCrystalAt(new Vector3(centerX, spawnY, 0f));

        // Two bottom crystals
        if (laneIndex > 0)
            SpawnCrystalAt(new Vector3(LanesManager.Instance.LaneToWorldX(laneIndex - 1), spawnY - crystalSpacing, 0f));
        if (laneIndex < LanesManager.Instance.laneCount - 1)
            SpawnCrystalAt(new Vector3(LanesManager.Instance.LaneToWorldX(laneIndex + 1), spawnY - crystalSpacing, 0f));
    }

    private void SpawnDiamond(int laneIndex)
    {
        float centerX = LanesManager.Instance.LaneToWorldX(laneIndex);

        // Top crystal
        SpawnCrystalAt(new Vector3(centerX, spawnY, 0f));

        // Middle crystals
        if (laneIndex > 0)
            SpawnCrystalAt(new Vector3(LanesManager.Instance.LaneToWorldX(laneIndex - 1), spawnY - crystalSpacing, 0f));
        if (laneIndex < LanesManager.Instance.laneCount - 1)
            SpawnCrystalAt(new Vector3(LanesManager.Instance.LaneToWorldX(laneIndex + 1), spawnY - crystalSpacing, 0f));

        // Bottom crystal
        SpawnCrystalAt(new Vector3(centerX, spawnY - crystalSpacing * 2, 0f));
    }
}