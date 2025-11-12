using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ✅ FIXED PROCEDURAL SPAWNER - Simplified untuk prefab dengan pre-positioned children
/// 
/// Key Features:
/// - Obstacle spawn di X=0 (prefab handle child positioning)
/// - Coin spawn berdasarkan free lanes
/// - Separate spawner loops (obstacle & coin independent)
/// - TimeFreeze support (stop obstacles, continue coins)
/// - SpeedBoost support (consistent spacing)
/// - Star system (3 stars per level)
/// 
/// REPLACE: Assets/Script/Level/ProceduralSpawner.cs
/// </summary>
public class ProceduralSpawner : MonoBehaviour
{
    #region REFERENCES
    [Header("🔧 REQUIRED REFERENCES")]
    public LanesManager lanesManager;
    public DifficultyManager difficultyManager;

    [Header("📦 COLLECTIBLE PREFABS")]
    public GameObject coinPrefab;
    public GameObject starPrefab;

    [Header("📚 DATA")]
    public FragmentPrefabRegistry fragmentRegistry;
    public LevelDatabase levelDatabase;

    [Header("🎨 PATTERN CONFIGS")]
    [Tooltip("List of obstacle configs (dengan prefab)")]
    public List<ObstacleConfig> obstacleConfigs = new List<ObstacleConfig>();

    [Tooltip("List of coin patterns")]
    public List<CoinPattern> coinPatterns = new List<CoinPattern>();

    [Header("⚙️ SPAWN SETTINGS")]
    public Transform spawnParent;
    public float spawnY = 10f;
    public float minSafeDistance = 4f;
    #endregion

    #region SPAWN TIMING
    [Header("⏱️ SPAWN INTERVALS")]
[Tooltip("Base obstacle interval (detik) - INCREASED for better spacing")]
public float baseObstacleInterval = 3.5f; // ✅ INCREASED from 2.5f to 3.5f

[Tooltip("Base coin interval (detik)")]
public float baseCoinInterval = 1.8f;

    [Tooltip("SpeedBoost spawn rate multiplier")]
    public float speedBoostSpawnMultiplier = 0.7f;
    #endregion

    #region STAR SYSTEM
    [Header("⭐ STAR SETTINGS")]
    public Vector2 star1TimeRange = new Vector2(10f, 20f);
    public Vector2 star2ProgressRange = new Vector2(0.35f, 0.55f);
    public Vector2 star3ProgressRange = new Vector2(0.80f, 0.95f);
    #endregion

    #region DEBUG
    [Header("🎯 DEBUG")]
    public bool enableDebugLogs = false;
    public bool showGizmos = true;

    [Header("📊 RUNTIME STATUS")]
    [SerializeField] private int totalSpawned = 0;
    [SerializeField] private int obstacleSpawned = 0;
    [SerializeField] private int coinSpawned = 0;
    [SerializeField] private int fragmentSpawned = 0;
    [SerializeField] private int starSpawned = 0;
    [SerializeField] private string currentObstacleId = "";
    [SerializeField] private string currentCoinPattern = "";
    #endregion

    #region PRIVATE VARS
    private int laneCount = 3;
    private float laneOffset = 2.5f;

    // Current active obstacle
    private ObstacleConfig currentObstacle = null;

    // Pattern weights
    private int totalObstacleWeight = 0;
    private int totalCoinWeight = 0;

    // Level data
    private FragmentRequirement[] levelRequirements;

    // Star tracking
    private bool[] starSpawned_Flags = new bool[3];
    private float star1ScheduledTime = 0f;

    // Coroutines
    private Coroutine obstacleCoroutine;
    private Coroutine coinCoroutine;
    private Coroutine starCoroutine;

    private bool isInitialized = false;
    private bool isPaused = false;

    // Obstacle tracking
    private float lastObstacleSpawnY = 0f;
    private float obstacleBlockedUntilY = 0f;
    #endregion

    #region INITIALIZATION
    void Awake()
    {
        SetupReferences();
        BuildWeights();
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

    void BuildWeights()
    {
        totalObstacleWeight = 0;
        totalCoinWeight = 0;

        if (obstacleConfigs != null)
        {
            foreach (var cfg in obstacleConfigs)
            {
                if (cfg != null && cfg.IsValid())
                    totalObstacleWeight += cfg.selectionWeight;
            }
        }

        if (coinPatterns != null)
        {
            foreach (var pattern in coinPatterns)
            {
                if (pattern != null && pattern.IsValid())
                    totalCoinWeight += pattern.selectionWeight;
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

        lastObstacleSpawnY = spawnY - 10f;
        obstacleBlockedUntilY = spawnY - 10f;

        // Start spawners
        obstacleCoroutine = StartCoroutine(ObstacleSpawnerLoop());
        coinCoroutine = StartCoroutine(CoinSpawnerLoop());
        starCoroutine = StartCoroutine(StarSpawnerLoop());

        isInitialized = true;
        Log("✅ PROCEDURAL SPAWNER INITIALIZED");
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (obstacleConfigs == null || obstacleConfigs.Count == 0)
        {
            LogError("obstacleConfigs is empty!");
            valid = false;
        }

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogError("coinPatterns is empty!");
            valid = false;
        }

        if (coinPrefab == null)
        {
            LogError("coinPrefab is null!");
            valid = false;
        }

        if (starPrefab == null)
        {
            LogError("starPrefab is null!");
            valid = false;
        }

        // Validate configs
        if (obstacleConfigs != null)
        {
            foreach (var cfg in obstacleConfigs)
            {
                if (cfg != null && !cfg.IsValid())
                {
                    LogError($"Invalid obstacle config: {cfg.displayName}");
                    valid = false;
                }
            }
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

    void ScheduleStar1()
    {
        star1ScheduledTime = Time.time + Random.Range(star1TimeRange.x, star1TimeRange.y);
        Log($"Star 1 scheduled at T+{star1ScheduledTime - Time.time:F1}s");
    }
    #endregion

    #region OBSTACLE SPAWNER
    IEnumerator ObstacleSpawnerLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            // Check TimeFreeze

            if (CanSpawnObstacles())
            {
                yield return StartCoroutine(SpawnObstacleWave());

                float speedMod = GetSpeedModifier();
                float interval = baseObstacleInterval * speedMod;

                yield return new WaitForSeconds(interval);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    bool CanSpawnObstacles()
    {
        if (BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)
            return false;

        return !isPaused;
    }

    IEnumerator SpawnObstacleWave()
{
    // Select obstacle
    ObstacleConfig config = SelectObstacleConfig();
    if (config == null)
    {
        LogWarning("No valid obstacle config!");
        yield break;
    }

    // ✅ SPAWN AT X=0 (prefab handles child positioning)
    Vector3 spawnPos = new Vector3(0f, spawnY, 0f);
    GameObject obstacle = Instantiate(config.prefab, spawnPos, Quaternion.identity, spawnParent);

    // ✅ UPDATED: Setup obstacle (dengan dynamic support)
    float currentSpeed = GetCurrentSpeed();
    SetupObstacle(obstacle, config, currentSpeed);

    // Update tracking
    currentObstacle = config;
    currentObstacleId = config.obstacleId;
    lastObstacleSpawnY = spawnY;
    
    // ✅ UPDATED: Calculate blocked distance (termasuk extra spacing)
    float totalSpacing = config.GetTotalSpacing() + minSafeDistance;
    obstacleBlockedUntilY = spawnY - totalSpacing;

    obstacleSpawned++;
    totalSpawned++;

    Log($"🌍 Spawned obstacle: {config.displayName} (ID: {config.obstacleId}) - Total spacing: {totalSpacing:F1}");

    yield return null;
}

    ObstacleConfig SelectObstacleConfig()
    {
        if (obstacleConfigs == null || obstacleConfigs.Count == 0) return null;

        // Filter valid configs
        List<ObstacleConfig> validConfigs = obstacleConfigs.Where(c => c != null && c.IsValid()).ToList();

        if (validConfigs.Count == 0) return null;

        // Weighted selection
        int totalWeight = validConfigs.Sum(c => c.selectionWeight);
        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var cfg in validConfigs)
        {
            cumulative += cfg.selectionWeight;
            if (rand < cumulative)
                return cfg;
        }

        return validConfigs[0];
    }
    #endregion

    #region COIN SPAWNER
    IEnumerator CoinSpawnerLoop()
    {
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            yield return StartCoroutine(SpawnCoinWave());

            float speedMod = GetSpeedModifier();
            float interval = baseCoinInterval * speedMod;

            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator SpawnCoinWave()
    {
        // Select coin pattern
        CoinPattern pattern = SelectCoinPattern();
        if (pattern == null)
        {
            LogWarning("No valid coin pattern!");
            yield break;
        }

        // Get valid spawn lanes (based on current obstacle's free lanes)
        List<int> freeLanes = GetCurrentFreeLanes();
        List<int> validLanes = pattern.GetValidSpawnLanes(freeLanes);

        if (validLanes.Count == 0)
        {
            LogWarning($"No valid lanes for pattern: {pattern.patternName}");
            yield break;
        }

        currentCoinPattern = pattern.patternName;

        // ✅ SPAWN COINS
        float currentSpeed = GetCurrentSpeed();
        float spacing = pattern.verticalSpacing;

        // ✅ ADJUST SPACING untuk SpeedBoost
        if (BoosterManager.Instance != null && BoosterManager.Instance.speedBoostActive)
        {
            spacing *= 1.5f; // Increase spacing to maintain visual distance
        }

        foreach (int lane in validLanes)
        {
            float laneX = GetLaneWorldX(lane);
            float currentY = spawnY;

            for (int i = 0; i < pattern.coinsPerLane; i++)
            {
                currentY -= spacing;

                // Decide: coin or fragment?
                GameObject prefab = DecideCollectiblePrefab(pattern);

                if (prefab != null)
                {
                    Vector3 pos = new Vector3(laneX, currentY, 0f);
                    GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

                    SetupMoverFixed(spawned, currentSpeed);

                    if (prefab != coinPrefab && prefab != starPrefab)
                    {
                        SetupFragmentComponent(spawned, prefab);
                        fragmentSpawned++;
                    }
                    else
                    {
                        coinSpawned++;
                    }

                    totalSpawned++;
                }

                yield return null;
            }
        }

        Log($"💰 Spawned coin pattern: {pattern.patternName} (compatible: {pattern.compatibleObstacleId}) in {validLanes.Count} lanes");
    }

    CoinPattern SelectCoinPattern()
    {
        if (coinPatterns == null || coinPatterns.Count == 0) return null;

        // Filter compatible patterns
        string currentObstacleId = currentObstacle != null ? currentObstacle.obstacleId : "";

        List<CoinPattern> compatiblePatterns = coinPatterns
            .Where(p => p != null && p.IsValid() && p.IsCompatibleWith(currentObstacleId))
            .ToList();

        if (compatiblePatterns.Count == 0)
        {
            // Fallback: allow any
            compatiblePatterns = coinPatterns.Where(p => p != null && p.IsValid()).ToList();
        }

        if (compatiblePatterns.Count == 0) return null;

        // Weighted selection
        int totalWeight = compatiblePatterns.Sum(p => p.selectionWeight);
        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var p in compatiblePatterns)
        {
            cumulative += p.selectionWeight;
            if (rand < cumulative)
                return p;
        }

        return compatiblePatterns[0];
    }

    List<int> GetCurrentFreeLanes()
    {
        if (currentObstacle != null && currentObstacle.freeLanes != null && currentObstacle.freeLanes.Count > 0)
        {
            return new List<int>(currentObstacle.freeLanes);
        }

        // Fallback: all lanes
        List<int> allLanes = new List<int>();
        for (int i = 0; i < laneCount; i++)
        {
            allLanes.Add(i);
        }
        return allLanes;
    }

    GameObject DecideCollectiblePrefab(CoinPattern pattern)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < pattern.fragmentChance)
        {
            GameObject fragmentPrefab = GetRequiredFragmentPrefab();
            if (fragmentPrefab != null)
                return fragmentPrefab;
        }

        return coinPrefab;
    }

    GameObject GetRequiredFragmentPrefab()
    {
        if (levelRequirements == null || levelRequirements.Length == 0) return null;
        if (fragmentRegistry == null) return null;

        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];
        return fragmentRegistry.GetPrefab(req.type, req.colorVariant);
    }

    void SetupFragmentComponent(GameObject spawned, GameObject originalPrefab)
    {
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

    #region STAR SPAWNER
    IEnumerator StarSpawnerLoop()
    {
        yield return new WaitForSeconds(2f);

        while (starSpawned < 3)
        {
            yield return new WaitForSeconds(0.5f);

            float progress = GetFragmentProgress();

            // Star 1: Time-based
            if (!starSpawned_Flags[0] && Time.time >= star1ScheduledTime)
            {
                yield return StartCoroutine(TrySpawnStar(0));
            }

            // Star 2: Progress 35-55%
            if (!starSpawned_Flags[1] && starSpawned_Flags[0] &&
                progress >= star2ProgressRange.x && progress <= star2ProgressRange.y)
            {
                yield return StartCoroutine(TrySpawnStar(1));
            }

            // Star 3: Progress 80-95%
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
        int maxAttempts = 30;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            // Get free lanes
            List<int> freeLanes = GetCurrentFreeLanes();

            if (freeLanes.Count > 0)
            {
                int lane = freeLanes[Random.Range(0, freeLanes.Count)];
                float laneX = GetLaneWorldX(lane);

                Vector3 pos = new Vector3(laneX, spawnY, 0f);
                GameObject star = Instantiate(starPrefab, pos, Quaternion.identity, spawnParent);

                float speed = GetCurrentSpeed();
                SetupMoverFixed(star, speed);

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

    // ✅ ADD THIS METHOD AFTER SpawnObstacleWave()

/// <summary>
/// ✅ UPDATED: Setup obstacle dengan support untuk dynamic movement
/// </summary>
void SetupObstacle(GameObject obstacle, ObstacleConfig config, float currentSpeed)
{
    // Setup vertical mover (normal downward movement)
    SetupMoverFixed(obstacle, currentSpeed);
    
    // ✅ CHECK: Apakah ini dynamic obstacle?
    if (config.isDynamicObstacle)
    {
        // Add dynamic mover component
        var dynamicMover = obstacle.GetComponent<DynamicObstacleMover>();
        
        if (dynamicMover == null)
        {
            dynamicMover = obstacle.AddComponent<DynamicObstacleMover>();
        }
        
        // Calculate horizontal speed (faster = more aggressive)
        float horizontalSpeed = currentSpeed * config.dynamicSpeedMultiplier;
        
        // Initialize dynamic movement
        dynamicMover.Initialize(
            config.movementLane,
            horizontalSpeed,
            currentSpeed,
            config.movementStartDelay
        );
        
        Log($"⚡ Dynamic obstacle initialized: {config.displayName} → Lane {config.movementLane} @ speed x{config.dynamicSpeedMultiplier}");
    }
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
    void SetupMoverFixed(GameObject obj, float speed)
    {
        var planetMover = obj.GetComponent<PlanetMover>();
        if (planetMover != null)
        {
            planetMover.SetFixedSpeed(speed);
            return;
        }

        var coinMover = obj.GetComponent<CoinMover>();
        if (coinMover != null)
        {
            coinMover.SetFixedSpeed(speed);
            return;
        }

        var fragmentMover = obj.GetComponent<FragmentMover>();
        if (fragmentMover != null)
        {
            fragmentMover.SetFixedSpeed(speed);
            return;
        }

        // Fallback
        var mover = obj.AddComponent<PlanetMover>();
        mover.SetFixedSpeed(speed);
    }

    float GetCurrentSpeed()
    {
        if (difficultyManager != null)
            return difficultyManager.CurrentSpeed;
        return 3f;
    }

    float GetSpeedModifier()
    {
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
    
    #region PUBLIC API
    public void PauseSpawner()
    {
        isPaused = true;
        Log("⏸️ Spawner paused");
    }
    
    public void ResumeSpawner()
    {
        isPaused = false;
        Log("▶️ Spawner resumed");
    }
    
    public void StopSpawner()
    {
        if (obstacleCoroutine != null)
            StopCoroutine(obstacleCoroutine);
        
        if (coinCoroutine != null)
            StopCoroutine(coinCoroutine);
        
        if (starCoroutine != null)
            StopCoroutine(starCoroutine);
        
        Log("⏹️ Spawner stopped");
    }
    #endregion
    
    #region DEBUG & GIZMOS
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        
        // Draw spawn line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-10, spawnY, 0), new Vector3(10, spawnY, 0));
        
        // Draw lane markers
        for (int i = 0; i < laneCount; i++)
        {
            float x = GetLaneWorldX(i);
            
            // Check if lane is free
            bool isFree = true;
            if (currentObstacle != null)
            {
                isFree = currentObstacle.IsLaneFree(i);
            }
            
            Gizmos.color = isFree ? Color.green : Color.red;
            Gizmos.DrawLine(new Vector3(x, spawnY - 3, 0), new Vector3(x, spawnY + 3, 0));
        }
        
#if UNITY_EDITOR
        // Draw status text
        float progress = GetFragmentProgress();
        string status = $"Obstacles: {obstacleSpawned} | Coins: {coinSpawned} | Fragments: {fragmentSpawned}\n";
        status += $"Stars: {starSpawned}/3 | Progress: {progress:P0}\n";
        status += $"Current Obstacle: {currentObstacleId}\n";
        status += $"Current Coin Pattern: {currentCoinPattern}";
        
        if (BoosterManager.Instance != null)
        {
            if (BoosterManager.Instance.timeFreezeActive)
                status += "\n⏸️ TIME FREEZE ACTIVE";
            
            if (BoosterManager.Instance.speedBoostActive)
                status += "\n⚡ SPEED BOOST ACTIVE";
        }
        
        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 2f, 0),
            status,
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white }, fontSize = 11 }
        );
#endif
    }
    
    [ContextMenu("Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("========== PROCEDURAL SPAWNER STATUS ==========");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Total Spawned: {totalSpawned}");
        Debug.Log($"Obstacles: {obstacleSpawned} | Coins: {coinSpawned}");
        Debug.Log($"Fragments: {fragmentSpawned} | Stars: {starSpawned}/3");
        Debug.Log($"Fragment Progress: {GetFragmentProgress():P0}");
        Debug.Log($"Current Obstacle: {currentObstacleId}");
        Debug.Log($"Current Coin Pattern: {currentCoinPattern}");
        
        if (currentObstacle != null)
        {
            Debug.Log($"\n--- Current Obstacle Info ---");
            Debug.Log($"Name: {currentObstacle.displayName}");
            Debug.Log($"Blocked Lanes: {string.Join(", ", currentObstacle.blockedLanes)}");
            Debug.Log($"Free Lanes: {string.Join(", ", currentObstacle.freeLanes)}");
        }
        
        Debug.Log("===============================================");
    }
    
    [ContextMenu("Debug: Force Spawn Obstacle")]
    void Debug_ForceSpawnObstacle()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(SpawnObstacleWave());
    }
    
    [ContextMenu("Debug: Force Spawn Coins")]
    void Debug_ForceSpawnCoins()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(SpawnCoinWave());
    }
    
    [ContextMenu("Debug: Force Spawn Star")]
    void Debug_ForceSpawnStar()
    {
        if (!Application.isPlaying) return;
        if (starSpawned >= 3)
        {
            Debug.LogWarning("All 3 stars already spawned!");
            return;
        }
        StartCoroutine(TrySpawnStar(starSpawned));
    }
    
    [ContextMenu("Debug: List All Patterns")]
    void Debug_ListPatterns()
    {
        Debug.Log("========== OBSTACLE CONFIGS ==========");
        if (obstacleConfigs != null)
        {
            for (int i = 0; i < obstacleConfigs.Count; i++)
            {
                var cfg = obstacleConfigs[i];
                if (cfg != null)
                {
                    Debug.Log($"[{i}] ID: {cfg.obstacleId} | Name: {cfg.displayName} | Blocked: [{string.Join(",", cfg.blockedLanes)}] | Free: [{string.Join(",", cfg.freeLanes)}]");
                }
            }
        }
        
        Debug.Log("\n========== COIN PATTERNS ==========");
        if (coinPatterns != null)
        {
            for (int i = 0; i < coinPatterns.Count; i++)
            {
                var pattern = coinPatterns[i];
                if (pattern != null)
                {
                    Debug.Log($"[{i}] Name: {pattern.patternName} | Compatible: {pattern.compatibleObstacleId} | Lanes: [{string.Join(",", pattern.spawnLanes)}]");
                }
            }
        }
    }
    #endregion
}
