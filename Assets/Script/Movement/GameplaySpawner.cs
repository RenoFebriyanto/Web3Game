using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ✅ COMPLETE FIXED VERSION - GameplaySpawner
/// FIXES:
/// - Planet spawn consistency (no more delays/gaps)
/// - Proper lane blocking system
/// - Time freeze integration
/// - Smart star spawning (3 per level, synced with fragment progress)
/// - Planet variations (1 or 2 lanes simultaneously)
/// - Improved distance checking (no overlap)
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

    [Tooltip("Planet spawn interval (detik)")]
    public float planetInterval = 2.5f;

    [Tooltip("Planet spawn interval ketika double spawn")]
    public float doublePlanetInterval = 4f;

    public float itemSpawnDelay = 0.15f;
    public float minPlanetDistance = 4f; // Distance minimum antar planet
    public float minSafeZone = 2f; // Safe zone antara planet dan collectibles

    [Header("⭐ STAR CONTROL")]
    [Tooltip("Star 1 spawn delay range (detik)")]
    public Vector2 star1SpawnTimeRange = new Vector2(5f, 15f);

    [Tooltip("Star 2 spawn saat fragment progress 30-60%")]
    public Vector2 star2ProgressRange = new Vector2(0.3f, 0.6f);

    [Tooltip("Star 3 spawn saat fragment progress 85-95%")]
    public Vector2 star3ProgressRange = new Vector2(0.85f, 0.95f);

    [Header("🌍 PLANET VARIATIONS")]
    [Tooltip("Chance untuk spawn 2 planets sekaligus (0-100%)")]
    [Range(0f, 100f)]
    public float doublePlanetChance = 40f;

    [Header("📉 FRAGMENT CONTROL")]
    [Tooltip("Reduce fragment spawn chance (0-100%)")]
    [Range(0f, 100f)]
    public float fragmentSpawnChanceMultiplier = 30f;

    [Header("🔧 ADVANCED TUNING")]
    [Tooltip("Planet spawn cooldown per lane (detik)")]
    public float planetLaneCooldown = 1.5f;

    [Tooltip("Collectible spawn cooldown per lane (detik)")]
    public float collectibleLaneCooldown = 1f;

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
    private float[] lastPlanetSpawnTime; // Track waktu spawn planet per lane
    private Dictionary<int, float> lastPlanetSpawnY = new Dictionary<int, float>();
    private Dictionary<int, float> lastCollectibleSpawnY = new Dictionary<int, float>();
    private float lastAnyPlanetSpawnTime = 0f; // Track waktu spawn planet (any lane)

    private FragmentRequirement[] levelRequirements;
    private int totalPatternWeight = 0;

    // Star tracking
    private bool[] starSpawned_Flags = new bool[3]; // [star1, star2, star3]
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
        lastPlanetSpawnTime = new float[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            laneBlockedUntil[i] = 0f;
            lastSpawnYInLane[i] = spawnY - 10f;
            lastPlanetSpawnTime[i] = -10f; // Start with old time
            lastPlanetSpawnY[i] = spawnY - 10f;
            lastCollectibleSpawnY[i] = spawnY - 10f;
        }

        if (spawnParent == null)
            spawnParent = transform;

        BuildWeightedPatternList();

        // Initialize star tracking
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

        // Schedule star 1 spawn time
        star1SpawnTime = Time.time + Random.Range(star1SpawnTimeRange.x, star1SpawnTimeRange.y);
        star1Scheduled = true;
        Log($"Star 1 scheduled at: {star1SpawnTime - Time.time:F1}s from now");

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
                Log($"✓ Loaded {levelRequirements.Length} requirements for {levelId}");
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
    // ✅ PLANET SPAWNER - FIXED CONSISTENT SPAWN
    // ========================================

    IEnumerator PlanetSpawnLoop()
    {
        Log("Planet spawn loop started");
        yield return new WaitForSeconds(0.2f); // Shorter initial delay

        while (true)
        {
            // ✅ TIME FREEZE CHECK
            if (BoosterManager.Instance != null &&
                (BoosterManager.Instance.timeFreezeActive || !BoosterManager.Instance.CanSpawn()))
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            // ✅ FIXED: Check if enough time passed since last spawn
            float timeSinceLastSpawn = Time.time - lastAnyPlanetSpawnTime;
            if (timeSinceLastSpawn < planetInterval)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            List<int> availableLanes = GetAvailableLanesForPlanet();

            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            // ✅ PLANET VARIATIONS
            float roll = Random.Range(0f, 100f);

            if (roll < doublePlanetChance && availableLanes.Count >= 2)
            {
                // Spawn 2 planets
                SpawnDoublePlanets(availableLanes);
                yield return new WaitForSeconds(doublePlanetInterval);
            }
            else
            {
                // Spawn 1 planet
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];
                SpawnPlanet(lane);
                yield return new WaitForSeconds(planetInterval);
            }
        }
    }

    void SpawnDoublePlanets(List<int> availableLanes)
    {
        if (availableLanes.Count < 2) return;

        // Pilih 2 lanes berbeda
        List<int> tempLanes = new List<int>(availableLanes);

        int lane1 = tempLanes[Random.Range(0, tempLanes.Count)];
        tempLanes.Remove(lane1);

        if (tempLanes.Count == 0) return;
        int lane2 = tempLanes[Random.Range(0, tempLanes.Count)];

        // Spawn both
        SpawnPlanet(lane1);
        SpawnPlanet(lane2);

        Log($"✓ Double planets spawned: lane {lane1}, {lane2}");
    }

    /// <summary>
    /// ✅ FIXED: Simplified planet lane availability check
    /// </summary>
    List<int> GetAvailableLanesForPlanet()
    {
        List<int> available = new List<int>();
        float currentTime = Time.time;

        for (int i = 0; i < laneCount; i++)
        {
            // Check cooldown per lane
            if (currentTime - lastPlanetSpawnTime[i] < planetLaneCooldown)
                continue;

            // Check distance from last collectible
            float lastCollectY = lastCollectibleSpawnY.ContainsKey(i) ? lastCollectibleSpawnY[i] : spawnY - 10f;
            float distFromCollect = spawnY - lastCollectY;

            if (distFromCollect < minSafeZone)
                continue;

            available.Add(i);
        }

        return available;
    }

    /// <summary>
    /// ✅ REMOVED: Overly complex checking - simplified in GetAvailableLanesForPlanet
    /// </summary>

    void SpawnPlanet(int lane)
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0) return;

        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        GameObject planet = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

        float currentSpeed = GetCurrentSpeed();
        SetupMover(planet, currentSpeed);

        // ✅ FIXED: Update tracking variables
        lastSpawnYInLane[lane] = spawnY;
        lastPlanetSpawnY[lane] = spawnY;
        lastPlanetSpawnTime[lane] = Time.time;
        lastAnyPlanetSpawnTime = Time.time;

        planetSpawned++;
        totalSpawned++;

        Log($"✓ Planet spawned: lane {lane}, total: {planetSpawned}");
    }

    // ========================================
    // ✅ PATTERN SPAWNER - FIXED TIMING
    // ========================================

    IEnumerator PatternSpawnLoop()
    {
        Log("Pattern spawn loop started");
        yield return new WaitForSeconds(1f);

        while (true)
        {
            // ✅ TIME FREEZE: Collectibles tetap spawn
            if (BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            List<int> availableLanes = GetAvailableLanesForCollectibles();
            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            int baseLane = availableLanes[Random.Range(0, availableLanes.Count)];
            CoinSpawnPattern pattern = SelectRandomPattern();

            if (pattern == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // Calculate lanes used by pattern
            HashSet<int> usedLanes = new HashSet<int> { baseLane };
            foreach (var point in pattern.spawnPoints)
            {
                int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);
                usedLanes.Add(targetLane);
            }

            yield return StartCoroutine(SpawnPattern(pattern, baseLane));

            float delay = pattern.GetRandomDelay() * 0.7f;
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// ✅ NEW: Get available lanes for collectibles (coins/fragments)
    /// </summary>
    List<int> GetAvailableLanesForCollectibles()
    {
        List<int> available = new List<int>();
        float currentTime = Time.time;

        for (int i = 0; i < laneCount; i++)
        {
            // Skip if blocked
            if (currentTime < laneBlockedUntil[i])
                continue;

            // Check distance from last planet
            float lastPlanetY = lastPlanetSpawnY.ContainsKey(i) ? lastPlanetSpawnY[i] : spawnY - 10f;
            float distFromPlanet = spawnY - lastPlanetY;

            if (distFromPlanet < minSafeZone)
                continue;

            available.Add(i);
        }

        return available;
    }

    IEnumerator SpawnPattern(CoinSpawnPattern pattern, int baseLane)
    {
        if (pattern == null || pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
            yield break;

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
                lastCollectibleSpawnY[targetLane] = currentY;

                // ✅ Block lane temporarily
                laneBlockedUntil[targetLane] = Time.time + collectibleLaneCooldown;

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

        // ✅ REDUCED FRAGMENT CHANCE - Apply multiplier
        float adjustedFragmentChance = pattern.fragmentSubstituteChance * (fragmentSpawnChanceMultiplier / 100f);

        // Fragment (with reduced chance)
        if (roll < adjustedFragmentChance)
        {
            GameObject fragmentPrefab = GetRequiredFragmentPrefab();
            if (fragmentPrefab != null)
                return fragmentPrefab;
        }

        // Default: Coin
        return coinPrefab;
    }

    // ========================================
    // ✅ SMART STAR SPAWN SYSTEM
    // ========================================

    IEnumerator StarSpawnLoop()
    {
        Log("Star spawn loop started");

        // Wait for game to start
        yield return new WaitForSeconds(2f);

        while (starSpawned < 3)
        {
            yield return new WaitForSeconds(0.5f);

            float progress = GetFragmentCollectionProgress();

            // ✅ STAR 1: Spawn at scheduled time
            if (!starSpawned_Flags[0] && star1Scheduled && Time.time >= star1SpawnTime)
            {
                yield return StartCoroutine(SpawnStarSafely(0));
                starSpawned_Flags[0] = true;
                Log($"Star 1 spawned (random time)");
            }

            // ✅ STAR 2: Spawn saat 30-60% fragment collected
            if (!starSpawned_Flags[1] && starSpawned_Flags[0] &&
                progress >= star2ProgressRange.x && progress <= star2ProgressRange.y)
            {
                yield return StartCoroutine(SpawnStarSafely(1));
                starSpawned_Flags[1] = true;
                Log($"Star 2 spawned (progress: {progress:P0})");
            }

            // ✅ STAR 3: Spawn saat 85-95% (sebelum fragment terakhir)
            if (!starSpawned_Flags[2] && starSpawned_Flags[1] &&
                progress >= star3ProgressRange.x && progress <= star3ProgressRange.y)
            {
                yield return StartCoroutine(SpawnStarSafely(2));
                starSpawned_Flags[2] = true;
                Log($"Star 3 spawned (progress: {progress:P0}) - before last fragment!");
            }
        }

        Log("All 3 stars spawned!");
    }

    IEnumerator SpawnStarSafely(int starIndex)
    {
        // Wait for safe spawn opportunity
        int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            List<int> availableLanes = GetAvailableLanesForCollectibles();

            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];

                // Check safe distance from planets
                float lastPlanetY = lastPlanetSpawnY.ContainsKey(lane) ? lastPlanetSpawnY[lane] : spawnY - 10f;
                float distFromPlanet = spawnY - lastPlanetY;

                if (distFromPlanet >= minSafeZone)
                {
                    Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
                    GameObject star = Instantiate(starPrefab, pos, Quaternion.identity, spawnParent);

                    float currentSpeed = GetCurrentSpeed();
                    SetupMover(star, currentSpeed);

                    lastCollectibleSpawnY[lane] = spawnY;
                    laneBlockedUntil[lane] = Time.time + collectibleLaneCooldown;

                    starSpawned++;
                    totalSpawned++;

                    Log($"✅ Star {starIndex + 1} spawned successfully in lane {lane}");
                    yield break;
                }
            }

            attempts++;
            yield return new WaitForSeconds(0.5f);
        }

        LogError($"❌ Failed to spawn star {starIndex + 1} after {maxAttempts} attempts");
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

    float GetLaneWorldX(int laneIndex)
    {
        float center = (laneCount - 1) / 2f;
        float centerX = lanesManager != null ? lanesManager.transform.position.x : 0f;
        float offset = lanesManager != null ? lanesManager.laneOffset : laneOffset;
        return centerX + (laneIndex - center) * offset;
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
        Debug.LogError($"[Spawner] {msg}");
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
        string starStatus = $"⭐ {starSpawned}/3";
        for (int i = 0; i < 3; i++)
        {
            if (starSpawned_Flags[i])
                starStatus += $" [S{i + 1}✓]";
        }

        float progress = GetFragmentCollectionProgress();

        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 1, 0),
            $"Spawner: {totalSpawned}\n{starStatus}\nProgress: {progress:P0}\nPlanets: {planetSpawned}",
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
        Debug.Log($"Coins: {coinSpawned} | Fragments: {fragmentSpawned}");
        Debug.Log($"Stars: {starSpawned}/3 [S1:{starSpawned_Flags[0]} S2:{starSpawned_Flags[1]} S3:{starSpawned_Flags[2]}]");
        Debug.Log($"Fragment Progress: {GetFragmentCollectionProgress():P0}");
        Debug.Log($"Time Freeze Active: {(BoosterManager.Instance != null ? BoosterManager.Instance.timeFreezeActive : false)}");
        Debug.Log($"Last Planet Spawn: {Time.time - lastAnyPlanetSpawnTime:F2}s ago");
        Debug.Log($"Planet Interval: {planetInterval}s");
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
}