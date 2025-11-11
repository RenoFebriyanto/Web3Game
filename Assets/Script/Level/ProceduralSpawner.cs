using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ADVANCED PROCEDURAL SPAWNER - Subway Surfer style
/// Features:
/// - Lane blocking detection (always 1+ lane free)
/// - Continuous spawn (no gaps/delays)
/// - Smart pattern selection (no overlap)
/// - Star system (3 stars based on fragment progress)
/// - Booster integration (TimeFreeze, SpeedBoost)
/// - Separate planet and coin patterns
/// 
/// Letakkan di: Assets/Script/Movement/ProceduralSpawner.cs
/// </summary>
public class ProceduralSpawner : MonoBehaviour
{
    #region REFERENCES
    [Header("🔧 REQUIRED REFERENCES")]
    public LanesManager lanesManager;
    public DifficultyManager difficultyManager;

    [Header("📦 PREFABS")]
    public GameObject[] planetPrefabs;
    public GameObject coinPrefab;
    public GameObject starPrefab;

    [Header("📚 DATA")]
    public FragmentPrefabRegistry fragmentRegistry;
    public LevelDatabase levelDatabase;

    [Header("🎨 PATTERNS")]
    public List<PlanetSpawnPattern> planetPatterns = new List<PlanetSpawnPattern>();
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();

    [Header("⚙️ SETTINGS")]
    public Transform spawnParent;
    public float spawnY = 10f;
    public float planetVerticalSpacing = 2.5f;
    public float coinVerticalSpacing = 2f;
    public float minSafeDistance = 3f; // Minimum jarak antar wave
    #endregion

    #region SPAWN TIMING
    [Header("⏱️ SPAWN TIMING")]
    [Tooltip("Base planet spawn interval (detik)")]
    public float basePlanetInterval = 1.5f;

    [Tooltip("Base coin pattern interval (detik)")]
    public float baseCoinInterval = 2.0f;

    [Tooltip("SpeedBoost multiplier untuk spawn rate")]
    public float speedBoostSpawnMultiplier = 0.7f; // Spawn lebih cepat tapi jaga jarak
    #endregion

    #region STAR CONTROL
    [Header("⭐ STAR SYSTEM (3 Stars Per Level)")]
    [Tooltip("Star 1: Random time range")]
    public Vector2 star1TimeRange = new Vector2(8f, 15f);

    [Tooltip("Star 2: Fragment progress range (30-60%)")]
    public Vector2 star2ProgressRange = new Vector2(0.3f, 0.6f);

    [Tooltip("Star 3: Fragment progress range (85-95%)")]
    public Vector2 star3ProgressRange = new Vector2(0.85f, 0.95f);
    #endregion

    #region DEBUG
    [Header("🎯 DEBUG")]
    public bool enableDebugLogs = false;
    public bool showGizmos = true;
    public bool enableDetailedLogs = false;

    [Header("📊 RUNTIME STATUS")]
    [SerializeField] private int totalSpawned = 0;
    [SerializeField] private int planetSpawned = 0;
    [SerializeField] private int coinSpawned = 0;
    [SerializeField] private int fragmentSpawned = 0;
    [SerializeField] private int starSpawned = 0;
    #endregion

    #region PRIVATE VARS
    private int laneCount = 3;
    private float laneOffset = 2.5f;

    // Lane tracking
    private class LaneState
    {
        public bool isBlocked;
        public float blockedUntilY;
        public float lastSpawnY;
        public string blockReason;
    }
    private LaneState[] lanes;

    // Pattern weights
    private int totalPlanetWeight = 0;
    private int totalCoinWeight = 0;

    // Level data
    private FragmentRequirement[] levelRequirements;

    // Star tracking
    private bool[] starSpawned_Flags = new bool[3];
    private float star1ScheduledTime = 0f;
    private bool isInitialized = false;

    // Spawn queues
    private Queue<System.Action> planetSpawnQueue = new Queue<System.Action>();
    private Queue<System.Action> coinSpawnQueue = new Queue<System.Action>();

    // Next spawn times
    private float nextPlanetSpawnTime = 0f;
    private float nextCoinSpawnTime = 0f;
    #endregion

    #region INITIALIZATION
    void Awake()
    {
        SetupReferences();
        InitializeLanes();
        BuildPatternWeights();
    }

    void SetupReferences()
    {
        if (lanesManager == null)
            lanesManager = LanesManager.EnsureExists();

        if (difficultyManager == null)
            difficultyManager = DifficultyManager.EnsureExists();

        if (lanesManager != null)
        {
            laneCount = lanesManager.laneCount;
            laneOffset = lanesManager.laneOffset;
        }

        if (spawnParent == null)
            spawnParent = transform;
    }

    void InitializeLanes()
    {
        lanes = new LaneState[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = new LaneState
            {
                isBlocked = false,
                blockedUntilY = spawnY - 20f,
                lastSpawnY = spawnY - 10f,
                blockReason = ""
            };
        }
    }

    void BuildPatternWeights()
    {
        totalPlanetWeight = 0;
        totalCoinWeight = 0;

        if (planetPatterns != null)
        {
            foreach (var p in planetPatterns)
            {
                if (p != null) totalPlanetWeight += p.selectionWeight;
            }
        }

        if (coinPatterns != null)
        {
            foreach (var c in coinPatterns)
            {
                if (c != null) totalCoinWeight += c.selectionWeight;
            }
        }
    }

    void Start()
    {
        if (!ValidateSetup())
        {
            LogError("❌ SETUP VALIDATION FAILED!");
            enabled = false;
            return;
        }

        LoadLevelRequirements();
        ScheduleStar1();

        // Initialize spawn times
        nextPlanetSpawnTime = Time.time + 0.5f;
        nextCoinSpawnTime = Time.time + 1.0f;

        StartCoroutine(MasterSpawnLoop());
        StartCoroutine(StarSpawnLoop());

        isInitialized = true;
        Log("✅ PROCEDURAL SPAWNER INITIALIZED");
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (planetPrefabs == null || planetPrefabs.Length == 0)
        {
            LogError("planetPrefabs empty!");
            valid = false;
        }

        if (coinPrefab == null)
        {
            LogError("coinPrefab null!");
            valid = false;
        }

        if (starPrefab == null)
        {
            LogError("starPrefab null!");
            valid = false;
        }

        if (planetPatterns == null || planetPatterns.Count == 0)
        {
            LogError("planetPatterns empty!");
            valid = false;
        }

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogError("coinPatterns empty!");
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
                Log($"✓ Loaded {levelRequirements.Length} fragment requirements");
            }
        }
    }

    void ScheduleStar1()
    {
        star1ScheduledTime = Time.time + Random.Range(star1TimeRange.x, star1TimeRange.y);
        Log($"Star 1 scheduled at T+{star1ScheduledTime - Time.time:F1}s");
    }
    #endregion

    #region MASTER SPAWN LOOP
    /// <summary>
    /// Master loop - manages continuous spawning
    /// </summary>
    IEnumerator MasterSpawnLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            float currentTime = Time.time;
            UpdateLaneStates();

            // ✅ PLANET SPAWN CHECK
            if (currentTime >= nextPlanetSpawnTime)
            {
                bool canSpawnPlanet = CanSpawnPlanets();
                if (canSpawnPlanet)
                {
                    TrySpawnPlanetPattern();
                }
                else if (enableDetailedLogs)
                {
                    Log("⏸️ Planet spawn paused (TimeFreeze active)");
                }
            }

            // ✅ COIN SPAWN CHECK (always active, even during TimeFreeze)
            if (currentTime >= nextCoinSpawnTime)
            {
                TrySpawnCoinPattern();
            }

            yield return null; // Run every frame for precise timing
        }
    }

    void UpdateLaneStates()
    {
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes[i].blockedUntilY < spawnY - minSafeDistance)
            {
                lanes[i].isBlocked = false;
                lanes[i].blockReason = "";
            }
        }
    }

    bool CanSpawnPlanets()
    {
        // TimeFreeze blocks planet spawning
        if (BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)
        {
            return false;
        }
        return true;
    }
    #endregion

    #region PLANET SPAWNING
    void TrySpawnPlanetPattern()
    {
        // Get available lanes (at least minFreeLanes must remain)
        List<int> availableLanes = GetAvailableLanesForPlanet();

        if (availableLanes.Count == 0)
        {
            if (enableDetailedLogs)
                Log("⚠️ No available lanes for planet pattern");

            // Retry sooner
            float retrySpeedMod = GetSpeedModifier();
            nextPlanetSpawnTime = Time.time + (basePlanetInterval * 0.5f * retrySpeedMod);
            return;
        }

        // Select pattern
        PlanetSpawnPattern pattern = SelectPlanetPattern(availableLanes);
        if (pattern == null)
        {
            LogWarning("No valid planet pattern found!");
            nextPlanetSpawnTime = Time.time + basePlanetInterval;
            return;
        }

        // Choose base lane
        int baseLane = SelectBaseLaneForPattern(pattern, availableLanes);
        if (baseLane == -1)
        {
            if (enableDetailedLogs)
                Log($"Cannot spawn pattern {pattern.name} - no valid base lane");

            nextPlanetSpawnTime = Time.time + (basePlanetInterval * 0.3f);
            return;
        }

        // Spawn pattern
        StartCoroutine(SpawnPlanetPattern(pattern, baseLane));

        // Schedule next planet spawn
        float currentSpeedMod = GetSpeedModifier();
        float baseInterval = basePlanetInterval * currentSpeedMod;
        float patternDelay = pattern.GetRandomDelay();
        nextPlanetSpawnTime = Time.time + baseInterval + patternDelay;

        if (enableDetailedLogs)
            Log($"🌍 Spawning planet pattern: {pattern.name}, next in {baseInterval + patternDelay:F2}s");
    }

    IEnumerator SpawnPlanetPattern(PlanetSpawnPattern pattern, int baseLane)
    {
        if (pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
            yield break;

        float currentY = spawnY;
        HashSet<int> usedLanes = pattern.GetBlockedLanes(baseLane, laneCount);

        // Block lanes
        foreach (int lane in usedLanes)
        {
            lanes[lane].isBlocked = true;
            lanes[lane].blockReason = $"Planet:{pattern.name}";
        }

        float currentSpeed = GetCurrentSpeed();

        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];
            int targetLane = baseLane + Mathf.RoundToInt(point.x);
            targetLane = Mathf.Clamp(targetLane, 0, laneCount - 1);

            currentY -= point.y * planetVerticalSpacing;

            // Spawn planet
            GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
            Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
            GameObject planet = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

            SetupMover(planet, currentSpeed);

            lanes[targetLane].lastSpawnY = currentY;
            lanes[targetLane].blockedUntilY = currentY;

            planetSpawned++;
            totalSpawned++;

            yield return new WaitForSeconds(0.1f); // Small delay between items in pattern
        }

        // Update block status
        float patternLength = pattern.GetTotalVerticalDistance() * planetVerticalSpacing;
        float blockY = currentY - patternLength;

        foreach (int lane in usedLanes)
        {
            lanes[lane].blockedUntilY = blockY;
        }
    }

    List<int> GetAvailableLanesForPlanet()
    {
        List<int> available = new List<int>();

        for (int i = 0; i < laneCount; i++)
        {
            if (!lanes[i].isBlocked)
            {
                available.Add(i);
            }
        }

        return available;
    }

    PlanetSpawnPattern SelectPlanetPattern(List<int> availableLanes)
    {
        if (planetPatterns == null || planetPatterns.Count == 0) return null;

        // Filter valid patterns
        List<PlanetSpawnPattern> validPatterns = new List<PlanetSpawnPattern>();

        foreach (var pattern in planetPatterns)
        {
            if (pattern == null) continue;

            // Check if pattern can fit in available lanes
            bool canFit = false;
            foreach (int baseLane in availableLanes)
            {
                if (pattern.IsValidForBaseLane(baseLane, laneCount))
                {
                    canFit = true;
                    break;
                }
            }

            if (canFit)
                validPatterns.Add(pattern);
        }

        if (validPatterns.Count == 0) return null;

        // Weighted random selection
        int totalWeight = 0;
        foreach (var p in validPatterns)
        {
            totalWeight += p.selectionWeight;
        }

        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var p in validPatterns)
        {
            cumulative += p.selectionWeight;
            if (rand < cumulative)
                return p;
        }

        return validPatterns[0];
    }

    int SelectBaseLaneForPattern(PlanetSpawnPattern pattern, List<int> availableLanes)
    {
        List<int> validBaseLanes = new List<int>();

        foreach (int lane in availableLanes)
        {
            if (pattern.IsValidForBaseLane(lane, laneCount))
            {
                // Additional check: ensure lanes used by pattern are not blocked
                var blocked = pattern.GetBlockedLanes(lane, laneCount);
                bool allClear = true;

                foreach (int b in blocked)
                {
                    if (lanes[b].isBlocked)
                    {
                        allClear = false;
                        break;
                    }
                }

                if (allClear)
                    validBaseLanes.Add(lane);
            }
        }

        if (validBaseLanes.Count == 0) return -1;
        return validBaseLanes[Random.Range(0, validBaseLanes.Count)];
    }
    #endregion

    #region COIN SPAWNING
    void TrySpawnCoinPattern()
    {
        // Get free lanes (lanes not blocked by planets)
        List<int> freeLanes = GetFreeLanesForCollectibles();

        if (freeLanes.Count == 0)
        {
            if (enableDetailedLogs)
                Log("⚠️ No free lanes for coin pattern");

            nextCoinSpawnTime = Time.time + (baseCoinInterval * 0.3f);
            return;
        }

        // Select pattern
        CoinSpawnPattern pattern = SelectCoinPattern();
        if (pattern == null)
        {
            LogWarning("No coin pattern found!");
            nextCoinSpawnTime = Time.time + baseCoinInterval;
            return;
        }

        // Choose base lane from free lanes
        int baseLane = freeLanes[Random.Range(0, freeLanes.Count)];

        // Spawn pattern
        StartCoroutine(SpawnCoinPattern(pattern, baseLane));

        // Schedule next coin spawn
        float speedMod = GetSpeedModifier();
        float delay = pattern.GetRandomDelay() * speedMod;
        nextCoinSpawnTime = Time.time + baseCoinInterval + delay;

        if (enableDetailedLogs)
            Log($"💰 Spawning coin pattern: {pattern.patternName}, next in {baseCoinInterval + delay:F2}s");
    }

    IEnumerator SpawnCoinPattern(CoinSpawnPattern pattern, int baseLane)
    {
        if (pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
            yield break;

        float currentY = spawnY;
        float currentSpeed = GetCurrentSpeed();

        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];
            int targetLane = baseLane + Mathf.RoundToInt(point.x);
            targetLane = Mathf.Clamp(targetLane, 0, laneCount - 1);

            currentY -= point.y * coinVerticalSpacing;

            // Decide what to spawn
            GameObject prefab = DecideCollectiblePrefab(pattern);

            if (prefab != null)
            {
                Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
                GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

                SetupMover(spawned, currentSpeed);
                SetupCollectibleComponent(spawned, prefab);

                // Update stats
                if (prefab == coinPrefab) coinSpawned++;
                else if (prefab != coinPrefab && prefab != starPrefab) fragmentSpawned++;

                totalSpawned++;
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    List<int> GetFreeLanesForCollectibles()
    {
        List<int> free = new List<int>();

        for (int i = 0; i < laneCount; i++)
        {
            // Lane is free if not blocked by planet pattern
            if (!lanes[i].isBlocked || lanes[i].blockReason == "")
            {
                free.Add(i);
            }
        }

        return free;
    }

    CoinSpawnPattern SelectCoinPattern()
    {
        if (coinPatterns == null || coinPatterns.Count == 0) return null;

        int rand = Random.Range(0, totalCoinWeight);
        int cumulative = 0;

        foreach (var p in coinPatterns)
        {
            if (p == null) continue;
            cumulative += p.selectionWeight;
            if (rand < cumulative)
                return p;
        }

        return coinPatterns[0];
    }

    GameObject DecideCollectiblePrefab(CoinSpawnPattern pattern)
    {
        float roll = Random.Range(0f, 100f);

        // Fragment
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
    #endregion

    #region STAR SPAWNING
    IEnumerator StarSpawnLoop()
    {
        yield return new WaitForSeconds(2f);

        while (starSpawned < 3)
        {
            yield return new WaitForSeconds(0.5f);

            float progress = GetFragmentProgress();

            // Star 1: Random time
            if (!starSpawned_Flags[0] && Time.time >= star1ScheduledTime)
            {
                yield return StartCoroutine(TrySpawnStar(0));
            }

            // Star 2: Fragment progress 30-60%
            if (!starSpawned_Flags[1] && starSpawned_Flags[0] &&
                progress >= star2ProgressRange.x && progress <= star2ProgressRange.y)
            {
                yield return StartCoroutine(TrySpawnStar(1));
            }

            // Star 3: Fragment progress 85-95%
            if (!starSpawned_Flags[2] && starSpawned_Flags[1] &&
                progress >= star3ProgressRange.x && progress <= star3ProgressRange.y)
            {
                yield return StartCoroutine(TrySpawnStar(2));
            }
        }

        Log("✅ All 3 stars spawned!");
    }

    IEnumerator TrySpawnStar(int starIndex)
    {
        int maxAttempts = 20;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            List<int> freeLanes = GetFreeLanesForCollectibles();

            if (freeLanes.Count > 0)
            {
                int lane = freeLanes[Random.Range(0, freeLanes.Count)];
                Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
                GameObject star = Instantiate(starPrefab, pos, Quaternion.identity, spawnParent);

                float speed = GetCurrentSpeed();
                SetupMover(star, speed);

                starSpawned_Flags[starIndex] = true;
                starSpawned++;
                totalSpawned++;

                Log($"⭐ Star {starIndex + 1} spawned in lane {lane} (progress: {GetFragmentProgress():P0})");
                yield break;
            }

            attempts++;
            yield return new WaitForSeconds(0.3f);
        }

        LogWarning($"Failed to spawn star {starIndex + 1} after {maxAttempts} attempts");
    }

    float GetFragmentProgress()
    {
        if (LevelGameSession.Instance == null || levelRequirements == null || levelRequirements.Length == 0)
            return 0f;

        int totalRequired = 0;
        int totalCollected = 0;

        foreach (var req in levelRequirements)
        {
            totalRequired += req.count;
            int remaining = LevelGameSession.Instance.GetRemaining(req.type, req.colorVariant);
            totalCollected += (req.count - remaining);
        }

        if (totalRequired == 0) return 0f;
        return (float)totalCollected / totalRequired;
    }
    #endregion

    #region UTILITY
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

    float GetSpeedModifier()
    {
        // SpeedBoost affects spawn rate
        if (BoosterManager.Instance != null && BoosterManager.Instance.speedBoostActive)
        {
            return speedBoostSpawnMultiplier;
        }
        return 1f;
    }

    float GetLaneWorldX(int lane)
    {
        if (lanesManager != null)
            return lanesManager.LaneToWorldX(lane);

        float center = (laneCount - 1) / 2f;
        return (lane - center) * laneOffset;
    }

    void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[ProceduralSpawner] {msg}");
    }

    void LogWarning(string msg)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[ProceduralSpawner] ⚠️ {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[ProceduralSpawner] ❌ {msg}");
    }
    #endregion

    #region DEBUG & GIZMOS
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        // Draw spawn line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-10, spawnY, 0), new Vector3(10, spawnY, 0));

        // Draw lane states
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes == null || i >= lanes.Length) continue;

            float x = GetLaneWorldX(i);
            Gizmos.color = lanes[i].isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(x, spawnY - 2, 0), new Vector3(x, spawnY + 2, 0));

            // Draw blocked until Y
            if (lanes[i].isBlocked)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                float y = lanes[i].blockedUntilY;
                Gizmos.DrawCube(new Vector3(x, y, 0), new Vector3(0.5f, 0.5f, 0.5f));
            }
        }

#if UNITY_EDITOR
        // Draw status text
        float progress = GetFragmentProgress();
        string status = $"Total: {totalSpawned} | Planets: {planetSpawned} | Coins: {coinSpawned}\n";
        status += $"Fragments: {fragmentSpawned} | Stars: {starSpawned}/3\n";
        status += $"Progress: {progress:P0}";

        if (BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)
        {
            status += "\n⏸️ TIME FREEZE ACTIVE";
        }

        if (BoosterManager.Instance != null && BoosterManager.Instance.speedBoostActive)
        {
            status += "\n⚡ SPEED BOOST ACTIVE";
        }

        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 1.5f, 0),
            status,
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
        );
#endif
    }

    [ContextMenu("🔍 Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("========== PROCEDURAL SPAWNER STATUS ==========");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Total Spawned: {totalSpawned}");
        Debug.Log($"Planets: {planetSpawned} | Coins: {coinSpawned}");
        Debug.Log($"Fragments: {fragmentSpawned} | Stars: {starSpawned}/3");
        Debug.Log($"Fragment Progress: {GetFragmentProgress():P0}");
        Debug.Log($"Current Speed: {GetCurrentSpeed():F2}");
        Debug.Log($"Speed Modifier: {GetSpeedModifier():F2}");
        Debug.Log("\n--- Lane States ---");
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes != null && i < lanes.Length)
            {
                string status = lanes[i].isBlocked ? "BLOCKED" : "FREE";
                Debug.Log($"Lane {i}: {status} | Reason: {lanes[i].blockReason} | BlockedY: {lanes[i].blockedUntilY:F1}");
            }
        }
        Debug.Log("===============================================");
    }

    [ContextMenu("⭐ Debug: Force Spawn Star")]
    void Debug_ForceSpawnStar()
    {
        if (starSpawned >= 3)
        {
            Debug.LogWarning("All 3 stars already spawned!");
            return;
        }

        StartCoroutine(TrySpawnStar(starSpawned));
    }

    [ContextMenu("🌍 Debug: Force Spawn Planet Pattern")]
    void Debug_ForceSpawnPlanet()
    {
        TrySpawnPlanetPattern();
    }

    [ContextMenu("💰 Debug: Force Spawn Coin Pattern")]
    void Debug_ForceSpawnCoin()
    {
        TrySpawnCoinPattern();
    }
    #endregion
}