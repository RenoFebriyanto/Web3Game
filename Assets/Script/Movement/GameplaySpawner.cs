using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FINAL VERSION - Subway Surfers style spawner
/// - Natural spawn timing (items appear one by one)
/// - No overlapping with proper lane blocking
/// - Unified speed system via DifficultyManager
/// - Progressive difficulty
/// 
/// REPLACE: Assets/Script/Movement/GameplaySpawner.cs
/// </summary>
public class ImprovedGameplaySpawner : MonoBehaviour
{
    [Header("References")]
    public LanesManager lanesManager;
    public Transform spawnParent;
    public LevelDatabase levelDatabase;
    public DifficultyManager difficultyManager;

    [Header("Prefabs")]
    public GameObject[] planetPrefabs;
    public GameObject coinPrefab;
    public GameObject starPrefab;
    public FragmentPrefabRegistry fragmentRegistry;

    [Header("Coin Pattern System")]
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();
    public CoinSpawnPattern forcedPattern;

    [Header("World Settings")]
    public float spawnY = 10f;
    public float coinVerticalSpacing = 0.8f;

    [Header("Planet Spawn")]
    public float planetInterval = 2.5f;
    public float planetBlockDuration = 3f;

    [Header("Pattern Spawn Settings")]
    public float minPatternDelay = 2f;
    public float maxPatternDelay = 4f;
    public bool debugPatternSpawn = true;

    [Header("Spawn Timing (CRITICAL)")]
    [Tooltip("Delay between each item in pattern (makes spawn natural)")]
    public float itemSpawnDelay = 0.15f;

    [Tooltip("Safety distance from last spawned object")]
    public float minSpawnDistance = 2f;

    // Runtime
    int laneCount = 3;
    float laneOffset = 2.5f;
    float[] laneBlockedUntil;
    float[] lastSpawnYInLane; // Track last spawn Y position per lane
    FragmentRequirement[] levelRequirements;
    int totalPatternWeight = 0;

    void Awake()
    {
        // Auto-find references
        if (lanesManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            lanesManager = FindFirstObjectByType<LanesManager>();
#else
            lanesManager = FindObjectOfType<LanesManager>();
#endif
        }

        if (difficultyManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            difficultyManager = FindFirstObjectByType<DifficultyManager>();
#else
            difficultyManager = FindObjectOfType<DifficultyManager>();
#endif
        }

        if (lanesManager != null)
        {
            laneCount = lanesManager.laneCount;
            laneOffset = lanesManager.laneOffset;
        }

        laneBlockedUntil = new float[laneCount];
        lastSpawnYInLane = new float[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            laneBlockedUntil[i] = 0f;
            lastSpawnYInLane[i] = spawnY + 10f; // Initialize far above screen
        }

        if (spawnParent == null)
            spawnParent = transform;

        BuildWeightedPatternList();
    }

    void Start()
    {
        LoadLevelRequirements();

        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(PatternSpawnLoop());
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
        totalPatternWeight = 0;

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            Debug.LogWarning("[ImprovedSpawner] No coin patterns assigned!");
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
        if (forcedPattern != null)
            return forcedPattern;

        if (coinPatterns == null || coinPatterns.Count == 0)
            return null;

        int randomWeight = Random.Range(0, totalPatternWeight);
        int cumulative = 0;

        foreach (var pattern in coinPatterns)
        {
            if (pattern == null) continue;

            cumulative += pattern.selectionWeight;
            if (randomWeight < cumulative)
                return pattern;
        }

        return coinPatterns[0];
    }

    IEnumerator PatternSpawnLoop()
    {
        yield return new WaitForSeconds(3f);

        while (true)
        {
            // Get available lanes
            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // Select lane
            int baseLane = availableLanes[Random.Range(0, availableLanes.Count)];

            // Select pattern
            CoinSpawnPattern pattern = SelectRandomPattern();
            if (pattern == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Check if lane is clear enough
            if (!IsLaneClearForPattern(baseLane))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // Block lane during spawn
            float patternDuration = CalculatePatternDuration(pattern);
            laneBlockedUntil[baseLane] = Time.time + patternDuration;

            // Spawn pattern (items spawn one by one)
            yield return StartCoroutine(SpawnPattern(pattern, baseLane));

            // Wait before next pattern
            float delay = pattern.GetRandomDelay();

            if (debugPatternSpawn)
            {
                Debug.Log($"[PatternSpawner] '{pattern.patternName}' lane {baseLane}. Next in {delay:F1}s");
            }

            yield return new WaitForSeconds(delay);
        }
    }

    float CalculatePatternDuration(CoinSpawnPattern pattern)
    {
        if (pattern == null || pattern.spawnPoints == null)
            return 1f;

        float totalDelay = pattern.spawnPoints.Count * itemSpawnDelay;
        return totalDelay + 0.5f; // Add buffer
    }

    bool IsLaneClearForPattern(int lane)
    {
        // Check if last spawn in this lane is far enough
        float lastY = lastSpawnYInLane[lane];
        float minClearDistance = minSpawnDistance;

        return (spawnY - lastY) >= minClearDistance;
    }

    IEnumerator SpawnPattern(CoinSpawnPattern pattern, int baseLane)
    {
        if (pattern == null || pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
            yield break;

        float currentY = spawnY;
        float currentSpeed = GetCurrentSpeed();

        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];

            // Calculate target lane
            int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);

            // Calculate Y position (relative to previous item)
            currentY -= point.y * coinVerticalSpacing;

            // Decide what to spawn
            GameObject prefabToSpawn = DecideSpawnItem(pattern);

            if (prefabToSpawn != null)
            {
                Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
                GameObject spawned = Instantiate(prefabToSpawn, pos, Quaternion.identity, spawnParent);

                // Setup mover with CURRENT speed
                SetupMover(spawned, currentSpeed);

                // Setup collectible component
                SetupCollectibleComponent(spawned, prefabToSpawn);

                // Update last spawn Y for this lane
                lastSpawnYInLane[targetLane] = currentY;
            }

            // CRITICAL: Wait before spawning next item (makes it natural)
            yield return new WaitForSeconds(itemSpawnDelay);
        }
    }

    GameObject DecideSpawnItem(CoinSpawnPattern pattern)
    {
        float roll = Random.Range(0f, 100f);

        // Star (rarest)
        if (roll < pattern.starSubstituteChance)
        {
            if (starPrefab != null)
                return starPrefab;
        }

        // Fragment
        roll -= pattern.starSubstituteChance;
        if (roll < pattern.fragmentSubstituteChance)
        {
            GameObject fragmentPrefab = GetRequiredFragmentPrefab();
            if (fragmentPrefab != null)
                return fragmentPrefab;
        }

        // Default: Coin
        return coinPrefab;
    }

    GameObject GetRequiredFragmentPrefab()
    {
        if (levelRequirements == null || levelRequirements.Length == 0)
            return null;

        if (fragmentRegistry == null)
            return null;

        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];
        return fragmentRegistry.GetPrefab(req.type, req.colorVariant);
    }

    void SetupCollectibleComponent(GameObject spawned, GameObject originalPrefab)
    {
        // Skip if it's coin or star (already have pickup scripts)
        if (originalPrefab == coinPrefab || originalPrefab == starPrefab)
            return;

        // Fragment collectible
        var collectible = spawned.GetComponent<FragmentCollectible>();
        if (collectible == null)
            collectible = spawned.AddComponent<FragmentCollectible>();

        // Find matching requirement
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

    // ========================================
    // PLANET SPAWNER
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

                // Check if lane is clear
                if (IsLaneClearForPattern(lane))
                {
                    SpawnPlanet(lane);
                    laneBlockedUntil[lane] = Time.time + planetBlockDuration;
                }
            }
        }
    }

    void SpawnPlanet(int lane)
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0) return;

        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        GameObject planet = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

        // Set speed
        float currentSpeed = GetCurrentSpeed();
        SetupMover(planet, currentSpeed);

        // Update last spawn Y
        lastSpawnYInLane[lane] = spawnY;
    }

    // ========================================
    // UNIFIED MOVER SETUP
    // ========================================

    void SetupMover(GameObject obj, float speed)
    {
        // Try all mover types
        var planetMover = obj.GetComponent<PlanetMover>();
        if (planetMover != null)
        {
            planetMover.SetSpeed(speed);
            return;
        }

        var coinMover = obj.GetComponent<CoinMover>();
        if (coinMover != null)
        {
            coinMover.SetSpeed(speed);
            return;
        }

        var fragmentMover = obj.GetComponent<FragmentMover>();
        if (fragmentMover != null)
        {
            fragmentMover.SetSpeed(speed);
            return;
        }

        // Fallback: add PlanetMover
        var mover = obj.AddComponent<PlanetMover>();
        mover.SetSpeed(speed);
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    float GetCurrentSpeed()
    {
        if (difficultyManager != null)
            return difficultyManager.CurrentSpeed;

        return 3f; // Fallback
    }

    List<int> GetAvailableLanes()
    {
        List<int> available = new List<int>();
        float currentTime = Time.time;

        for (int i = 0; i < laneCount; i++)
        {
            if (currentTime >= laneBlockedUntil[i])
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
            Debug.LogWarning("Must be in Play Mode!");
            return;
        }

        StartCoroutine(SpawnPattern(SelectRandomPattern(), 1));
    }

    void OnDrawGizmos()
    {
        // Draw spawn zones
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(-10, spawnY, 0),
            new Vector3(10, spawnY, 0)
        );

        // Draw lane positions
        for (int i = 0; i < laneCount; i++)
        {
            float x = GetLaneWorldX(i);
            Gizmos.color = Time.time >= laneBlockedUntil[i] ? Color.green : Color.red;
            Gizmos.DrawLine(
                new Vector3(x, spawnY - 2, 0),
                new Vector3(x, spawnY + 2, 0)
            );
        }
    }
}