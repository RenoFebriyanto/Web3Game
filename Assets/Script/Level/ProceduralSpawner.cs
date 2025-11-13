using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ✅ FINAL FIXED VERSION - ALL BUGS RESOLVED
/// 
/// FIXES:
/// 1. Coin pattern HANYA spawn untuk compatible obstacle
/// 2. Dynamic obstacle STRICT check (hanya LinearLeft yang bergerak)
/// 3. Star TIDAK spawn sendiri - replace coin seperti fragment (dengan chance lebih rendah)
/// 4. Spacing fix: minSafeSpacing = 2.5
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
    #endregion

    #region SPAWN TIMING
    [Header("⏱️ SPAWN INTERVALS")]
    [Tooltip("Base obstacle interval (detik)")]
    public float baseObstacleInterval = 3.5f;

    [Tooltip("Base coin interval (detik)")]
    public float baseCoinInterval = 1.8f;

    [Tooltip("SpeedBoost spawn rate multiplier")]
    public float speedBoostSpawnMultiplier = 0.7f;
    
    [Header("🎯 SPACING SAFETY")]
    [Tooltip("Minimum safe spacing antara obstacles (2.5 = 1 lane width)")]
    public float minSafeSpacing = 2.5f;
    #endregion

    #region COLLECTIBLE CHANCES
    [Header("🎲 COLLECTIBLE SPAWN CHANCES")]
    [Tooltip("Chance untuk spawn fragment instead of coin (%)")]
    [Range(0f, 100f)]
    public float fragmentChance = 15f;
    
    [Tooltip("Chance untuk spawn star instead of coin (%) - LEBIH RENDAH dari fragment")]
    [Range(0f, 100f)]
    public float starChance = 5f;
    
    [Header("⭐ STAR PROGRESS TRACKING")]
    [Tooltip("Total stars yang harus di-spawn per level")]
    public int totalStarsPerLevel = 3;
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

    // Coroutines
    private Coroutine obstacleCoroutine;
    private Coroutine coinCoroutine;

    private bool isInitialized = false;
    private bool isPaused = false;

    // Obstacle tracking
    private float lastObstacleSpawnY = 0f;
    private float obstacleBlockedUntilY = 0f;
    
    // ✅ NEW: Track spawned stars
    private int starsSpawnedThisLevel = 0;
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

        lastObstacleSpawnY = spawnY - 10f;
        obstacleBlockedUntilY = spawnY - 10f;
        
        // ✅ Reset star counter
        starsSpawnedThisLevel = 0;

        // ✅ CRITICAL: Start HANYA obstacle spawner
        // Coin spawner akan di-trigger DARI obstacle spawner
        obstacleCoroutine = StartCoroutine(ObstacleSpawnerLoop());

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
    #endregion

    #region OBSTACLE SPAWNER
    IEnumerator ObstacleSpawnerLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            if (CanSpawnObstacles())
            {
                // ✅ CRITICAL: Spawn obstacle LALU spawn coin pattern yang compatible
                yield return StartCoroutine(SpawnObstacleWave());
                
                // ✅ CRITICAL: Spawn coin pattern HANYA untuk obstacle yang baru di-spawn
                yield return StartCoroutine(SpawnCoinWaveForCurrentObstacle());

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

        // Spawn AT X=0 (prefab handles child positioning)
        Vector3 spawnPos = new Vector3(0f, spawnY, 0f);
        GameObject obstacle = Instantiate(config.prefab, spawnPos, Quaternion.identity, spawnParent);

        // ✅ CRITICAL FIX: Setup obstacle dengan strict dynamic check
        float currentSpeed = GetCurrentSpeed();
        SetupObstacle(obstacle, config, currentSpeed);

        // Update tracking
        currentObstacle = config;
        currentObstacleId = config.obstacleId;
        lastObstacleSpawnY = spawnY;
        
        // Calculate blocked distance
        float totalSpacing = config.obstacleHeight + config.extraSpacing + minSafeSpacing;
        obstacleBlockedUntilY = spawnY - totalSpacing;

        obstacleSpawned++;
        totalSpawned++;

        Log($"🌍 Spawned: {config.displayName} (ID: {config.obstacleId}) | Dynamic: {config.isDynamicObstacle} | Spacing: {totalSpacing:F1}");

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

    /// <summary>
    /// ✅ CRITICAL FIX: Setup obstacle - STRICT check untuk dynamic movement
    /// </summary>
    void SetupObstacle(GameObject obstacle, ObstacleConfig config, float currentSpeed)
    {
        // 1. Setup vertical mover (normal downward movement) - SEMUA obstacle punya ini
        SetupMoverFixed(obstacle, currentSpeed);
        
        // 2. ✅ STRICT CHECK: HANYA add dynamic mover jika isDynamicObstacle == true
        if (config.isDynamicObstacle)
        {
            // Check apakah sudah ada component (prevent duplicate)
            var existingMover = obstacle.GetComponent<DynamicObstacleMover>();
            
            if (existingMover != null)
            {
                // Already has component (prefab might have it)
                LogWarning($"{config.displayName} already has DynamicObstacleMover component!");
                
                // ✅ CRITICAL: Re-initialize even if exists
                float horizontalSpeed = currentSpeed * config.dynamicSpeedMultiplier;
                existingMover.Initialize(
                    config.movementLane,
                    horizontalSpeed,
                    currentSpeed,
                    config.movementStartDelay
                );
            }
            else
            {
                // Add dynamic mover component
                var dynamicMover = obstacle.AddComponent<DynamicObstacleMover>();
                
                // Calculate horizontal speed
                float horizontalSpeed = currentSpeed * config.dynamicSpeedMultiplier;
                
                // Initialize dynamic movement
                dynamicMover.Initialize(
                    config.movementLane,
                    horizontalSpeed,
                    currentSpeed,
                    config.movementStartDelay
                );
                
                Log($"⚡ DYNAMIC OBSTACLE ADDED: {config.displayName} → Lane {config.movementLane}");
            }
        }
        else
        {
            // ✅ CRITICAL: DESTROY any existing DynamicObstacleMover component
            var existingMover = obstacle.GetComponent<DynamicObstacleMover>();
            if (existingMover != null)
            {
                LogWarning($"🗑️ DESTROYING unwanted DynamicObstacleMover from STATIC obstacle: {config.displayName}");
                Destroy(existingMover);
            }
            
            Log($"🔵 STATIC OBSTACLE: {config.displayName} - No dynamic movement");
        }
    }
    #endregion

    #region COIN SPAWNER
    /// <summary>
    /// ✅ CRITICAL FIX: Spawn coin wave HANYA untuk obstacle yang baru di-spawn
    /// Dipanggil dari ObstacleSpawnerLoop SETELAH obstacle spawn
    /// </summary>
    IEnumerator SpawnCoinWaveForCurrentObstacle()
    {
        // ✅ CRITICAL: Check apakah ada obstacle yang active
        if (currentObstacle == null)
        {
            LogWarning("No current obstacle! Skipping coin spawn.");
            yield break;
        }
        
        // Select coin pattern yang COMPATIBLE dengan obstacle ini
        CoinPattern pattern = SelectCompatibleCoinPattern(currentObstacle.obstacleId);
        
        if (pattern == null)
        {
            Log($"⚠️ No compatible coin pattern for {currentObstacle.obstacleId}");
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

        // Spawn coins
        float currentSpeed = GetCurrentSpeed();
        float spacing = pattern.verticalSpacing;

        // Adjust spacing untuk SpeedBoost
        if (BoosterManager.Instance != null && BoosterManager.Instance.speedBoostActive)
        {
            spacing *= 1.5f;
        }

        foreach (int lane in validLanes)
        {
            float laneX = GetLaneWorldX(lane);
            float currentY = spawnY;

            for (int i = 0; i < pattern.coinsPerLane; i++)
            {
                currentY -= spacing;

                // ✅ CRITICAL: Decide collectible type (coin/fragment/star)
                GameObject prefab = DecideCollectiblePrefab();

                if (prefab != null)
                {
                    Vector3 pos = new Vector3(laneX, currentY, 0f);
                    GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

                    SetupMoverFixed(spawned, currentSpeed);

                    // Setup component based on prefab type
                    if (prefab == starPrefab)
                    {
                        starSpawned++;
                        starsSpawnedThisLevel++;
                        Log($"⭐ Star spawned (replaced coin) - Total: {starsSpawnedThisLevel}/{totalStarsPerLevel}");
                    }
                    else if (prefab != coinPrefab)
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

        Log($"💰 Spawned coin pattern: {pattern.patternName} (compatible: {currentObstacle.obstacleId}) in {validLanes.Count} lanes");
    }

    /// <summary>
    /// ✅ CRITICAL FIX: Select coin pattern yang COMPATIBLE dengan obstacle ID
    /// </summary>
    CoinPattern SelectCompatibleCoinPattern(string obstacleId)
    {
        if (coinPatterns == null || coinPatterns.Count == 0) return null;

        // ✅ CRITICAL: Filter HANYA pattern yang compatible dengan obstacle ID ini
        List<CoinPattern> compatiblePatterns = coinPatterns
            .Where(p => p != null && p.IsValid() && p.IsCompatibleWith(obstacleId))
            .ToList();

        if (compatiblePatterns.Count == 0)
        {
            Log($"⚠️ No compatible patterns for obstacle: {obstacleId}");
            return null;
        }

        // Weighted selection
        int totalWeight = compatiblePatterns.Sum(p => p.selectionWeight);
        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var p in compatiblePatterns)
        {
            cumulative += p.selectionWeight;
            if (rand < cumulative)
            {
                Log($"✓ Selected compatible pattern: {p.patternName} for obstacle: {obstacleId}");
                return p;
            }
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

    /// <summary>
    /// ✅ CRITICAL FIX: Decide collectible type dengan priority: Star > Fragment > Coin
    /// Star hanya spawn jika belum mencapai limit
    /// </summary>
    GameObject DecideCollectiblePrefab()
    {
        float roll = Random.Range(0f, 100f);

        // ✅ PRIORITY 1: Star (HANYA jika belum mencapai limit)
        if (starsSpawnedThisLevel < totalStarsPerLevel && roll < starChance)
        {
            return starPrefab;
        }

        // ✅ PRIORITY 2: Fragment
        if (roll < fragmentChance)
        {
            GameObject fragmentPrefab = GetRequiredFragmentPrefab();
            if (fragmentPrefab != null)
                return fragmentPrefab;
        }

        // ✅ PRIORITY 3: Coin (default)
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
        string status = $"Obstacles: {obstacleSpawned} | Coins: {coinSpawned} | Fragments: {fragmentSpawned}\n";
        status += $"Stars: {starsSpawnedThisLevel}/{totalStarsPerLevel}\n";
        status += $"Current Obstacle: {currentObstacleId}\n";
        status += $"Current Coin Pattern: {currentCoinPattern}";
        
        if (BoosterManager.Instance != null)
        {
            if (BoosterManager.Instance.timeFreezeActive)
                status += "\n⏸️ TIME FREEZE";
            
            if (BoosterManager.Instance.speedBoostActive)
                status += "\n⚡ SPEED BOOST";
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
        Debug.Log($"Fragments: {fragmentSpawned} | Stars: {starsSpawnedThisLevel}/{totalStarsPerLevel}");
        Debug.Log($"Current Obstacle: {currentObstacleId}");
        Debug.Log($"Current Coin Pattern: {currentCoinPattern}");
        
        if (currentObstacle != null)
        {
            Debug.Log($"\n--- Current Obstacle Info ---");
            Debug.Log($"Name: {currentObstacle.displayName}");
            Debug.Log($"ID: {currentObstacle.obstacleId}");
            Debug.Log($"Dynamic: {currentObstacle.isDynamicObstacle}");
            Debug.Log($"Blocked Lanes: {string.Join(", ", currentObstacle.blockedLanes)}");
            Debug.Log($"Free Lanes: {string.Join(", ", currentObstacle.freeLanes)}");
        }
        
        Debug.Log("===============================================");
    }
    
    [ContextMenu("Debug: Force Spawn Obstacle + Coin")]
    void Debug_ForceSpawnObstacle()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(DebugSpawnWave());
    }
    
    IEnumerator DebugSpawnWave()
    {
        yield return StartCoroutine(SpawnObstacleWave());
        yield return StartCoroutine(SpawnCoinWaveForCurrentObstacle());
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
                    Debug.Log($"[{i}] ID: {cfg.obstacleId} | Name: {cfg.displayName} | Dynamic: {cfg.isDynamicObstacle} | Spacing: {cfg.extraSpacing}");
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