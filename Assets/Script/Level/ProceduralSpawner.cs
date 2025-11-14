using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ✅ FINAL FIXED VERSION - Coin Alignment + Dynamic Movement Fix
/// 
/// FIXES:
/// 1. ✅ Coin spawn sejajar dengan obstacle (tengah-tengah obstacle height)
/// 2. ✅ Dynamic movement sesuai config (LinearLeft bergerak, bukan LinearRight)
/// 3. Star spawn dengan limit
/// 4. Proper spacing
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
    
    [Tooltip("Chance untuk spawn star instead of coin (%)")]
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

    // ✅ NEW: Track current obstacle spawn position
    private float currentObstacleSpawnY = 0f;
    private float currentObstacleHeight = 0f;

    // Current active obstacle
    private ObstacleConfig currentObstacle = null;

    // Pattern weights
    private int totalObstacleWeight = 0;
    private int totalCoinWeight = 0;

    // Level data
    private FragmentRequirement[] levelRequirements;

    // Coroutines
    private Coroutine obstacleCoroutine;
    private bool isPaused = false;

    // Obstacle tracking
    private float lastObstacleSpawnY = 0f;
    private float obstacleBlockedUntilY = 0f;
    
    // Track spawned stars
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
        
        starsSpawnedThisLevel = 0;

        obstacleCoroutine = StartCoroutine(ObstacleSpawnerLoop());
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
                yield return StartCoroutine(SpawnObstacleWave());
                
                // ✅ CRITICAL: Spawn coin wave SETELAH obstacle spawn
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
        ObstacleConfig config = SelectObstacleConfig();
        if (config == null)
        {
            LogWarning("No valid obstacle config!");
            yield break;
        }

        // ✅ FIX 1: Store obstacle spawn position & height
        currentObstacleSpawnY = spawnY;
        currentObstacleHeight = config.obstacleHeight;

        Vector3 spawnPos = new Vector3(0f, currentObstacleSpawnY, 0f);
        GameObject obstacle = Instantiate(config.prefab, spawnPos, Quaternion.identity, spawnParent);

        float currentSpeed = GetCurrentSpeed();
        SetupObstacle(obstacle, config, currentSpeed);

        currentObstacle = config;
        currentObstacleId = config.obstacleId;
        lastObstacleSpawnY = spawnY;
        
        float totalSpacing = config.obstacleHeight + config.extraSpacing + minSafeSpacing;
        obstacleBlockedUntilY = spawnY - totalSpacing;

        obstacleSpawned++;
        totalSpawned++;

        Log($"🌍 Spawned: {config.displayName} (ID: {config.obstacleId}) | Dynamic: {config.isDynamicObstacle} | Height: {config.obstacleHeight}");

        yield return null;
    }

    ObstacleConfig SelectObstacleConfig()
    {
        if (obstacleConfigs == null || obstacleConfigs.Count == 0) return null;

        List<ObstacleConfig> validConfigs = obstacleConfigs.Where(c => c != null && c.IsValid()).ToList();

        if (validConfigs.Count == 0) return null;

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
/// ✅ ALTERNATIVE: Track obstacle reference to prevent wrong assignment
/// </summary>
void SetupObstacle(GameObject obstacle, ObstacleConfig config, float currentSpeed)
{
    // 1. Setup vertical mover
    SetupMoverFixed(obstacle, currentSpeed);
    
    // 2. Clean existing components
    var existingMovers = obstacle.GetComponents<DynamicObstacleMover>();
    if (existingMovers != null && existingMovers.Length > 0)
    {
        foreach (var mover in existingMovers)
        {
            DestroyImmediate(mover); // ✅ Use DestroyImmediate
        }
    }
    
    // 3. Debug logging
    Log($"Setting up: {config.displayName} (Dynamic: {config.isDynamicObstacle})");
    
    // 4. ✅ Add component with validation
    if (config.isDynamicObstacle)
    {
        // Double-check no existing component
        if (obstacle.GetComponent<DynamicObstacleMover>() != null)
        {
            LogWarning($"⚠️ DynamicMover still exists after cleanup! Skipping add.");
            return;
        }
        
        var dynamicMover = obstacle.AddComponent<DynamicObstacleMover>();
        
        // ✅ CRITICAL: Set obstacle name as identifier
        obstacle.name = $"{config.displayName}(Clone-DYNAMIC)";
        
        float horizontalSpeed = currentSpeed * config.dynamicSpeedMultiplier;
        
        dynamicMover.Initialize(
            config.movementLane,
            horizontalSpeed,
            currentSpeed,
            config.movementStartDelay
        );
        
        Log($"⚡ ADDED DYNAMIC: {obstacle.name} → Lane {config.movementLane}");
    }
    else
    {
        // ✅ Mark as static in name
        obstacle.name = $"{config.displayName}(Clone-STATIC)";
        Log($"🔵 STATIC: {obstacle.name}");
    }
}

    #endregion

    #region COIN SPAWNER
    /// <summary>
    /// ✅ FIX 1: Spawn coin ALIGNED dengan obstacle (tengah-tengah obstacle height)
    /// </summary>
    IEnumerator SpawnCoinWaveForCurrentObstacle()
    {
        if (currentObstacle == null)
        {
            LogWarning("No current obstacle! Skipping coin spawn.");
            yield break;
        }
        
        CoinPattern pattern = SelectCompatibleCoinPattern(currentObstacle.obstacleId);
        
        if (pattern == null)
        {
            Log($"⚠️ No compatible coin pattern for {currentObstacle.obstacleId}");
            yield break;
        }

        List<int> freeLanes = GetCurrentFreeLanes();
        List<int> validLanes = pattern.GetValidSpawnLanes(freeLanes);

        if (validLanes.Count == 0)
        {
            LogWarning($"No valid lanes for pattern: {pattern.patternName}");
            yield break;
        }

        currentCoinPattern = pattern.patternName;

        float currentSpeed = GetCurrentSpeed();
        float spacing = pattern.verticalSpacing;

        if (BoosterManager.Instance != null && BoosterManager.Instance.speedBoostActive)
        {
            spacing *= 1.5f;
        }

        // ✅ FIX 1: Calculate coin start position (TENGAH-TENGAH obstacle)
        float totalPatternHeight = pattern.coinsPerLane * spacing;
        
        // Start dari tengah obstacle, lalu spawn ke atas dan bawah
        float coinStartY = currentObstacleSpawnY - (currentObstacleHeight / 3f) + (totalPatternHeight / 3f);

        Log($"💰 Coin alignment: Obstacle Y={currentObstacleSpawnY:F1}, Height={currentObstacleHeight:F1}, Coin Start Y={coinStartY:F1}");

        foreach (int lane in validLanes)
        {
            float laneX = GetLaneWorldX(lane);
            float currentY = coinStartY;

            for (int i = 0; i < pattern.coinsPerLane; i++)
            {
                GameObject prefab = DecideCollectiblePrefab();

                if (prefab != null)
                {
                    Vector3 pos = new Vector3(laneX, currentY, 0f);
                    GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

                    SetupMoverFixed(spawned, currentSpeed);

                    if (prefab == starPrefab)
                    {
                        starSpawned++;
                        starsSpawnedThisLevel++;
                        Log($"⭐ Star spawned - Total: {starsSpawnedThisLevel}/{totalStarsPerLevel}");
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

                // Move DOWN untuk coin berikutnya
                currentY -= spacing;

                yield return null;
            }
        }

        Log($"✓ Spawned pattern: {pattern.patternName} in {validLanes.Count} lanes (aligned with {currentObstacle.displayName})");
    }

    CoinPattern SelectCompatibleCoinPattern(string obstacleId)
    {
        if (coinPatterns == null || coinPatterns.Count == 0) return null;

        List<CoinPattern> compatiblePatterns = coinPatterns
            .Where(p => p != null && p.IsValid() && p.IsCompatibleWith(obstacleId))
            .ToList();

        if (compatiblePatterns.Count == 0)
        {
            Log($"⚠️ No compatible patterns for obstacle: {obstacleId}");
            return null;
        }

        int totalWeight = compatiblePatterns.Sum(p => p.selectionWeight);
        int rand = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var p in compatiblePatterns)
        {
            cumulative += p.selectionWeight;
            if (rand < cumulative)
            {
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

        List<int> allLanes = new List<int>();
        for (int i = 0; i < laneCount; i++)
        {
            allLanes.Add(i);
        }
        return allLanes;
    }

    GameObject DecideCollectiblePrefab()
    {
        float roll = Random.Range(0f, 100f);

        if (starsSpawnedThisLevel < totalStarsPerLevel && roll < starChance)
        {
            return starPrefab;
        }

        if (roll < fragmentChance)
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
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(-10, spawnY, 0), new Vector3(10, spawnY, 0));
        
        for (int i = 0; i < laneCount; i++)
        {
            float x = GetLaneWorldX(i);
            
            bool isFree = true;
            if (currentObstacle != null)
            {
                isFree = currentObstacle.IsLaneFree(i);
            }
            
            Gizmos.color = isFree ? Color.green : Color.red;
            Gizmos.DrawLine(new Vector3(x, spawnY - 3, 0), new Vector3(x, spawnY + 3, 0));
        }
        
        // ✅ NEW: Draw obstacle height range
        if (currentObstacle != null && currentObstacleHeight > 0)
        {
            Gizmos.color = Color.yellow;
            float topY = currentObstacleSpawnY;
            float bottomY = currentObstacleSpawnY - currentObstacleHeight;
            
            Gizmos.DrawLine(new Vector3(-5, topY, 0), new Vector3(5, topY, 0));
            Gizmos.DrawLine(new Vector3(-5, bottomY, 0), new Vector3(5, bottomY, 0));
        }
        
#if UNITY_EDITOR
        string status = $"Obstacles: {obstacleSpawned} | Coins: {coinSpawned} | Fragments: {fragmentSpawned}\n";
        status += $"Stars: {starsSpawnedThisLevel}/{totalStarsPerLevel}\n";
        status += $"Current: {currentObstacleId} (H: {currentObstacleHeight:F1})\n";
        status += $"Pattern: {currentCoinPattern}";
        
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
        Debug.Log($"Total Spawned: {totalSpawned}");
        Debug.Log($"Obstacles: {obstacleSpawned} | Coins: {coinSpawned}");
        Debug.Log($"Fragments: {fragmentSpawned} | Stars: {starsSpawnedThisLevel}/{totalStarsPerLevel}");
        Debug.Log($"Current Obstacle: {currentObstacleId} (Height: {currentObstacleHeight})");
        Debug.Log($"Current Pattern: {currentCoinPattern}");
        
        if (currentObstacle != null)
        {
            Debug.Log($"\n--- Obstacle Info ---");
            Debug.Log($"Name: {currentObstacle.displayName}");
            Debug.Log($"ID: {currentObstacle.obstacleId}");
            Debug.Log($"isDynamicObstacle: {currentObstacle.isDynamicObstacle}");
            Debug.Log($"Movement Lane: {currentObstacle.movementLane}");
            Debug.Log($"Blocked: [{string.Join(", ", currentObstacle.blockedLanes)}]");
            Debug.Log($"Free: [{string.Join(", ", currentObstacle.freeLanes)}]");
        }
        
        Debug.Log("===============================================");
    }
    
    [ContextMenu("Debug: List Obstacle Configs")]
    void Debug_ListObstacleConfigs()
    {
        Debug.Log("========== OBSTACLE CONFIGS ==========");
        if (obstacleConfigs != null)
        {
            foreach (var cfg in obstacleConfigs)
            {
                if (cfg != null)
                {
                    Debug.Log($"ID: {cfg.obstacleId} | Name: {cfg.displayName}");
                    Debug.Log($"  → isDynamicObstacle: {cfg.isDynamicObstacle}");
                    Debug.Log($"  → movementLane: {cfg.movementLane}");
                    Debug.Log($"  → Height: {cfg.obstacleHeight}");
                }
            }
        }
        Debug.Log("======================================");
    }
    #endregion
}