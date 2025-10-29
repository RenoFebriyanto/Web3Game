using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FIXED VERSION - Simple, reliable spawner with proper debugging
/// REPLACE: Assets/Script/Movement/GameplaySpawner.cs
/// 
/// CRITICAL SETUP (Check Inspector):
/// 1. Assign ALL prefabs (planet, coin, star)
/// 2. Assign FragmentRegistry (drag from Assets/Script/Level/Config/FragmenRegist/)
/// 3. Assign LevelDatabase (drag from Assets/Script/Level/)
/// 4. Assign CoinPatterns (drag all patterns from Assets/Script/Level/ConfigPaternCoin/)
/// 5. LanesManager & DifficultyManager will auto-find (must exist in scene)
/// </summary>
public class FixedGameplaySpawner : MonoBehaviour
{
    [Header("🔧 REQUIRED SETUP - Check These!")]
    [Tooltip("Drag LanesManager from scene (will auto-find if null)")]
    public LanesManager lanesManager;

    [Tooltip("Drag DifficultyManager from scene (will auto-find if null)")]
    public DifficultyManager difficultyManager;

    [Header("📦 PREFABS (Drag from Project)")]
    [Tooltip("Planet prefabs array - drag all planet prefabs here")]
    public GameObject[] planetPrefabs;

    [Tooltip("Coin prefab - drag Coin prefab here")]
    public GameObject coinPrefab;

    [Tooltip("Star prefab - drag Star prefab here")]
    public GameObject starPrefab;

    [Header("📚 DATA ASSETS")]
    [Tooltip("FragmentPrefabRegistry - drag from Assets/Script/Level/Config/FragmenRegist/")]
    public FragmentPrefabRegistry fragmentRegistry;

    [Tooltip("LevelDatabase - drag from Assets/Script/Level/")]
    public LevelDatabase levelDatabase;

    [Header("🎨 COIN PATTERNS")]
    [Tooltip("Drag ALL coin patterns from Assets/Script/Level/ConfigPaternCoin/")]
    public List<CoinSpawnPattern> coinPatterns = new List<CoinSpawnPattern>();

    [Header("⚙️ SPAWN SETTINGS")]
    public Transform spawnParent;
    public float spawnY = 10f;
    public float coinVerticalSpacing = 2;
    public float planetInterval = 2.5f;
    public float itemSpawnDelay = 0.15f;
    public float minSpawnDistance = 2f;

    [Header("🎯 DEBUG (Check this in Play Mode!)")]
    public bool enableDebugLogs = true;
    public bool showGizmos = true;

    [Header("📊 RUNTIME STATUS (Read-Only)")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private int totalSpawned = 0;
    [SerializeField] private int coinSpawned = 0;
    [SerializeField] private int planetSpawned = 0;
    [SerializeField] private int fragmentSpawned = 0;

    // Runtime
    private int laneCount = 3;
    private float laneOffset = 2.5f;
    private float[] laneBlockedUntil;
    private float[] lastSpawnYInLane;
    private FragmentRequirement[] levelRequirements;
    private int totalPatternWeight = 0;

    void Awake()
    {
        Log("=== SPAWNER AWAKE - Starting Setup ===");

        // Auto-find references
        if (lanesManager == null)
        {
            lanesManager = FindFirstObjectByType<LanesManager>();
            Log($"LanesManager auto-find: {(lanesManager != null ? "SUCCESS" : "FAILED!")}");
        }

        if (difficultyManager == null)
        {
            difficultyManager = FindFirstObjectByType<DifficultyManager>();
            Log($"DifficultyManager auto-find: {(difficultyManager != null ? "SUCCESS" : "FAILED!")}");
        }

        // Setup lanes
        if (lanesManager != null)
        {
            laneCount = lanesManager.laneCount;
            laneOffset = lanesManager.laneOffset;
            Log($"Lanes configured: count={laneCount}, offset={laneOffset}");
        }
        else
        {
            LogError("LanesManager NOT FOUND! Using default values.");
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
            Log("spawnParent set to self");
        }

        BuildWeightedPatternList();
    }

    void Start()
    {
        Log("=== SPAWNER START - Running Validation ===");

        // Validate setup
        if (!ValidateSetup())
        {
            LogError("SPAWNER VALIDATION FAILED! Check Inspector assignments!");
            return;
        }

        LoadLevelRequirements();

        // Start spawn loops
        Log("Starting spawn coroutines...");
        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(PatternSpawnLoop());

        isInitialized = true;
        Log("=== SPAWNER INITIALIZED SUCCESSFULLY ===");
    }

    bool ValidateSetup()
    {
        bool valid = true;

        // Check prefabs
        if (planetPrefabs == null || planetPrefabs.Length == 0)
        {
            LogError("❌ planetPrefabs array is EMPTY! Drag planet prefabs in Inspector!");
            valid = false;
        }
        else
        {
            Log($"✓ Planet prefabs: {planetPrefabs.Length} assigned");
        }

        if (coinPrefab == null)
        {
            LogError("❌ coinPrefab is NULL! Drag coin prefab in Inspector!");
            valid = false;
        }
        else
        {
            Log($"✓ Coin prefab: {coinPrefab.name}");
        }

        if (starPrefab == null)
        {
            LogWarning("⚠️ starPrefab is NULL (optional, but recommended)");
        }
        else
        {
            Log($"✓ Star prefab: {starPrefab.name}");
        }

        // Check data assets
        if (fragmentRegistry == null)
        {
            LogWarning("⚠️ fragmentRegistry is NULL - fragments won't spawn");
        }
        else
        {
            Log($"✓ Fragment registry assigned");
        }

        if (levelDatabase == null)
        {
            LogWarning("⚠️ levelDatabase is NULL - using fallback spawning");
        }
        else
        {
            Log($"✓ Level database assigned");
        }

        // Check patterns
        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogError("❌ coinPatterns list is EMPTY! Drag coin patterns in Inspector!");
            valid = false;
        }
        else
        {
            Log($"✓ Coin patterns: {coinPatterns.Count} assigned");
        }

        // Check managers
        if (lanesManager == null)
        {
            LogError("❌ LanesManager NOT FOUND in scene! Add LanesManager to scene!");
            valid = false;
        }

        if (difficultyManager == null)
        {
            LogWarning("⚠️ DifficultyManager NOT FOUND - using default speed");
        }

        return valid;
    }

    void LoadLevelRequirements()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        Log($"Loading requirements for level: {levelId}");

        if (levelDatabase != null)
        {
            LevelConfig config = levelDatabase.GetById(levelId);
            if (config != null && config.requirements != null)
            {
                levelRequirements = config.requirements.ToArray();
                Log($"✓ Loaded {levelRequirements.Length} requirements from {levelId}");
            }
            else
            {
                LogWarning($"Level config not found for {levelId}");
            }
        }
    }

    void BuildWeightedPatternList()
    {
        totalPatternWeight = 0;

        if (coinPatterns == null || coinPatterns.Count == 0)
        {
            LogWarning("No coin patterns assigned!");
            return;
        }

        foreach (var pattern in coinPatterns)
        {
            if (pattern == null)
            {
                LogWarning("Null pattern in list, skipping");
                continue;
            }
            totalPatternWeight += pattern.selectionWeight;
        }

        Log($"Pattern system ready: {coinPatterns.Count} patterns, total weight: {totalPatternWeight}");
    }

    // ========================================
    // PLANET SPAWNER
    // ========================================

    IEnumerator PlanetSpawnLoop()
    {
        Log("Planet spawn loop started");
        yield return new WaitForSeconds(2f);

        // ✅ TAMBAHKAN LOOP COUNTER:
        int loopCount = 0;
        while (true)
        {
            loopCount++;
            yield return new WaitForSeconds(planetInterval);

            List<int> availableLanes = GetAvailableLanes();

            // ✅ TAMBAHKAN DEBUG LOG:
            if (loopCount <= 3 || loopCount % 10 == 0)
            {
                Log($"PlanetLoop #{loopCount}: Available lanes: {availableLanes.Count}/{laneCount}", false);
            }

            if (availableLanes.Count > 0)
            {
                int lane = availableLanes[Random.Range(0, availableLanes.Count)];
                if (IsLaneClearForSpawn(lane))
                {
                    SpawnPlanet(lane);
                }
                else if (loopCount <= 5)
                {
                    Log($"PlanetLoop #{loopCount}: Lane {lane} not clear for spawn", false);
                }
            }
            else if (loopCount <= 5)
            {
                Log($"PlanetLoop #{loopCount}: No available lanes (all blocked)", false);
            }
        }
    }

    void SpawnPlanet(int lane)
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0)
        {
            LogError("Cannot spawn planet: planetPrefabs array is empty!");
            return;
        }

        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        GameObject planet = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

        float currentSpeed = GetCurrentSpeed();
        SetupMover(planet, currentSpeed);

        lastSpawnYInLane[lane] = spawnY;
        laneBlockedUntil[lane] = Time.time + 3f;

        planetSpawned++;
        totalSpawned++;

        Log($"✓ Spawned planet '{prefab.name}' in lane {lane} at y={spawnY:F1} (speed={currentSpeed:F1})", false);
    }

    // ========================================
    // PATTERN SPAWNER
    // ========================================

    IEnumerator PatternSpawnLoop()
    {
        Log("Pattern spawn loop started");
        yield return new WaitForSeconds(3f);

        while (true)
        {
            List<int> availableLanes = GetAvailableLanes();
            if (availableLanes.Count == 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            int baseLane = availableLanes[Random.Range(0, availableLanes.Count)];

            CoinSpawnPattern pattern = SelectRandomPattern();
            if (pattern == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (!IsLaneClearForSpawn(baseLane))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // ✅ CODE BARU (BLOCK SEMUA LANES YANG DIPAKAI):
            // Hitung lanes yang akan dipakai pattern ini
            HashSet<int> usedLanes = new HashSet<int>();
            usedLanes.Add(baseLane);

            foreach (var point in pattern.spawnPoints)
            {
                int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);
                usedLanes.Add(targetLane);
            }

            // Block SEMUA lanes yang dipakai
            float patternDuration = CalculatePatternDuration(pattern);
            float blockUntil = Time.time + patternDuration + 1f;

            foreach (int lane in usedLanes)
            {
                laneBlockedUntil[lane] = blockUntil;
            }

            yield return StartCoroutine(SpawnPattern(pattern, baseLane));

            float delay = pattern.GetRandomDelay();
            Log($"✓ Pattern '{pattern.patternName}' complete. Next in {delay:F1}s", false);

            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator SpawnPattern(CoinSpawnPattern pattern, int baseLane)
    {
        if (pattern == null || pattern.spawnPoints == null || pattern.spawnPoints.Count == 0)
        {
            LogWarning("Invalid pattern!");
            yield break;
        }

        float currentY = spawnY;
        float currentSpeed = GetCurrentSpeed();

        float minY = -2f;

        Log($"Spawning pattern '{pattern.patternName}' with {pattern.spawnPoints.Count} items in lane {baseLane}", false);

        for (int i = 0; i < pattern.spawnPoints.Count; i++)
        {
            

            Vector2 point = pattern.spawnPoints[i];
            int targetLane = Mathf.Clamp(baseLane + Mathf.RoundToInt(point.x), 0, laneCount - 1);
            currentY -= point.y * coinVerticalSpacing;

            if (currentY < minY)
            {
                Log($"Pattern stopped at item {i}/{pattern.spawnPoints.Count} - Y too low ({currentY:F1})", false);
                break;  // Stop spawning jika terlalu rendah
            }

            // langsung spawn tanpa check Y
            GameObject prefabToSpawn = DecideSpawnItem(pattern);

            if (prefabToSpawn != null)
            {
                Vector3 pos = new Vector3(GetLaneWorldX(targetLane), currentY, 0f);
                GameObject spawned = Instantiate(prefabToSpawn, pos, Quaternion.identity, spawnParent);

                SetupMover(spawned, currentSpeed);
                SetupCollectibleComponent(spawned, prefabToSpawn);

                lastSpawnYInLane[targetLane] = currentY;

                // Count spawned items
                if (prefabToSpawn == coinPrefab) coinSpawned++;
                else if (prefabToSpawn != coinPrefab && prefabToSpawn != starPrefab) fragmentSpawned++;

                totalSpawned++;
            }

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
            {
                Log("Rolled STAR", false);
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
                Log("Rolled FRAGMENT", false);
                return fragmentPrefab;
            }
        }

        // Default: Coin
        Log("Rolled COIN", false);
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
        if (originalPrefab == coinPrefab || originalPrefab == starPrefab)
            return;

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

    // ========================================
    // MOVER SETUP
    // ========================================

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

    // ========================================
    // HELPER METHODS
    // ========================================

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
        // ✅ CODE BARU (WITH SAFETY CHECK + DEBUG):
        if (lane < 0 || lane >= laneCount) return false;

        float lastY = lastSpawnYInLane[lane];
        float distance = spawnY - lastY;
        bool isClear = distance >= minSpawnDistance;

        // Debug log untuk troubleshooting
        if (!isClear && enableDebugLogs)
        {
            Log($"Lane {lane} NOT clear: distance={distance:F1}, need={minSpawnDistance}, lastY={lastY:F1}, spawnY={spawnY:F1}", false);
        }

        return isClear;
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

    float CalculatePatternDuration(CoinSpawnPattern pattern)
    {
        if (pattern == null || pattern.spawnPoints == null)
            return 1f;
        // ✅ CODE BARU (HITUNG SPAWN + CLEAR TIME):
        float spawnTime = pattern.spawnPoints.Count * itemSpawnDelay;

        // Hitung total vertical distance
        float totalVerticalDistance = 0f;
        foreach (var point in pattern.spawnPoints)
        {
            totalVerticalDistance += point.y * coinVerticalSpacing;
        }

        // Waktu untuk clear dari spawn point
        float currentSpeed = GetCurrentSpeed();
        float clearTime = totalVerticalDistance / currentSpeed;

        return spawnTime + clearTime + 1f;
    }

    // ========================================
    // DEBUG LOGGING
    // ========================================

    void Log(string msg, bool alwaysShow = true)
    {
        if (enableDebugLogs || alwaysShow)
            Debug.Log($"[Spawner] {msg}");
    }

    void LogWarning(string msg)
    {
        Debug.LogWarning($"[Spawner] ⚠️ {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[Spawner] ❌ {msg}");
    }

    // ========================================
    // GIZMOS
    // ========================================

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        // Draw spawn line
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, spawnY, 0), new Vector3(10, spawnY, 0));

        // Draw lane positions
        for (int i = 0; i < laneCount; i++)
        {
            float x = GetLaneWorldX(i);
            Gizmos.color = Time.time >= laneBlockedUntil[i] ? Color.green : Color.red;
            Gizmos.DrawLine(new Vector3(x, spawnY - 2, 0), new Vector3(x, spawnY + 2, 0));
        }

        // Draw status text in Scene view
        UnityEditor.Handles.Label(
            new Vector3(0, spawnY + 1, 0),
            $"Spawner: {totalSpawned} total\nCoins: {coinSpawned} | Planets: {planetSpawned} | Fragments: {fragmentSpawned}",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
        );
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("🔍 Debug: Print Setup Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("========================================");
        Debug.Log("  SPAWNER SETUP STATUS");
        Debug.Log("========================================");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Total Spawned: {totalSpawned}");
        Debug.Log($"  - Coins: {coinSpawned}");
        Debug.Log($"  - Planets: {planetSpawned}");
        Debug.Log($"  - Fragments: {fragmentSpawned}");
        Debug.Log($"\nManagers:");
        Debug.Log($"  - LanesManager: {(lanesManager != null ? "✓" : "✗")}");
        Debug.Log($"  - DifficultyManager: {(difficultyManager != null ? "✓" : "✗")}");
        Debug.Log($"\nPrefabs:");
        Debug.Log($"  - Planets: {(planetPrefabs != null ? planetPrefabs.Length : 0)}");
        Debug.Log($"  - Coin: {(coinPrefab != null ? "✓" : "✗")}");
        Debug.Log($"  - Star: {(starPrefab != null ? "✓" : "✗")}");
        Debug.Log($"\nData:");
        Debug.Log($"  - Fragment Registry: {(fragmentRegistry != null ? "✓" : "✗")}");
        Debug.Log($"  - Level Database: {(levelDatabase != null ? "✓" : "✗")}");
        Debug.Log($"  - Coin Patterns: {(coinPatterns != null ? coinPatterns.Count : 0)}");
        Debug.Log("========================================");
    }

    [ContextMenu("🧪 Test: Spawn 1 Coin")]
    void Debug_SpawnTestCoin()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode!");
            return;
        }

        if (coinPrefab == null)
        {
            LogError("Coin prefab not assigned!");
            return;
        }

        Vector3 pos = new Vector3(0, spawnY, 0);
        GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity, spawnParent);
        SetupMover(coin, GetCurrentSpeed());
        Log("✓ Test coin spawned at center");
    }

    [ContextMenu("🧪 Test: Spawn 1 Planet")]
    void Debug_SpawnTestPlanet()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode!");
            return;
        }

        if (planetPrefabs == null || planetPrefabs.Length == 0)
        {
            LogError("Planet prefabs not assigned!");
            return;
        }

        Vector3 pos = new Vector3(0, spawnY, 0);
        GameObject planet = Instantiate(planetPrefabs[0], pos, Quaternion.identity, spawnParent);
        SetupMover(planet, GetCurrentSpeed());
        Log("✓ Test planet spawned at center");
    }

    [ContextMenu("🔄 Debug: Reset Lanes (Unblock All)")]
    void Debug_ResetLanes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode!");
            return;
        }

        for (int i = 0; i < laneCount; i++)
        {
            laneBlockedUntil[i] = 0f;
            lastSpawnYInLane[i] = spawnY - minSpawnDistance - 5f;
        }

        Debug.Log("[DEBUG] All lanes reset and unblocked!");
    }
}