using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MERGED & FIXED VERSION - Best of both scripts
/// Planet spawning: From Script 1 (fast, no delay, working perfectly)
/// Star system: From Script 2 (3 stars per level, synced with fragment progress)
/// Planet variations: From Script 2 (double planet spawns)
/// Time freeze: From Script 2 (planets stop during time freeze)
/// Coin/Fragment patterns: Fixed to spawn continuously
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

    [Tooltip("✅ Planet spawn interval (1.2s for continuous spawn)")]
    public float planetInterval = 1.2f;

    public float itemSpawnDelay = 0.15f;
    public float minSpawnDistance = 2f;

    [Header("⭐ STAR CONTROL (3 Stars Per Level)")]
    [Tooltip("Star 1 spawn delay range (detik)")]
    public Vector2 star1SpawnTimeRange = new Vector2(5f, 15f);

    [Tooltip("Star 2 spawn saat fragment progress 30-60%")]
    public Vector2 star2ProgressRange = new Vector2(0.3f, 0.6f);

    [Tooltip("Star 3 spawn saat fragment progress 85-95% (sebelum fragment terakhir)")]
    public Vector2 star3ProgressRange = new Vector2(0.85f, 0.95f);

    [Header("🌍 PLANET VARIATIONS")]
    [Tooltip("Chance untuk spawn 2 planets sekaligus (0-100%)")]
    [Range(0f, 100f)]
    public float doublePlanetChance = 40f;

    [Header("🎯 DEBUG")]
    public bool enableDebugLogs = false;
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

    // ✅ Star tracking (3 stars per level)
    private bool[] starSpawned_Flags = new bool[3];
    private float star1SpawnTime = 0f;
    private bool star1Scheduled = false;

    void Awake()
    {
        Log("=== SPAWNER AWAKE ===");

        if (lanesManager == null)
            lanesManager = FindFirstObjectByType<LanesManager>();

        if (difficultyManager == null)
            difficultyManager = FindFirstObjectByType<DifficultyManager>();

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
            spawnParent = transform;

        BuildWeightedPatternList();

        // ✅ Initialize star tracking
        for (int i = 0; i < 3; i++)
            starSpawned_Flags[i] = false;
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

        // ✅ Schedule star 1 spawn time
        star1SpawnTime = Time.time + Random.Range(star1SpawnTimeRange.x, star1SpawnTimeRange.y);
        star1Scheduled = true;
        Log($"Star 1 scheduled at: {star1SpawnTime - Time.time:F1}s from now");

        // ✅ Start all spawn loops
        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(PatternSpawnLoop());
        StartCoroutine(StarSpawnLoop());

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

        if (starPrefab == null)
        {
            LogError("❌ starPrefab NULL!");
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
    // ✅ PLANET SPAWNER - FROM SCRIPT 1 (WORKING PERFECTLY)
    // With additions: Time Freeze & Planet Variations
    // ========================================

    IEnumerator PlanetSpawnLoop()
    {
        Log("Planet spawn loop started");
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            yield return new WaitForSeconds(planetInterval); // ✅ 1.2s (fast, no delay)

            // ✅ TIME FREEZE CHECK - Skip planet spawn during time freeze
            if (BoosterManager.Instance != null && !BoosterManager.Instance.CanSpawn())
            {
                continue;
            }

            List<int> availableLanes = GetAvailableLanes();

            if (availableLanes.Count > 0)
            {
                // ✅ PLANET VARIATIONS - Sometimes spawn 2 planets
                float roll = Random.Range(0f, 100f);

                if (roll < doublePlanetChance && availableLanes.Count >= 2)
                {
                    // Spawn 2 planets di 2 lanes berbeda
                    SpawnDoublePlanets(availableLanes);
                }
                else
                {
                    // Spawn 1 planet (original behavior from Script 1)
                    int lane = availableLanes[Random.Range(0, availableLanes.Count)];
                    SpawnPlanet(lane);
                }
            }
        }
    }

    void SpawnDoublePlanets(List<int> availableLanes)
    {
        if (availableLanes.Count < 2) return;

        // Pilih 2 lanes berbeda secara random
        List<int> tempLanes = new List<int>(availableLanes);

        // Lane 1
        int lane1 = tempLanes[Random.Range(0, tempLanes.Count)];
        tempLanes.Remove(lane1);

        // Lane 2 (different from lane 1)
        if (tempLanes.Count == 0) return;
        int lane2 = tempLanes[Random.Range(0, tempLanes.Count)];

        // Spawn both planets
        SpawnPlanet(lane1);
        SpawnPlanet(lane2);

        Log($"Spawned double planets in lanes: {lane1}, {lane2}");
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
        laneBlockedUntil[lane] = Time.time + 1.5f; // ✅ Same as Script 1

        planetSpawned++;
        totalSpawned++;
    }

    // ========================================
    // ✅ PATTERN SPAWNER - FROM SCRIPT 1 (FIXED TO SPAWN CONTINUOUSLY)
    // ========================================

    IEnumerator PatternSpawnLoop()
    {
        Log("Pattern spawn loop started");
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            // ✅ IMPORTANT: Don't check time freeze for collectibles
            // Only check BoosterManager exists
            if (BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)
            {
                // During time freeze, collectibles still spawn but planets don't
                // This is correct behavior
            }

            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            int baseLane = availableLanes[Random.Range(0, availableLanes.Count)];
            CoinSpawnPattern pattern = SelectRandomPattern();

            if (pattern == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (!IsLaneClearForSpawn(baseLane))
            {
                yield return new WaitForSeconds(0.3f);
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
            float blockUntil = Time.time + patternDuration + 0.5f;

            foreach (int lane in usedLanes)
            {
                laneBlockedUntil[lane] = blockUntil;
            }

            yield return StartCoroutine(SpawnPattern(pattern, baseLane));

            float delay = pattern.GetRandomDelay() * 0.7f;
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

        // ✅ Star control - DISABLED in pattern (handled by StarSpawnLoop)
        // Stars now spawn via dedicated system, NOT random in patterns

        // Fragment
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

    // ========================================
    // ✅ SMART STAR SPAWN SYSTEM - FROM SCRIPT 2
    // 3 Stars per level, synced with fragment progress
    // ========================================

    IEnumerator StarSpawnLoop()
    {
        Log("Star spawn loop started");
        yield return new WaitForSeconds(2f);

        while (starSpawned < 3)
        {
            yield return new WaitForSeconds(0.5f);

            float progress = GetFragmentCollectionProgress();

            // ✅ STAR 1: Spawn at scheduled random time
            if (!starSpawned_Flags[0] && star1Scheduled && Time.time >= star1SpawnTime)
            {
                yield return StartCoroutine(SpawnStarSafely(0));
                starSpawned_Flags[0] = true;
                Log($"⭐ Star 1 spawned (random time)");
            }

            // ✅ STAR 2: Spawn saat 30-60% fragment collected
            if (!starSpawned_Flags[1] && starSpawned_Flags[0] &&
                progress >= star2ProgressRange.x && progress <= star2ProgressRange.y)
            {
                yield return StartCoroutine(SpawnStarSafely(1));
                starSpawned_Flags[1] = true;
                Log($"⭐ Star 2 spawned (progress: {progress:P0})");
            }

            // ✅ STAR 3: Spawn saat 85-95% (sebelum fragment terakhir)
            if (!starSpawned_Flags[2] && starSpawned_Flags[1] &&
                progress >= star3ProgressRange.x && progress <= star3ProgressRange.y)
            {
                yield return StartCoroutine(SpawnStarSafely(2));
                starSpawned_Flags[2] = true;
                Log($"⭐ Star 3 spawned (progress: {progress:P0}) - before last fragment!");
            }
        }

        Log("✓ All 3 stars spawned!");
    }

    IEnumerator SpawnStarSafely(int starIndex)
    {
        int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            List<int> availableLanes = GetAvailableLanes();

            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];

                if (IsLaneClearForSpawn(lane))
                {
                    Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
                    GameObject star = Instantiate(starPrefab, pos, Quaternion.identity, spawnParent);

                    float currentSpeed = GetCurrentSpeed();
                    SetupMover(star, currentSpeed);

                    lastSpawnYInLane[lane] = spawnY;
                    laneBlockedUntil[lane] = Time.time + 2f;

                    starSpawned++;
                    totalSpawned++;

                    Log($"✅ Star {starIndex + 1} spawned in lane {lane}");
                    yield break;
                }
            }

            attempts++;
            yield return new WaitForSeconds(0.5f);
        }

        LogError($"Failed to spawn star {starIndex + 1} after {maxAttempts} attempts");
    }

    float GetFragmentCollectionProgress()
    {
        if (LevelGameSession.Instance == null || levelRequirements == null || levelRequirements.Length == 0)
            return 0f;

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

        return spawnTime + clearTime + 0.5f;
    }

    void Log(string msg)
    {
        if (enableDebugLogs)
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
        float progress = GetFragmentCollectionProgress();
        string starStatus = $"⭐ {starSpawned}/3";
        for (int i = 0; i < 3; i++)
        {
            if (starSpawned_Flags[i])
                starStatus += $" [S{i + 1}✓]";
        }

        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 1, 0),
            $"Spawner: {totalSpawned}\n{starStatus}\nFragment Progress: {progress:P0}\nPlanets: {planetSpawned}",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
        );
#endif
    }

    [ContextMenu("🔍 Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("========================================");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Total Spawned: {totalSpawned}");
        Debug.Log($"Planets: {planetSpawned}");
        Debug.Log($"Coins: {coinSpawned}");
        Debug.Log($"Fragments: {fragmentSpawned}");
        Debug.Log($"Stars: {starSpawned}/3 [S1:{starSpawned_Flags[0]} S2:{starSpawned_Flags[1]} S3:{starSpawned_Flags[2]}]");
        Debug.Log($"Fragment Progress: {GetFragmentCollectionProgress():P0}");
        Debug.Log($"Planet Interval: {planetInterval}s");
        Debug.Log($"Time Freeze Active: {(BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)}");
        Debug.Log("========================================");
    }

    [ContextMenu("🌟 Debug: Force Spawn Star")]
    void Debug_ForceSpawnStar()
    {
        if (starSpawned >= 3)
        {
            Debug.LogWarning("All 3 stars already spawned!");
            return;
        }

        StartCoroutine(SpawnStarSafely(starSpawned));
    }

    [ContextMenu("🌍 Debug: Force Spawn Double Planet")]
    void Debug_ForceDoublePlanet()
    {
        List<int> availableLanes = GetAvailableLanes();
        if (availableLanes.Count >= 2)
        {
            SpawnDoublePlanets(availableLanes);
            Debug.Log("✓ Double planet spawned");
        }
        else
        {
            Debug.LogWarning("Not enough available lanes for double planet");
        }
    }
}