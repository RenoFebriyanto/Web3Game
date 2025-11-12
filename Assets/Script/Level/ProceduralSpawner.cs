using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ✅ FIXED PROCEDURAL SPAWNER - Pattern Matching System
/// 
/// Features:
/// - Obstacle-Coin Pattern Matching via ID
/// - Separate Planet & Coin Spawners (independent timing)
/// - Lane Blocking System (coins spawn only in free lanes)
/// - TimeFreeze Support (stop planets, coins continue)
/// - SpeedBoost Support (consistent spacing)
/// - Star System (3 stars per level, progress-based)
/// - NO DELAYS (continuous spawning)
/// 
/// REPLACE: Assets/Script/Level/ProceduralSpawner.cs
/// </summary>
public class ProceduralSpawner : MonoBehaviour
{
    #region REFERENCES
    [Header("🔧 REQUIRED REFERENCES")]
    public LanesManager lanesManager;
    public DifficultyManager difficultyManager;
    
    [Header("📦 PREFABS")]
    public GameObject[] obstaclePrefabs;
    public GameObject coinPrefab;
    public GameObject starPrefab;
    
    [Header("📚 DATA")]
    public FragmentPrefabRegistry fragmentRegistry;
    public LevelDatabase levelDatabase;
    
    [Header("🎨 NEW PATTERN SYSTEM")]
    [Tooltip("Obstacle patterns dengan ID")]
    public List<ObstacleSpawnPattern> obstaclePatterns = new List<ObstacleSpawnPattern>();
    
    [Tooltip("Coin patterns dengan obstacle ID reference")]
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();
    
    [Header("⚙️ SPAWN SETTINGS")]
    public Transform spawnParent;
    public float spawnY = 10f;
    public float obstacleVerticalSpacing = 2.5f;
    public float coinVerticalSpacing = 2f;
    #endregion
    
    #region SPAWN TIMING
    [Header("⏱️ CONTINUOUS SPAWN TIMING (No Delays)")]
    [Tooltip("Base obstacle wave interval (detik)")]
    public float baseObstacleInterval = 2.0f;
    
    [Tooltip("Base coin wave interval (detik) - independent dari obstacle")]
    public float baseCoinInterval = 1.5f;
    
    [Tooltip("SpeedBoost multiplier untuk spawn rate (0.7 = 30% faster)")]
    public float speedBoostSpawnMultiplier = 0.7f;
    
    [Tooltip("Minimum safe distance antara waves (world units)")]
    public float minSafeDistance = 4f;
    #endregion
    
    #region STAR CONTROL
    [Header("⭐ STAR SYSTEM")]
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
    [SerializeField] private string currentObstaclePattern = "";
    [SerializeField] private string currentCoinPattern = "";
    #endregion
    
    #region PRIVATE VARS
    private int laneCount = 3;
    private float laneOffset = 2.5f;
    
    // Lane state tracking
    private class LaneState
    {
        public bool isBlocked;
        public float lastObstacleY;
        public string blockReason;
    }
    private LaneState[] lanes;
    
    // Current active obstacle pattern (untuk coin matching)
    private ObstacleSpawnPattern currentActiveObstaclePattern = null;
    private int currentObstacleBaseLane = 1; // Center lane default
    
    // Pattern weights
    private int totalObstacleWeight = 0;
    private int totalCoinWeight = 0;
    
    // Level requirements
    private FragmentRequirement[] levelRequirements;
    
    // Star tracking
    private bool[] starSpawned_Flags = new bool[3];
    private float star1ScheduledTime = 0f;
    
    // Coroutines
    private Coroutine obstacleSpawnerCoroutine;
    private Coroutine coinSpawnerCoroutine;
    private Coroutine starSpawnerCoroutine;
    
    private bool isInitialized = false;
    private bool isPaused = false;
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
                lastObstacleY = spawnY - 10f,
                blockReason = ""
            };
        }
    }
    
    void BuildPatternWeights()
    {
        totalObstacleWeight = 0;
        totalCoinWeight = 0;
        
        if (obstaclePatterns != null)
        {
            foreach (var p in obstaclePatterns)
            {
                if (p != null) totalObstacleWeight += p.selectionWeight;
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
        
        // Start separate spawner coroutines
        obstacleSpawnerCoroutine = StartCoroutine(ObstacleSpawnerLoop());
        coinSpawnerCoroutine = StartCoroutine(CoinSpawnerLoop());
        starSpawnerCoroutine = StartCoroutine(StarSpawnerLoop());
        
        isInitialized = true;
        Log("✅ PROCEDURAL SPAWNER INITIALIZED (Pattern Matching System)");
    }
    
    bool ValidateSetup()
    {
        bool valid = true;
        
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            LogError("obstaclePrefabs empty!");
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
        
        if (obstaclePatterns == null || obstaclePatterns.Count == 0)
        {
            LogError("obstaclePatterns empty!");
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
    
    #region OBSTACLE SPAWNER (Independent Loop)
    /// <summary>
    /// ✅ Obstacle spawner loop - INDEPENDENT dari coin spawner
    /// Stops saat TimeFreeze active
    /// </summary>
    IEnumerator ObstacleSpawnerLoop()
    {
        yield return new WaitForSeconds(0.5f); // Initial delay
        
        while (true)
        {
            // ✅ CHECK: TimeFreeze blocks obstacle spawning
            if (CanSpawnObstacles())
            {
                yield return StartCoroutine(SpawnObstacleWave());
                
                // ✅ CALCULATE NEXT SPAWN TIME (dengan speed modifier)
                float speedMod = GetSpeedModifier();
                float interval = baseObstacleInterval * speedMod;
                
                yield return new WaitForSeconds(interval);
            }
            else
            {
                // TimeFreeze active - wait
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
        // ✅ SELECT OBSTACLE PATTERN
        ObstacleSpawnPattern pattern = SelectObstaclePattern();
        if (pattern == null)
        {
            LogWarning("No valid obstacle pattern!");
            yield break;
        }
        
        // ✅ SELECT BASE LANE
        int baseLane = SelectBaseLane(pattern);
        if (baseLane == -1)
        {
            LogWarning($"Cannot find valid base lane for {pattern.displayName}");
            yield break;
        }
        
        // ✅ SAVE CURRENT PATTERN (untuk coin matching)
        currentActiveObstaclePattern = pattern;
        currentObstacleBaseLane = baseLane;
        currentObstaclePattern = pattern.patternId;
        
        // ✅ SPAWN OBSTACLES
        yield return StartCoroutine(SpawnObstaclePattern(pattern, baseLane));
        
        Log($"🌍 Spawned obstacle: {pattern.displayName} (ID: {pattern.patternId}) at lane {baseLane}");
    }
    
    IEnumerator SpawnObstaclePattern(ObstacleSpawnPattern pattern, int baseLane)
    {
        if (pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
            yield break;
        
        float currentY = spawnY;
        float currentSpeed = GetCurrentSpeed();
        
        // ✅ MARK LANES AS BLOCKED
        HashSet<int> blockedLanes = pattern.GetBlockedLanes(baseLane, laneCount);
        foreach (int lane in blockedLanes)
        {
            lanes[lane].isBlocked = true;
            lanes[lane].blockReason = pattern.patternId;
        }
        
        // ✅ SPAWN EACH OBSTACLE IN PATTERN
        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];
            int targetLane = baseLane + Mathf.RoundToInt(point.x);
            targetLane = Mathf.Clamp(targetLane, 0, laneCount - 1);
            
            currentY -= point.y * obstacleVerticalSpacing;
            
            // Spawn obstacle
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
            GameObject obstacle = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
            
            // ✅ CRITICAL: Set speed dengan FIXED mode (no dynamic adjustment)
            SetupMoverFixed(obstacle, currentSpeed);
            
            lanes[targetLane].lastObstacleY = currentY;
            
            obstacleSpawned++;
            totalSpawned++;
            
            yield return null; // No delay, instant spawn
        }
        
        // ✅ UPDATE LANE STATES (clear blocks setelah obstacle lewat)
        float patternLength = pattern.GetTotalVerticalDistance() * obstacleVerticalSpacing;
        float clearY = currentY - patternLength - minSafeDistance;
        
        foreach (int lane in blockedLanes)
        {
            lanes[lane].lastObstacleY = clearY;
        }
        
        // ✅ SCHEDULE LANE UNBLOCK
        StartCoroutine(UnblockLanesAfterDelay(blockedLanes.ToList(), patternLength));
    }
    
    IEnumerator UnblockLanesAfterDelay(List<int> blockedLanes, float distance)
    {
        float currentSpeed = GetCurrentSpeed();
        float timeToPass = distance / currentSpeed;
        
        yield return new WaitForSeconds(timeToPass + 1f); // Extra 1s safety
        
        foreach (int lane in blockedLanes)
        {
            lanes[lane].isBlocked = false;
            lanes[lane].blockReason = "";
        }
    }
    
    ObstacleSpawnPattern SelectObstaclePattern()
    {
        if (obstaclePatterns == null || obstaclePatterns.Count == 0) return null;
        
        // Filter valid patterns
        List<ObstacleSpawnPattern> validPatterns = new List<ObstacleSpawnPattern>();
        
        foreach (var pattern in obstaclePatterns)
        {
            if (pattern == null) continue;
            
            // Check if pattern can fit in any lane
            bool canFit = false;
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (pattern.IsValidForBaseLane(lane, laneCount))
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
    
    int SelectBaseLane(ObstacleSpawnPattern pattern)
    {
        List<int> validLanes = new List<int>();
        
        for (int lane = 0; lane < laneCount; lane++)
        {
            if (pattern.IsValidForBaseLane(lane, laneCount))
            {
                // Additional check: ensure lanes not recently blocked
                bool allClear = true;
                var usedLanes = pattern.GetUsedLanes(lane);
                
                foreach (int usedLane in usedLanes)
                {
                    if (lanes[usedLane].isBlocked ||
                        lanes[usedLane].lastObstacleY > spawnY - minSafeDistance)
                    {
                        allClear = false;
                        break;
                    }
                }
                
                if (allClear)
                    validLanes.Add(lane);
            }
        }
        
        if (validLanes.Count == 0) return -1;
        
        return validLanes[Random.Range(0, validLanes.Count)];
    }
    #endregion
    
    #region COIN SPAWNER (Independent Loop)
    /// <summary>
    /// ✅ Coin spawner loop - INDEPENDENT dari obstacle spawner
    /// Always active (even during TimeFreeze)
    /// </summary>
    IEnumerator CoinSpawnerLoop()
    {
        yield return new WaitForSeconds(1.0f); // Initial delay (agar ada obstacle dulu)
        
        while (true)
        {
            // ✅ ALWAYS SPAWN COINS (no TimeFreeze check)
            yield return StartCoroutine(SpawnCoinWave());
            
            // ✅ CALCULATE NEXT SPAWN TIME
            float speedMod = GetSpeedModifier();
            float interval = baseCoinInterval * speedMod;
            
            yield return new WaitForSeconds(interval);
        }
    }
    
    IEnumerator SpawnCoinWave()
    {
        // ✅ SELECT COIN PATTERN (matching current obstacle pattern)
        CoinSpawnPattern pattern = SelectCoinPattern();
        if (pattern == null)
        {
            LogWarning("No valid coin pattern!");
            yield break;
        }
        
        // ✅ DETERMINE BASE LANE (based on free lanes or current obstacle)
        int baseLane = SelectCoinBaseLane(pattern);
        if (baseLane == -1)
        {
            LogWarning("No free lanes for coin pattern");
            yield break;
        }
        
        currentCoinPattern = pattern.patternName;
        
        // ✅ SPAWN COINS
        yield return StartCoroutine(SpawnCoinPattern(pattern, baseLane));
        
        Log($"💰 Spawned coin: {pattern.patternName} (matches: {pattern.compatibleObstacleId}) at lane {baseLane}");
    }
    
    IEnumerator SpawnCoinPattern(CoinSpawnPattern pattern, int baseLane)
    {
        if (pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
            yield break;
        
        float currentY = spawnY;
        float currentSpeed = GetCurrentSpeed();
        
        // ✅ CALCULATE PROPER SPACING WITH SPEEDBOOST
        float spacing = coinVerticalSpacing;
        if (BoosterManager.Instance != null && BoosterManager.Instance.speedBoostActive)
        {
            // ✅ INCREASE SPACING during speedboost (keep distance consistent)
            spacing *= 1.5f;
        }
        
        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            Vector2 point = pattern.spawnPoints[i];
            int targetLane = baseLane + Mathf.RoundToInt(point.x);
            targetLane = Mathf.Clamp(targetLane, 0, laneCount - 1);
            
            currentY -= point.y * spacing;
            
            // ✅ DECIDE: Coin or Fragment?
            GameObject prefab = DecideCollectiblePrefab(pattern);
            
            if (prefab != null)
            {
                Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
                GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
                
                // ✅ CRITICAL: Set speed dengan FIXED mode
                SetupMoverFixed(spawned, currentSpeed);
                
                // Setup collectible component
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
            
            yield return null; // No delay
        }
    }
    
    CoinSpawnPattern SelectCoinPattern()
    {
        if (coinPatterns == null || coinPatterns.Count == 0) return null;
        
        // ✅ FILTER: Patterns compatible dengan current obstacle pattern
        List<CoinSpawnPattern> compatiblePatterns = new List<CoinSpawnPattern>();
        
        string currentObstacleId = currentActiveObstaclePattern != null ?
                                    currentActiveObstaclePattern.patternId : "";
        
        foreach (var pattern in coinPatterns)
        {
            if (pattern == null) continue;
            
            // Check compatibility
            if (pattern.IsCompatibleWith(currentObstacleId))
            {
                compatiblePatterns.Add(pattern);
            }
        }
        
        if (compatiblePatterns.Count == 0)
        {
            // Fallback: allow any pattern
            compatiblePatterns.AddRange(coinPatterns.Where(p => p != null));
        }
        
        if (compatiblePatterns.Count == 0) return null;
        
        // Weighted selection
        int totalWeight = 0;
        foreach (var p in compatiblePatterns)
        {
            totalWeight += p.selectionWeight;
        }
        
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
    
    int SelectCoinBaseLane(CoinSpawnPattern pattern)
    {
        // ✅ METHOD 1: Spawn in free lanes (not blocked by obstacles)
        if (pattern.spawnOnlyInFreeLanes && currentActiveObstaclePattern != null)
        {
            List<int> freeLanes = currentActiveObstaclePattern.GetFreeLanes(
                currentObstacleBaseLane, laneCount);
            
            if (freeLanes.Count > 0)
            {
                return freeLanes[Random.Range(0, freeLanes.Count)];
            }
        }
        
        // ✅ METHOD 2: Fallback - any lane not currently blocked
        List<int> availableLanes = new List<int>();
        for (int i = 0; i < laneCount; i++)
        {
            if (!lanes[i].isBlocked)
                availableLanes.Add(i);
        }
        
        if (availableLanes.Count > 0)
        {
            return availableLanes[Random.Range(0, availableLanes.Count)];
        }
        
        // ✅ METHOD 3: Ultimate fallback - center lane
        return laneCount / 2;
    }
    
    GameObject DecideCollectiblePrefab(CoinSpawnPattern pattern)
    {
        float roll = Random.Range(0f, 100f);
        
        // Fragment substitution?
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
            // Find free lanes
            List<int> freeLanes = new List<int>();
            for (int i = 0; i < laneCount; i++)
            {
                if (!lanes[i].isBlocked)
                    freeLanes.Add(i);
            }
            
            if (freeLanes.Count > 0)
            {
                int lane = freeLanes[Random.Range(0, freeLanes.Count)];
                Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
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
    /// <summary>
    /// ✅ CRITICAL FIX: Setup mover dengan FIXED speed (no dynamic adjustment)
    /// Prevents speed flickering
    /// </summary>
    void SetupMoverFixed(GameObject obj, float speed)
    {
        // Try planet mover
        var planetMover = obj.GetComponent<PlanetMover>();
        if (planetMover != null)
        {
            planetMover.SetFixedSpeed(speed);
            return;
        }
        
        // Try coin mover
        var coinMover = obj.GetComponent<CoinMover>();
        if (coinMover != null)
        {
            coinMover.SetFixedSpeed(speed);
            return;
        }
        
        // Try fragment mover
        var fragmentMover = obj.GetComponent<FragmentMover>();
        if (fragmentMover != null)
        {
            fragmentMover.SetFixedSpeed(speed);
            return;
        }
        
        // Fallback: add planet mover
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
        // SpeedBoost affects spawn rate (spawn faster untuk kompensasi speed increase)
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
        if (obstacleSpawnerCoroutine != null)
            StopCoroutine(obstacleSpawnerCoroutine);
        
        if (coinSpawnerCoroutine != null)
            StopCoroutine(coinSpawnerCoroutine);
        
        if (starSpawnerCoroutine != null)
            StopCoroutine(starSpawnerCoroutine);
        
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
        
        // Draw lane states
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes == null || i >= lanes.Length) continue;
            
            float x = GetLaneWorldX(i);
            Gizmos.color = lanes[i].isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(new Vector3(x, spawnY - 3, 0), new Vector3(x, spawnY + 3, 0));
        }
        
#if UNITY_EDITOR
        // Draw status
        float progress = GetFragmentProgress();
        string status = $"Obstacles: {obstacleSpawned} | Coins: {coinSpawned} | Fragments: {fragmentSpawned}\n";
        status += $"Stars: {starSpawned}/3 | Progress: {progress:P0}\n";
        status += $"Current Obstacle: {currentObstaclePattern}\n";
        status += $"Current Coin: {currentCoinPattern}";
        
        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 2f, 0),
            status,
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
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
        Debug.Log($"Current Obstacle Pattern: {currentObstaclePattern}");
        Debug.Log($"Current Coin Pattern: {currentCoinPattern}");
        Debug.Log("\n--- Lane States ---");
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes != null && i < lanes.Length)
            {
                string status = lanes[i].isBlocked ? "BLOCKED" : "FREE";
                Debug.Log($"Lane {i}: {status} | Reason: {lanes[i].blockReason}");
            }
        }
        Debug.Log("===============================================");
    }
    
    [ContextMenu("Debug: Force Spawn Obstacle")]
    void Debug_ForceSpawnObstacle()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(SpawnObstacleWave());
    }
    
    [ContextMenu("Debug: Force Spawn Coin")]
    void Debug_ForceSpawnCoin()
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
    #endregion
}