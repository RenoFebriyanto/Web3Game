using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Improved spawner dengan logic rapi seperti Subway Surf:
/// - Planet spawn random di 3 lanes
/// - Coin spawn continuous di lane yang TIDAK blocked
/// - Star spawn 3x random di gameplay
/// - Fragment spawn sesuai level requirements
/// </summary>
public class ImprovedGameplaySpawner : MonoBehaviour
{
    [Header("References")]
    public LanesManager lanesManager;
    public Transform spawnParent;
    public LevelDatabase levelDatabase;
    
    [Header("Prefabs")]
    public GameObject[] planetPrefabs;
    public GameObject coinPrefab;
    public GameObject starPrefab; // BARU: Prefab bintang
    public FragmentPrefabRegistry fragmentRegistry;
    
    [Header("World Settings")]
    public float spawnY = 10f;
    public float scrollSpeed = 3f;
    public float coinSpacing = 0.8f; // Jarak antar coin vertikal
    
    [Header("Planet Spawn")]
    public float planetInterval = 2.5f; // Interval spawn planet
    public float planetBlockDuration = 2f; // Durasi lane di-block setelah planet spawn
    
    [Header("Coin Spawn")]
    public float coinSpawnInterval = 0.3f; // Interval check spawn coin
    public int minCoinsInRow = 5; // Minimal coin berturut-turut
    public int maxCoinsInRow = 10; // Maksimal coin berturut-turut
    
    [Header("Fragment Spawn")]
    public float fragmentInterval = 3f;
    public float fragmentClusterInterval = 5f;
    
    [Header("Star Spawn")]
    public int totalStarsToSpawn = 3; // Jumlah bintang yang akan di-spawn
    public float minStarSpawnDelay = 8f; // Delay minimal antar bintang
    public float maxStarSpawnDelay = 15f; // Delay maksimal antar bintang

    // Runtime
    int laneCount = 3;
    float laneOffset = 2.5f;
    float[] laneBlockedUntil; // Track kapan lane bisa dipakai lagi
    FragmentRequirement[] levelRequirements;
    int starsSpawned = 0;

    void Awake()
    {
        if (lanesManager == null)
            lanesManager = FindFirstObjectByType<LanesManager>();
        
        if (lanesManager != null)
        {
            laneCount = lanesManager.laneCount;
            laneOffset = lanesManager.laneOffset;
        }
        
        laneBlockedUntil = new float[laneCount];
        for (int i = 0; i < laneCount; i++)
            laneBlockedUntil[i] = 0f;
        
        if (spawnParent == null)
            spawnParent = transform;
    }

    void Start()
    {
        LoadLevelRequirements();
        
        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(CoinSpawnLoop());
        StartCoroutine(FragmentSpawnLoop());
        StartCoroutine(StarSpawnLoop()); // BARU: Loop spawn bintang
    }

    void LoadLevelRequirements()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        
        if (levelDatabase != null)
        {
            LevelConfig config = levelDatabase.GetById(levelId);
            if (config != null && config.requirements != null)
            {
                levelRequirements = config.requirements.ToArray();
                Debug.Log($"[ImprovedSpawner] Loaded {levelRequirements.Length} requirements from {levelId}");
            }
        }
    }

    // ========================================
    // PLANET SPAWNER - Random lane, block lane after spawn
    // ========================================
    IEnumerator PlanetSpawnLoop()
    {
        yield return new WaitForSeconds(2f); // Initial delay
        
        while (true)
        {
            yield return new WaitForSeconds(planetInterval);
            
            // Pilih lane yang available
            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];
                SpawnPlanet(lane);
                
                // Block lane untuk sementara
                laneBlockedUntil[lane] = Time.time + planetBlockDuration;
            }
        }
    }

    void SpawnPlanet(int lane)
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0) return;
        
        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        
        GameObject planet = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
        
        var mover = planet.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(scrollSpeed);
        
        Debug.Log($"[ImprovedSpawner] Spawned planet at lane {lane}");
    }

    // ========================================
    // COIN SPAWNER - Continuous, avoid blocked lanes
    // ========================================
    IEnumerator CoinSpawnLoop()
    {
        yield return new WaitForSeconds(3f); // Initial delay
        
        while (true)
        {
            yield return new WaitForSeconds(coinSpawnInterval);
            
            // Pilih lane yang TIDAK blocked
            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count == 0) continue;
            
            // Random jumlah coin berturut-turut
            int coinCount = Random.Range(minCoinsInRow, maxCoinsInRow + 1);
            
            // Pilih 1-2 lanes untuk spawn coins
            int laneToUse = availableLanes[Random.Range(0, availableLanes.Count)];
            
            StartCoroutine(SpawnCoinRow(laneToUse, coinCount));
        }
    }

    IEnumerator SpawnCoinRow(int lane, int count)
    {
        if (coinPrefab == null) yield break;
        
        float spawnDelay = coinSpacing / scrollSpeed; // Delay agar coin rapi
        
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
            
            GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity, spawnParent);
            var mover = coin.GetComponent<PlanetMover>();
            if (mover != null) mover.SetSpeed(scrollSpeed);
            
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // ========================================
    // FRAGMENT SPAWNER - Based on level requirements
    // ========================================
    IEnumerator FragmentSpawnLoop()
    {
        yield return new WaitForSeconds(4f);
        
        while (true)
        {
            yield return new WaitForSeconds(fragmentInterval);
            
            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];
                SpawnFragment(lane);
            }
        }
    }

    void SpawnFragment(int lane)
    {
        if (levelRequirements == null || levelRequirements.Length == 0) return;
        if (fragmentRegistry == null) return;
        
        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];
        GameObject prefab = fragmentRegistry.GetPrefab(req.type, req.colorVariant);
        
        if (prefab == null) return;
        
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        GameObject frag = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
        
        var collectible = frag.GetComponent<FragmentCollectible>();
        if (collectible == null)
            collectible = frag.AddComponent<FragmentCollectible>();
        collectible.Initialize(req.type, req.colorVariant);
        
        var mover = frag.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(scrollSpeed);
    }

    // ========================================
    // STAR SPAWNER - Spawn 3 bintang secara random
    // ========================================
    IEnumerator StarSpawnLoop()
    {
        yield return new WaitForSeconds(5f); // Initial delay lebih lama
        
        // Notify GameplayStarManager berapa bintang yang akan di-spawn
        var starManager = FindFirstObjectByType<GameplayStarManager>();
        if (starManager != null)
        {
            starManager.totalStarsInLevel = totalStarsToSpawn;
        }
        
        // Spawn bintang satu per satu dengan interval random
        for (int i = 0; i < totalStarsToSpawn; i++)
        {
            float delay = Random.Range(minStarSpawnDelay, maxStarSpawnDelay);
            yield return new WaitForSeconds(delay);
            
            SpawnStar();
        }
        
        Debug.Log($"[ImprovedSpawner] Finished spawning {totalStarsToSpawn} stars");
    }

    void SpawnStar()
    {
        if (starPrefab == null)
        {
            Debug.LogWarning("[ImprovedSpawner] Star prefab not assigned!");
            return;
        }
        
        // Pilih lane available
        List<int> availableLanes = GetAvailableLanes();
        if (availableLanes.Count == 0)
        {
            Debug.LogWarning("[ImprovedSpawner] No available lanes for star!");
            return;
        }
        
        int lane = availableLanes[Random.Range(0, availableLanes.Count)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        
        GameObject star = Instantiate(starPrefab, pos, Quaternion.identity, spawnParent);
        
        var mover = star.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(scrollSpeed);
        
        starsSpawned++;
        Debug.Log($"[ImprovedSpawner] Spawned star {starsSpawned}/{totalStarsToSpawn} at lane {lane}");
    }

    // ========================================
    // HELPER METHODS
    // ========================================
    List<int> GetAvailableLanes()
    {
        List<int> available = new List<int>();
        for (int i = 0; i < laneCount; i++)
        {
            if (Time.time >= laneBlockedUntil[i])
                available.Add(i);
        }
        return available;
    }

    float GetLaneWorldX(int laneIndex)
    {
        float center = (laneCount - 1) / 2f;
        float centerX = lanesManager != null ? lanesManager.transform.position.x : 0f;
        float offset = lanesManager != null ? lanesManager.laneOffset : laneOffset;
        return centerX + (laneIndex - center) * offset;
    }
}