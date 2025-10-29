using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OPTIMIZED VERSION - Faster continuous planet spawn
/// KEY CHANGES:
/// 1. planetInterval: 2.5s → 1.2s (MUCH faster)
/// 2. Lane block duration: 3f → 1.5f (shorter)
/// 3. Pattern delays reduced
/// </summary>
public class FixedGameplaySpawner : MonoBehaviour
{
    [Header("🔧 REQUIRED SETUP")]
    public LanesManager lanesManager;
    public DifficultyManager difficultyManager;

    [Header("📦 PREFABS")]
    public GameObject[] planetPrefabs;
    public GameObject coinPrefab;
    public GameObject starPrefab;

    [Header("📚 DATA ASSETS")]
    public FragmentPrefabRegistry fragmentRegistry;
    public LevelDatabase levelDatabase;

    [Header("🎨 COIN PATTERNS")]
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();

    [Header("⚙️ SPAWN SETTINGS")]
    public Transform spawnParent;
    public float spawnY = 10f;
    public float coinVerticalSpacing = 2f;

    [Tooltip("✅ OPTIMIZED: Planet spawn interval (1.2s for continuous spawn)")]
    public float planetInterval = 1.2f; // ✅ REDUCED from 2.5s → 1.2s

    public float itemSpawnDelay = 0.15f;
    public float minSpawnDistance = 2f;

    [Header("⭐ STAR CONTROL")]
    public int maxStarsPerLevel = 3;
    [Range(0f, 1f)]
    public float lastStarProgressThreshold = 0.7f;

    [Header("🎯 DEBUG")]
    public bool enableDebugLogs = false; // ✅ DEFAULT OFF (less spam)
    public bool showGizmos = true;

    [Header("📊 RUNTIME STATUS")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private int totalSpawned = 0;
    [SerializeField] private int coinSpawned = 0;
    [SerializeField] private int planetSpawned = 0;
    [SerializeField] private int fragmentSpawned = 0;
    [SerializeField] private int starSpawned = 0;

    // Runtime
    private int laneCount = 3;
    private float laneOffset = 2.5f;
    private float[] laneBlockedUntil;
    private float[] lastSpawnYInLane;
    private FragmentRequirement[] levelRequirements;
    private int totalPatternWeight = 0;

    void Awake()
    {
        Log("=== SPAWNER AWAKE ===");

        if (lanesManager == null)
        {
            lanesManager = FindFirstObjectByType<LanesManager>();
        }

        if (difficultyManager == null)
        {
            difficultyManager = FindFirstObjectByType<DifficultyManager>();
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
            lastSpawnYInLane[i] = spawnY - minSpawnDistance - 5f;
        }

        if (spawnParent == null)
        {
            spawnParent = transform;
        }

        BuildWeightedPatternList();
    }

    void Start()
    {
        Log("=== SPAWNER START ===");

        if (!ValidateSetup())
        {
            LogError("VALIDATION FAILED!");
            return;
        }

        LoadLevelRequirements();

        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(PatternSpawnLoop());

        isInitialized = true;
        Log("✓ INITIALIZED");
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (planetPrefabs == null || planetPrefabs.Length == 0)
        {
            LogError("❌ planetPrefabs EMPTY!");
            valid = false;
        }

        if (coinPrefab == null)
        {
            LogError("❌ coinPrefab NULL!");
            valid = false;
        }

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogError("❌ coinPatterns EMPTY!");
            valid = false;
        }

        return valid;
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
                Log($"✓ Loaded {levelRequirements.Length} requirements");
            }
        }
    }

    void BuildWeightedPatternList()
    {
        totalPatternWeight = 0;

        if (coinPatterns == null || coinPatterns.Count == 0) return;

        foreach (var pattern in coinPatterns)
        {
            if (pattern == null) continue;
            totalPatternWeight += pattern.selectionWeight;
        }
    }

    // ========================================
    // ✅ PLANET SPAWNER - OPTIMIZED
    // ========================================

    IEnumerator PlanetSpawnLoop()
    {
        Log("Planet spawn loop started");
        yield return new WaitForSeconds(0.5f); // ✅ Quick initial delay

        while (true)
        {
            yield return new WaitForSeconds(planetInterval); // ✅ Now 1.2s (fast!)

            if (BoosterManager.Instance != null && !BoosterManager.Instance.CanSpawn())
            {
                continue;
            }

            List<int> availableLanes = GetAvailableLanes();

            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];

                // ✅ RELAXED: Spawn even if not perfectly clear (allow some overlap)
                SpawnPlanet(lane);
            }
        }
    }

    void SpawnPlanet(int lane)
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0) return;

        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        GameObject planet = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

        float currentSpeed = GetCurrentSpeed();
        SetupMover(planet, currentSpeed);

        lastSpawnYInLane[lane] = spawnY;
        laneBlockedUntil[lane] = Time.time + 1.5f; // ✅ REDUCED from 3f → 1.5f

        planetSpawned++;
        totalSpawned++;
    }

    // ========================================
    // PATTERN SPAWNER - REDUCED DELAYS
    // ========================================

    IEnumerator PatternSpawnLoop()
    {
        Log("Pattern spawn loop started");
        yield return new WaitForSeconds(1.5f); // ✅ Reduced from 3f

        while (true)
        {
            if (BoosterManager.Instance != null && !BoosterManager.Instance.CanSpawn())
            {
                yield return new WaitForSeconds(0.3f); // ✅ Reduced from 0.5f
                continue;
            }

            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.3f); // ✅ Reduced from 0.5f
                continue;
            }

            int baseLane = availableLanes[Random.Range(0, availableLanes.Count)];
            CoinSpawnPattern pattern = SelectRandomPattern();

            if (pattern == null)
            {
                yield return new WaitForSeconds(0.5f); // ✅ Reduced from 1f
                continue;
            }

            if (!IsLaneClearForSpawn(baseLane))
            {
                yield return new WaitForSeconds(0.3f); // ✅ Reduced from 0.5f
                continue;
            }

            HashSet<int> usedLanes = new HashSet<int>();
            usedLanes.Add(baseLane);

            foreach (var point in pattern.spawnPoints)
            {
                int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);
                usedLanes.Add(targetLane);
            }

            float patternDuration = CalculatePatternDuration(pattern);
            float blockUntil = Time.time + patternDuration + 0.5f; // ✅ Reduced from +1f

            foreach (int lane in usedLanes)
            {
                laneBlockedUntil[lane] = blockUntil;
            }

            yield return StartCoroutine(SpawnPattern(pattern, baseLane));

            float delay = pattern.GetRandomDelay() * 0.7f; // ✅ Reduce pattern delay by 30%
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
        float currentSpeed = GetCurrentSpeed();
        float minY = -2f;

        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];
            int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);
            currentY -= point.y * coinVerticalSpacing;

            if (currentY < minY) break;

            GameObject prefabToSpawn = DecideSpawnItem(pattern);

            if (prefabToSpawn != null)
            {
                Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
                GameObject spawned = Instantiate(prefabToSpawn, pos, Quaternion.identity, spawnParent);

                SetupMover(spawned, currentSpeed);
                SetupCollectibleComponent(spawned, prefabToSpawn);

                lastSpawnYInLane[targetLane] = currentY;

                if (prefabToSpawn == coinPrefab) coinSpawned++;
                else if (prefabToSpawn == starPrefab) starSpawned++;
                else if (prefabToSpawn != coinPrefab && prefabToSpawn != starPrefab) fragmentSpawned++;

                totalSpawned++;
            }

            yield return new WaitForSeconds(itemSpawnDelay);
        }
    }

    GameObject DecideSpawnItem(CoinSpawnPattern pattern)
    {
        float roll = Random.Range(0f, 100f);

        // Star control
        if (roll < pattern.starSubstituteChance)
        {
            if (CanSpawnStar())
            {
                return starPrefab;
            }
        }

        // Fragment
        roll -= pattern.starSubstituteChance;
        if (roll < pattern.fragmentSubstituteChance)
        {
            GameObject fragmentPrefab = GetRequiredFragmentPrefab();
            if (fragmentPrefab != null)
            {
                return fragmentPrefab;
            }
        }

        // Default: Coin
        return coinPrefab;
    }

    bool CanSpawnStar()
    {
        if (starPrefab == null) return false;
        if (starSpawned >= maxStarsPerLevel) return false;

        if (starSpawned == 2)
        {
            float fragmentProgress = GetFragmentCollectionProgress();

            if (fragmentProgress < lastStarProgressThreshold)
            {
                return false;
            }
        }

        return true;
    }

    float GetFragmentCollectionProgress()
    {
        if (LevelGameSession.Instance == null || levelRequirements == null || levelRequirements.Length == 0)
        {
            return 0f;
        }

        int totalRequired = 0;
        int totalCollected = 0;

        foreach (var req in levelRequirements)
        {
            totalRequired += req.count;

            int remaining = LevelGameSession.Instance.GetRemaining(req.type, req.colorVariant);
            int collected = req.count - remaining;

            totalCollected += collected;
        }

        if (totalRequired == 0) return 0f;

        return (float)totalCollected / totalRequired;
    }

    GameObject GetRequiredFragmentPrefab()
    {
        if (levelRequirements == null || levelRequirements.Length == 0) return null;
        if (fragmentRegistry == null) return null;

        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];
        return fragmentRegistry.GetPrefab(req.type, req.colorVariant);
    }

    void SetupCollectibleComponent(GameObject spawned, GameObject originalPrefab)
    {
        if (originalPrefab == coinPrefab || originalPrefab == starPrefab) return;

        var collectible = spawned.GetComponent<FragmentCollectible>();
        if (collectible == null)
            collectible = spawned.AddComponent<FragmentCollectible>();

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

    void SetupMover(GameObject obj, float speed)
    {
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

        var mover = obj.AddComponent<PlanetMover>();
        mover.SetSpeed(speed);
    }

    float GetCurrentSpeed()
    {
        if (difficultyManager != null)
            return difficultyManager.CurrentSpeed;
        return 3f;
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

    bool IsLaneClearForSpawn(int lane)
    {
        if (lane < 0 || lane >= laneCount) return false;

        float lastY = lastSpawnYInLane[lane];
        float distance = spawnY - lastY;

        return distance >= minSpawnDistance;
    }

    float GetLaneWorldX(int laneIndex)
    {
        float center = (laneCount - 1) / 2f;
        float centerX = lanesManager != null ? lanesManager.transform.position.x : 0f;
        float offset = lanesManager != null ? lanesManager.laneOffset : laneOffset;
        return centerX + (laneIndex - center) * offset;
    }

    CoinSpawnPattern SelectRandomPattern()
    {
        if (coinPatterns == null || coinPatterns.Count == 0) return null;

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

    float CalculatePatternDuration(CoinSpawnPattern pattern)
    {
        if (pattern == null || pattern.spawnPoints == null) return 1f;

        float spawnTime = pattern.spawnPoints.Count * itemSpawnDelay;

        float totalVerticalDistance = 0f;
        foreach (var point in pattern.spawnPoints)
        {
            totalVerticalDistance += point.y * coinVerticalSpacing;
        }

        float currentSpeed = GetCurrentSpeed();
        float clearTime = totalVerticalDistance / currentSpeed;

        return spawnTime + clearTime + 0.5f; // ✅ Reduced from +1f
    }

    void Log(string msg, bool alwaysShow = true)
    {
        if (enableDebugLogs || alwaysShow)
            Debug.Log($"[Spawner] {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[Spawner] ❌ {msg}");
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, spawnY, 0), new Vector3(10, spawnY, 0));

        for (int i = 0; i < laneCount; i++)
        {
            float x = GetLaneWorldX(i);
            Gizmos.color = Time.time >= laneBlockedUntil[i] ? Color.green : Color.red;
            Gizmos.DrawLine(new Vector3(x, spawnY - 2, 0), new Vector3(x, spawnY + 2, 0));
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 1, 0),
            $"Spawner: {totalSpawned}\nPlanets: {planetSpawned} | Stars: {starSpawned}/{maxStarsPerLevel}",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
        );
#endif
    }

    [ContextMenu("🔍 Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("========================================");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Total: {totalSpawned} | Planets: {planetSpawned}");
        Debug.Log($"Coins: {coinSpawned} | Fragments: {fragmentSpawned} | Stars: {starSpawned}/{maxStarsPerLevel}");
        Debug.Log($"Planet Interval: {planetInterval}s (FAST!)");
        Debug.Log("========================================");
    }
}