using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ✅ UPDATED: Simplified Procedural Spawner
/// - Planet: Spawn prefab obstacle (X=0, pre-arranged layout)
/// - Coin: Dynamic spawn dengan random chance (coin/star/fragment)
/// - TimeFreeze: Stop planet spawn, coin tetap spawn
/// - Star: Spawn based on fragment mission progress
/// 
/// REPLACE: Assets/Script/Level/ProceduralSpawner.cs
/// </summary>
public class ProceduralSpawner : MonoBehaviour
{
    #region REFERENCES
    [Header("🔧 REQUIRED REFERENCES")]
    public LanesManager lanesManager;
    public DifficultyManager difficultyManager;

    [Header("📦 OBSTACLE PREFABS (Pre-arranged)")]
    [Tooltip("Drag prefab obstacle yang sudah diatur layoutnya (X=0)")]
    public GameObject[] obstaclePrefabs;

    [Header("📦 COLLECTIBLE PREFABS")]
    public GameObject coinPrefab;
    public GameObject starPrefab;

    [Header("📚 DATA")]
    public FragmentPrefabRegistry fragmentRegistry;
    public LevelDatabase levelDatabase;

    [Header("🎨 COIN PATTERNS (Keep This!)")]
    [Tooltip("Pattern untuk layout coin (coin bisa berubah jadi star/fragment)")]
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();

    [Header("⚙️ SPAWN SETTINGS")]
    public Transform spawnParent;
    public float spawnY = 10f;
    public float coinVerticalSpacing = 2f;
    public float obstacleMinDistance = 8f; // Minimum jarak antar obstacle
    #endregion

    #region SPAWN TIMING
    [Header("⏱️ SPAWN TIMING")]
    [Tooltip("Obstacle spawn interval (detik)")]
    public float baseObstacleInterval = 3.0f;

    [Tooltip("Coin pattern spawn interval (detik)")]
    public float baseCoinInterval = 2.5f;
    #endregion

    #region RANDOM CHANCE
    [Header("🎲 RANDOM CHANCE (%)")]
    [Tooltip("Chance untuk spawn fragment (5-10% recommended)")]
    [Range(0f, 100f)]
    public float fragmentSpawnChance = 7f;

    [Tooltip("Chance untuk spawn star (3-5% recommended)")]
    [Range(0f, 100f)]
    public float starSpawnChance = 4f;

    [Tooltip("Star spawn multiplier based on progress (max 3x at 90% progress)")]
    public float starProgressMultiplier = 3f;
    #endregion

    #region STAR PROGRESS
    [Header("⭐ STAR SYSTEM (3 Stars Per Level)")]
    [Tooltip("Progress range untuk allow star spawn")]
    public Vector2 starProgressRange = new Vector2(0.1f, 0.95f);

    [Tooltip("Star spawn rate increase per 10% progress")]
    public AnimationCurve starProgressCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 3f);
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
    #endregion

    #region PRIVATE VARS
    private int laneCount = 3;
    private float laneOffset = 2.5f;

    private FragmentRequirement[] levelRequirements;
    private bool isInitialized = false;

    private float nextObstacleSpawnTime = 0f;
    private float nextCoinSpawnTime = 0f;

    private float lastObstacleY = 0f; // Track last obstacle spawn position

    private int totalStarsAllowed = 3;
    private int starsSpawnedCount = 0;
    #endregion

    #region INITIALIZATION
    void Awake()
    {
        SetupReferences();
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

    void Start()
    {
        if (!ValidateSetup())
        {
            LogError("❌ SETUP VALIDATION FAILED!");
            enabled = false;
            return;
        }

        LoadLevelRequirements();

        lastObstacleY = spawnY;

        nextObstacleSpawnTime = Time.time + 1.0f;
        nextCoinSpawnTime = Time.time + 1.5f;

        StartCoroutine(MasterSpawnLoop());

        isInitialized = true;
        Log("✅ PROCEDURAL SPAWNER INITIALIZED");
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            LogError("obstaclePrefabs empty! Assign obstacle prefabs.");
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

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogError("coinPatterns empty! Need at least 1 coin pattern.");
            valid = false;
        }

        if (fragmentRegistry == null)
        {
            LogError("fragmentRegistry null! Cannot spawn fragments.");
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

        if (levelRequirements == null || levelRequirements.Length == 0)
        {
            LogWarning("No level requirements found! Fragment spawn disabled.");
        }
    }
    #endregion

    #region MASTER SPAWN LOOP
    IEnumerator MasterSpawnLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            float currentTime = Time.time;

            // ✅ OBSTACLE SPAWN CHECK
            if (currentTime >= nextObstacleSpawnTime)
            {
                bool canSpawn = CanSpawnObstacle();
                if (canSpawn)
                {
                    SpawnObstacle();
                }
                else if (enableDebugLogs)
                {
                    Log("⏸️ Obstacle spawn paused (TimeFreeze active)");
                }
            }

            // ✅ COIN SPAWN CHECK (always active, even during TimeFreeze)
            if (currentTime >= nextCoinSpawnTime)
            {
                SpawnCoinPattern();
            }

            yield return null;
        }
    }

    bool CanSpawnObstacle()
    {
        // TimeFreeze blocks obstacle spawning
        if (BoosterManager.Instance != null && BoosterManager.Instance.timeFreezeActive)
        {
            return false;
        }
        return true;
    }
    #endregion

    #region OBSTACLE SPAWNING
    void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            LogError("No obstacle prefabs assigned!");
            return;
        }

        // Pick random obstacle prefab
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

        // ✅ SPAWN AT X=0 (prefab sudah diatur layoutnya)
        Vector3 spawnPos = new Vector3(0f, spawnY, 0f);
        GameObject obstacle = Instantiate(prefab, spawnPos, Quaternion.identity, spawnParent);
        obstacle.name = $"Obstacle_{obstacleSpawned}";

        // Setup mover (should be on parent obstacle prefab)
        SetupObstacleMover(obstacle);

        lastObstacleY = spawnY;
        obstacleSpawned++;
        totalSpawned++;

        // Schedule next obstacle spawn
        float currentSpeed = GetCurrentSpeed();
        float speedMod = GetSpeedModifier();
        float interval = baseObstacleInterval * speedMod;

        nextObstacleSpawnTime = Time.time + interval;

        if (enableDebugLogs)
        {
            Log($"🌍 Spawned obstacle: {prefab.name}, next in {interval:F2}s");
        }
    }

    void SetupObstacleMover(GameObject obstacle)
    {
        float currentSpeed = GetCurrentSpeed();

        // Check if mover already exists
        var mover = obstacle.GetComponent<PlanetMover>();
        if (mover != null)
        {
            mover.SetSpeed(currentSpeed);
            return;
        }

        // Add mover if not exists
        mover = obstacle.AddComponent<PlanetMover>();
        mover.SetSpeed(currentSpeed);
    }
    #endregion

    #region COIN PATTERN SPAWNING
    void SpawnCoinPattern()
    {
        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogError("No coin patterns available!");
            return;
        }

        // Select random coin pattern
        CoinSpawnPattern pattern = SelectCoinPattern();
        if (pattern == null)
        {
            LogWarning("Failed to select coin pattern!");
            nextCoinSpawnTime = Time.time + baseCoinInterval;
            return;
        }

        // Choose base lane
        int baseLane = Random.Range(0, laneCount);

        // Spawn pattern with dynamic collectible selection
        StartCoroutine(SpawnCoinPatternCoroutine(pattern, baseLane));

        // Schedule next coin spawn
        float speedMod = GetSpeedModifier();
        float delay = pattern.GetRandomDelay() * speedMod;
        nextCoinSpawnTime = Time.time + baseCoinInterval + delay;

        if (enableDebugLogs)
        {
            Log($"💰 Spawning coin pattern: {pattern.patternName}, next in {baseCoinInterval + delay:F2}s");
        }
    }

    IEnumerator SpawnCoinPatternCoroutine(CoinSpawnPattern pattern, int baseLane)
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

            // ✅ DYNAMIC COLLECTIBLE SELECTION
            GameObject prefab = DecideCollectiblePrefab();

            if (prefab != null)
            {
                float laneX = GetLaneWorldX(targetLane);
                Vector3 pos = new Vector3(laneX, currentY, 0f);
                GameObject spawned = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

                SetupCollectibleMover(spawned, currentSpeed);
                SetupCollectibleComponent(spawned, prefab);

                // Update stats
                if (prefab == coinPrefab)
                {
                    coinSpawned++;
                }
                else if (prefab == starPrefab)
                {
                    starSpawned++;
                    starsSpawnedCount++;
                }
                else
                {
                    fragmentSpawned++;
                }

                totalSpawned++;
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    CoinSpawnPattern SelectCoinPattern()
    {
        if (coinPatterns.Count == 0) return null;
        if (coinPatterns.Count == 1) return coinPatterns[0];

        // Weighted random selection
        int totalWeight = 0;
        foreach (var p in coinPatterns)
        {
            if (p != null) totalWeight += p.selectionWeight;
        }

        if (totalWeight == 0) return coinPatterns[0];

        int rand = Random.Range(0, totalWeight);
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

    /// <summary>
    /// ✅ CRITICAL: Dynamic collectible decision (coin/star/fragment)
    /// </summary>
    GameObject DecideCollectiblePrefab()
    {
        float progress = GetFragmentProgress();
        float roll = Random.Range(0f, 100f);

        // ✅ CHECK 1: Star spawn (progress-based chance)
        if (CanSpawnStar(progress))
        {
            float adjustedStarChance = GetAdjustedStarChance(progress);

            if (roll < adjustedStarChance)
            {
                if (enableDebugLogs)
                {
                    Log($"⭐ Spawning STAR (roll: {roll:F1}%, chance: {adjustedStarChance:F1}%, progress: {progress:P0})");
                }
                return starPrefab;
            }

            roll -= adjustedStarChance; // Adjust roll for next check
        }

        // ✅ CHECK 2: Fragment spawn
        if (levelRequirements != null && levelRequirements.Length > 0)
        {
            if (roll < fragmentSpawnChance)
            {
                GameObject fragmentPrefab = GetRequiredFragmentPrefab();
                if (fragmentPrefab != null)
                {
                    if (enableDebugLogs)
                    {
                        Log($"🔷 Spawning FRAGMENT (roll: {roll:F1}%, chance: {fragmentSpawnChance:F1}%)");
                    }
                    return fragmentPrefab;
                }
            }
        }

        // ✅ DEFAULT: Coin
        return coinPrefab;
    }

    bool CanSpawnStar(float progress)
    {
        // Max 3 stars per level
        if (starsSpawnedCount >= totalStarsAllowed)
            return false;

        // Progress must be within range
        if (progress < starProgressRange.x || progress > starProgressRange.y)
            return false;

        return true;
    }

    float GetAdjustedStarChance(float progress)
    {
        // Star spawn chance increases with progress
        float progressNormalized = Mathf.InverseLerp(starProgressRange.x, starProgressRange.y, progress);
        float multiplier = starProgressCurve.Evaluate(progressNormalized);

        return starSpawnChance * multiplier;
    }

    GameObject GetRequiredFragmentPrefab()
    {
        if (levelRequirements == null || levelRequirements.Length == 0) return null;
        if (fragmentRegistry == null) return null;

        // Pick random requirement
        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];
        return fragmentRegistry.GetPrefab(req.type, req.colorVariant);
    }

    void SetupCollectibleMover(GameObject collectible, float speed)
    {
        // Try existing mover components
        var coinMover = collectible.GetComponent<CoinMover>();
        if (coinMover != null)
        {
            coinMover.SetSpeed(speed);
            return;
        }

        var fragmentMover = collectible.GetComponent<FragmentMover>();
        if (fragmentMover != null)
        {
            fragmentMover.SetSpeed(speed);
            return;
        }

        // Fallback: add CoinMover
        coinMover = collectible.AddComponent<CoinMover>();
        coinMover.SetSpeed(speed);
    }

    void SetupCollectibleComponent(GameObject spawned, GameObject originalPrefab)
    {
        // Skip if coin or star
        if (originalPrefab == coinPrefab || originalPrefab == starPrefab)
            return;

        // Setup fragment collectible
        var collectible = spawned.GetComponent<FragmentCollectible>();
        if (collectible == null)
            collectible = spawned.AddComponent<FragmentCollectible>();

        // Find matching requirement
        if (levelRequirements != null && fragmentRegistry != null)
        {
            foreach (var req in levelRequirements)
            {
                var prefab = fragmentRegistry.GetPrefab(req.type, req.colorVariant);
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
            return 0.7f; // Spawn faster during speed boost
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

        // Draw lanes
        for (int i = 0; i < laneCount; i++)
        {
            float x = GetLaneWorldX(i);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(x, spawnY - 15, 0), new Vector3(x, spawnY + 2, 0));
        }

#if UNITY_EDITOR
        // Draw status text
        float progress = GetFragmentProgress();
        string status = $"Total: {totalSpawned} | Obstacles: {obstacleSpawned}\n";
        status += $"Coins: {coinSpawned} | Fragments: {fragmentSpawned} | Stars: {starsSpawnedCount}/3\n";
        status += $"Progress: {progress:P0}";

        if (BoosterManager.Instance != null)
        {
            if (BoosterManager.Instance.timeFreezeActive)
                status += "\n⏸️ TIME FREEZE";
            if (BoosterManager.Instance.speedBoostActive)
                status += "\n⚡ SPEED BOOST";
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
        Debug.Log($"Obstacles: {obstacleSpawned} | Coins: {coinSpawned}");
        Debug.Log($"Fragments: {fragmentSpawned} | Stars: {starsSpawnedCount}/3");
        Debug.Log($"Fragment Progress: {GetFragmentProgress():P0}");
        Debug.Log($"Current Speed: {GetCurrentSpeed():F2}");
        Debug.Log($"Speed Modifier: {GetSpeedModifier():F2}");
        Debug.Log("===============================================");
    }

    [ContextMenu("⭐ Debug: Check Star Spawn Chance")]
    void Debug_CheckStarChance()
    {
        float progress = GetFragmentProgress();
        float chance = GetAdjustedStarChance(progress);
        bool canSpawn = CanSpawnStar(progress);

        Debug.Log("========== STAR SPAWN CHANCE ==========");
        Debug.Log($"Progress: {progress:P0}");
        Debug.Log($"Base Chance: {starSpawnChance}%");
        Debug.Log($"Adjusted Chance: {chance:F2}%");
        Debug.Log($"Can Spawn: {canSpawn}");
        Debug.Log($"Stars Spawned: {starsSpawnedCount}/{totalStarsAllowed}");
        Debug.Log("========================================");
    }
    #endregion
}