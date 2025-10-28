using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Advanced spawner dengan pattern system seperti Subway Surfers
/// REPLACE file: Assets/Script/Movement/GameplaySpawner.cs
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
    public GameObject starPrefab;
    public FragmentPrefabRegistry fragmentRegistry;

    [Header("Coin Pattern System")]
    [Tooltip("List of coin spawn patterns (akan dipilih random dengan weight)")]
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();

    [Tooltip("Override: Paksa gunakan pattern tertentu untuk testing")]
    public CoinSpawnPattern forcedPattern;

    [Header("World Settings")]
    public float spawnY = 10f;
    public float scrollSpeed = 3f;
    public float coinVerticalSpacing = 0.8f;

    [Header("Planet Spawn")]
    public float planetInterval = 2.5f;
    public float planetBlockDuration = 2f;

    [Header("Pattern Spawn Settings")]
    public float minPatternDelay = 2f;
    public float maxPatternDelay = 4f;
    public bool debugPatternSpawn = true;

    // Runtime
    int laneCount = 3;
    float laneOffset = 2.5f;
    float[] laneBlockedUntil;
    FragmentRequirement[] levelRequirements;
    int starsSpawned = 0;

    // Pattern spawn tracking
    List<CoinSpawnPattern> weightedPatterns = new List<CoinSpawnPattern>();
    int totalPatternWeight = 0;

    void Awake()
    {
        if (lanesManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            lanesManager = FindFirstObjectByType<LanesManager>();
#else
            lanesManager = FindObjectOfType<LanesManager>();
#endif
        }

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

        // Build weighted pattern list
        BuildWeightedPatternList();
    }

    void Start()
    {
        LoadLevelRequirements();

        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(PatternSpawnLoop()); // NEW: Pattern-based spawn
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
    // PATTERN SYSTEM
    // ========================================

    void BuildWeightedPatternList()
    {
        weightedPatterns.Clear();
        totalPatternWeight = 0;

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            Debug.LogWarning("[ImprovedSpawner] No coin patterns assigned! Pattern spawn will not work.");
            return;
        }

        foreach (var pattern in coinPatterns)
        {
            if (pattern == null) continue;
            totalPatternWeight += pattern.selectionWeight;
        }

        Debug.Log($"[ImprovedSpawner] Loaded {coinPatterns.Count} patterns. Total weight: {totalPatternWeight}");
    }

    CoinSpawnPattern SelectRandomPattern()
    {
        // Test mode: force specific pattern
        if (forcedPattern != null)
        {
            return forcedPattern;
        }

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            Debug.LogWarning("[ImprovedSpawner] No patterns available!");
            return null;
        }

        // Weighted random selection
        int randomWeight = Random.Range(0, totalPatternWeight);
        int cumulative = 0;

        foreach (var pattern in coinPatterns)
        {
            if (pattern == null) continue;

            cumulative += pattern.selectionWeight;
            if (randomWeight < cumulative)
            {
                return pattern;
            }
        }

        // Fallback
        return coinPatterns[0];
    }

    IEnumerator PatternSpawnLoop()
    {
        yield return new WaitForSeconds(3f); // Initial delay

        while (true)
        {
            // Select random lane
            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            int baseLane = availableLanes[Random.Range(0, availableLanes.Count)];

            // Select pattern
            CoinSpawnPattern pattern = SelectRandomPattern();
            if (pattern == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Spawn pattern
            yield return StartCoroutine(SpawnPattern(pattern, baseLane));

            // Wait before next pattern
            float delay = pattern.GetRandomDelay();
            if (debugPatternSpawn)
            {
                Debug.Log($"[PatternSpawner] Spawned pattern '{pattern.patternName}' at lane {baseLane}. Next in {delay:F1}s");
            }

            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator SpawnPattern(CoinSpawnPattern pattern, int baseLane)
    {
        if (pattern == null || pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
        {
            yield break;
        }

        float currentY = spawnY;

        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];

            // Calculate lane (baseLane + offset)
            int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);

            // Calculate Y position
            currentY -= point.y * coinVerticalSpacing;

            // Decide what to spawn (coin/fragment/star based on %)
            GameObject prefabToSpawn = DecideSpawnItem(pattern);

            if (prefabToSpawn != null)
            {
                Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
                GameObject spawned = Instantiate(prefabToSpawn, pos, Quaternion.identity, spawnParent);

                // Setup mover
                var mover = spawned.GetComponent<PlanetMover>();
                if (mover != null) mover.SetSpeed(scrollSpeed);

                // Setup collectible component if needed
                SetupCollectibleComponent(spawned, prefabToSpawn);
            }

            // Small delay between items in pattern (untuk effect visual)
            yield return new WaitForSeconds(0.05f);
        }
    }

    GameObject DecideSpawnItem(CoinSpawnPattern pattern)
    {
        float roll = Random.Range(0f, 100f);

        // Check star chance first (lebih rare)
        if (roll < pattern.starSubstituteChance)
        {
            if (starPrefab != null)
            {
                return starPrefab;
            }
        }

        // Check fragment chance
        roll -= pattern.starSubstituteChance;
        if (roll < pattern.fragmentSubstituteChance)
        {
            // Spawn fragment yang dibutuhkan level
            GameObject fragmentPrefab = GetRequiredFragmentPrefab();
            if (fragmentPrefab != null)
            {
                return fragmentPrefab;
            }
        }

        // Default: spawn coin
        return coinPrefab;
    }

    GameObject GetRequiredFragmentPrefab()
    {
        if (levelRequirements == null || levelRequirements.Length == 0)
            return null;

        if (fragmentRegistry == null)
            return null;

        // Pilih random requirement
        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];
        return fragmentRegistry.GetPrefab(req.type, req.colorVariant);
    }

    void SetupCollectibleComponent(GameObject spawned, GameObject originalPrefab)
    {
        // Fragment collectible
        if (originalPrefab != coinPrefab && originalPrefab != starPrefab)
        {
            var collectible = spawned.GetComponent<FragmentCollectible>();
            if (collectible == null)
            {
                collectible = spawned.AddComponent<FragmentCollectible>();
            }

            // Find requirement for this fragment
            if (levelRequirements != null)
            {
                foreach (var req in levelRequirements)
                {
                    var prefab = fragmentRegistry?.GetPrefab(req.type, req.colorVariant);
                    if (prefab != null && prefab.name == originalPrefab.name)
                    {
                        collectible.Initialize(req.type, req.colorVariant);
                        break;
                    }
                }
            }
        }
    }

    // ========================================
    // PLANET SPAWNER (unchanged)
    // ========================================
    IEnumerator PlanetSpawnLoop()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            yield return new WaitForSeconds(planetInterval);

            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];
                SpawnPlanet(lane);
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

    // ========================================
    // DEBUG
    // ========================================
    [ContextMenu("Test Spawn Pattern")]
    void TestSpawnPattern()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode to test spawn!");
            return;
        }

        StartCoroutine(SpawnPattern(SelectRandomPattern(), 1));
    }
}