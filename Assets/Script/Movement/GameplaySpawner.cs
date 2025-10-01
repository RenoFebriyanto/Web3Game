using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Gameplay spawner: planets, coin-trains (spawned sequentially), fragments, boosters.
/// - Spawn coin trains as sequential single coin spawns so they form a visible train.
/// - Avoid spawning coins inside planets (Overlap checks).
/// - Use fragment registry or fallback arrays (assign in Inspector).
/// </summary>
public class GameplaySpawner : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    public LanesManager lanesManager;
    public Transform spawnParent;
    public LayerMask obstacleLayerMask;

    [Header("World / movement")]
    public float spawnY = 10f;
    public float worldScrollSpeed = 3f;

    [Header("Prefabs / registry (assign at least fallback arrays)")]
    public PlanetPrefabRegistry planetRegistry; // optional registry
    public GameObject[] planetPrefabs;
    public ScriptableObject fragmentRegistrySO; // optional (reflection read)
    public GameObject[] fragmentPrefabs;
    public GameObject[] boosterPrefabs;

    [Header("Coins (train behavior)")]
    public GameObject coinPrefab;
    [Tooltip("World spacing between consecutive coins in a train (units)")]
    public float coinSpacing = 0.8f;   // world spacing between coins
    [Tooltip("Chance to spawn a multi-coin train instead of single coin (0..1)")]
    public float coinTrainChance = 0.55f;
    public int coinTrainMin = 4;
    public int coinTrainMax = 7;

    [Header("Intervals")]
    public float planetInterval = 2.4f;
    public float coinTickInterval = 0.6f; // how often spawner decides to spawn (single or train)
    public float boosterInterval = 8f;

    [Header("Spacing & safety")]
    public float minSpacingWorld = 3.0f;
    public float overlapRadius = 0.45f;
    public float minDistanceFromPlanetForCoin = 1.0f;

    [Header("Probability")]
    [Range(0f, 1f)] public float fragmentChance = 0.14f;
    [Range(0f, 1f)] public float fragmentOnPlanetChance = 0.35f;
    [Range(0f, 1f)] public float boosterChance = 0.12f;

    // internals
    int laneCount = 3;
    float laneOffset = 2.5f;
    float[] lastSpawnTimePerLane;
    float[] laneBlockedUntil;
    float fragmentGlobalCooldown = 0f;
    float fragmentCooldownSeconds = 0.4f;

    void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (lanesManager == null) lanesManager = Object.FindFirstObjectByType<LanesManager>();
#else
        if (lanesManager == null) lanesManager = Object.FindObjectOfType<LanesManager>();
#endif
        if (lanesManager != null) { laneCount = Mathf.Max(1, lanesManager.laneCount); laneOffset = lanesManager.laneOffset; }

        lastSpawnTimePerLane = new float[laneCount];
        laneBlockedUntil = new float[laneCount];
        for (int i = 0; i < laneCount; i++) { lastSpawnTimePerLane[i] = -9999f; laneBlockedUntil[i] = -9999f; }

        if (spawnParent == null) spawnParent = transform;
    }

    void Start()
    {
        StartCoroutine(PlanetSpawnLoop());
        StartCoroutine(CoinTickLoop());
        StartCoroutine(BoosterSpawnLoop());
    }

    IEnumerator PlanetSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(planetInterval);
            TrySpawnPlanet();
        }
    }

    IEnumerator CoinTickLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(coinTickInterval);
            TrySpawnCoinOrTrainOrFragment();
        }
    }

    IEnumerator BoosterSpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(boosterInterval);
            TrySpawnBooster();
        }
    }

    void TrySpawnPlanet()
    {
        List<int> candidates = GetAvailableLanesBySpacing();
        if (candidates.Count == 0) return;
        int lane = candidates[Random.Range(0, candidates.Count)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);

        if (Physics2D.OverlapCircle(pos, overlapRadius, obstacleLayerMask)) return;

        GameObject prefab = GetRandomPlanetPrefab();
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
        var mover = go.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);

        lastSpawnTimePerLane[lane] = Time.time;

        // temporarily block center lanes around planet a bit (avoid coin spawn immediate)
        float blockT = 0.6f;
        for (int i = -1; i <= 1; i++)
        {
            int idx = lane + i;
            if (idx >= 0 && idx < laneCount) laneBlockedUntil[idx] = Time.time + blockT;
        }

        if (HasAnyFragmentPrefabs() && Random.value <= fragmentOnPlanetChance)
        {
            SpawnFragmentClusterNearLane(lane, pos.y - 1.2f);
        }
    }

    void TrySpawnCoinOrTrainOrFragment()
    {
        if (Time.time < fragmentGlobalCooldown) return;

        // choose lane
        List<int> cand = GetAvailableLanesBySpacingAndNotBlocked();
        if (cand.Count == 0) return;

        // pick center-preference
        int lane = ChooseLaneForCoin(cand);

        // decide train vs single vs fragment
        if (HasAnyFragmentPrefabs() && Random.value < fragmentChance)
        {
            // single fragment
            SpawnSingleFragment(lane, spawnY - 0.5f);
            fragmentGlobalCooldown = Time.time + fragmentCooldownSeconds;
            lastSpawnTimePerLane[lane] = Time.time;
            return;
        }

        if (Random.value < coinTrainChance)
        {
            int count = Random.Range(coinTrainMin, coinTrainMax + 1);
            StartCoroutine(SpawnCoinTrainCoroutine(lane, count));
            lastSpawnTimePerLane[lane] = Time.time;
            return;
        }

        // fallback single coin
        SpawnSingleCoin(lane, spawnY);
        lastSpawnTimePerLane[lane] = Time.time;
    }

    void TrySpawnBooster()
    {
        if (boosterPrefabs == null || boosterPrefabs.Length == 0) return;
        if (Random.value > boosterChance) return;

        List<int> candidates = GetAvailableLanesBySpacing();
        if (candidates.Count == 0) return;
        int lane = candidates[Random.Range(0, candidates.Count)];
        Vector3 pos = new Vector3(GetLaneWorldX(lane), spawnY, 0f);
        if (Physics2D.OverlapCircle(pos, overlapRadius, obstacleLayerMask)) return;

        GameObject prefab = GetRandomFromArray(boosterPrefabs);
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
        var mover = go.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);

        lastSpawnTimePerLane[lane] = Time.time;
    }

    IEnumerator SpawnCoinTrainCoroutine(int lane, int count)
    {
        // per-coin delay chosen so spacing in world units = coinSpacing while moving
        float perCoinDelay = (worldScrollSpeed > 0.001f) ? (coinSpacing / worldScrollSpeed) : 0.12f;
        float x = GetLaneWorldX(lane);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(x, spawnY - i * coinSpacing, 0f);

            // avoid placing if overlapping planet; try small downward shift or skip
            if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask))
            {
                pos.y -= coinSpacing;
                if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask))
                {
                    // skip this coin to avoid overlap
                    yield return new WaitForSeconds(perCoinDelay);
                    continue;
                }
            }

            GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity, spawnParent);
            var mover = coin.GetComponent<PlanetMover>();
            if (mover != null) mover.SetSpeed(worldScrollSpeed);

            yield return new WaitForSeconds(perCoinDelay);
        }
    }

    void SpawnSingleCoin(int lane, float startY)
    {
        if (coinPrefab == null) return;
        Vector3 pos = new Vector3(GetLaneWorldX(lane), startY, 0f);

        Collider2D planetNear = Physics2D.OverlapCircle(pos, minDistanceFromPlanetForCoin, obstacleLayerMask);
        if (planetNear != null) pos.y -= (minDistanceFromPlanetForCoin + coinSpacing);

        if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask))
        {
            pos.y -= coinSpacing;
            if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask)) return;
        }

        GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity, spawnParent);
        var mover = coin.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);
    }

    void SpawnSingleFragment(int lane, float startY)
    {
        // Pilih random requirement dari level
        if (levelRequirements == null || levelRequirements.Length == 0) return;

        var req = levelRequirements[Random.Range(0, levelRequirements.Length)];

        // Ambil prefab dari registry
        if (fragmentRegistrySO == null) return;
        var registry = fragmentRegistrySO as FragmentPrefabRegistry;
        if (registry == null) return;

        GameObject prefab = registry.GetPrefab(req.type, req.colorVariant);
        if (prefab == null) return;

        // Spawn position
        Vector3 pos = new Vector3(GetLaneWorldX(lane), startY, 0f);
        if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask))
        {
            pos.y -= coinSpacing;
            if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask)) return;
        }

        // Instantiate
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

        // PENTING: Set type dan variant
        var collectible = go.GetComponent<FragmentCollectible>();
        if (collectible == null)
        {
            collectible = go.AddComponent<FragmentCollectible>();
        }
        collectible.Initialize(req.type, req.colorVariant);

        // Set movement
        var mover = go.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);
    }

    void SpawnFragmentClusterNearLane(int lane, float startY)
    {
        if (levelRequirements == null || levelRequirements.Length == 0) return;

        int clusterCount = Random.Range(2, 4);
        float x = GetLaneWorldX(lane);

        for (int i = 0; i < clusterCount; i++)
        {
            // Random requirement
            var req = levelRequirements[Random.Range(0, levelRequirements.Length)];

            if (fragmentRegistrySO == null) continue;
            var registry = fragmentRegistrySO as FragmentPrefabRegistry;
            if (registry == null) continue;

            GameObject prefab = registry.GetPrefab(req.type, req.colorVariant);
            if (prefab == null) continue;

            Vector3 pos = new Vector3(
                x + Random.Range(-0.4f, 0.4f),
                startY - i * 0.6f - Random.Range(0f, 0.1f),
                0f
            );

            if (Physics2D.OverlapCircle(pos, 0.35f, obstacleLayerMask)) continue;

            GameObject go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

            // Set type dan variant
            var collectible = go.GetComponent<FragmentCollectible>();
            if (collectible == null)
            {
                collectible = go.AddComponent<FragmentCollectible>();
            }
            collectible.Initialize(req.type, req.colorVariant);

            var mover = go.GetComponent<PlanetMover>();
            if (mover != null) mover.SetSpeed(worldScrollSpeed);
        }
    }

    // helpers for lanes / availability
    int ChooseLaneForCoin(List<int> cand)
    {
        int center = laneCount / 2;
        if (cand.Contains(center)) return center;
        return cand[Random.Range(0, cand.Count)];
    }

    List<int> GetAvailableLanesBySpacingAndNotBlocked()
    {
        List<int> outList = new List<int>();
        for (int i = 0; i < laneCount; i++)
            if (Time.time - lastSpawnTimePerLane[i] >= (minSpacingWorld / Mathf.Max(0.0001f, worldScrollSpeed)) && Time.time >= laneBlockedUntil[i])
                outList.Add(i);
        return outList;
    }

    List<int> GetAvailableLanesBySpacing()
    {
        List<int> outList = new List<int>();
        for (int i = 0; i < laneCount; i++)
            if (Time.time - lastSpawnTimePerLane[i] >= (minSpacingWorld / Mathf.Max(0.0001f, worldScrollSpeed)))
                outList.Add(i);
        return outList;
    }

    float GetLaneWorldX(int laneIndex)
    {
        float centerLane = (laneCount - 1) / 2f;
        float centerX = (lanesManager != null) ? lanesManager.transform.position.x : 0f;
        float offset = (lanesManager != null) ? lanesManager.laneOffset : laneOffset;
        return centerX + (laneIndex - centerLane) * offset;
    }

    // random helpers / registries
    T GetRandomFromArray<T>(T[] arr) where T : Object
    {
        if (arr == null || arr.Length == 0) return null;
        if (arr.Length == 1) return arr[0];
        return arr[Random.Range(0, arr.Length)];
    }

    GameObject GetRandomPlanetPrefab()
    {
        if (planetRegistry != null)
        {
            var p = planetRegistry.GetRandomPrefab();
            if (p != null) return p;
        }
        return GetRandomFromArray(planetPrefabs);
    }

    GameObject GetRandomFragmentPrefab()
    {
        // try registry (ScriptableObject) via reflection if provided
        if (fragmentRegistrySO != null)
        {
            MethodInfo mi = fragmentRegistrySO.GetType().GetMethod("GetRandomPrefab", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                var val = mi.Invoke(fragmentRegistrySO, null) as GameObject;
                if (val != null) return val;
            }

            FieldInfo fi = fragmentRegistrySO.GetType().GetField("prefabs", BindingFlags.Instance | BindingFlags.Public);
            if (fi != null)
            {
                var arr = fi.GetValue(fragmentRegistrySO) as GameObject[];
                if (arr != null && arr.Length > 0) return arr[Random.Range(0, arr.Length)];
            }
        }
        return GetRandomFromArray(fragmentPrefabs);
    }

    bool HasAnyFragmentPrefabs()
    {
        if (fragmentRegistrySO != null) return true;
        return (fragmentPrefabs != null && fragmentPrefabs.Length > 0);
    }
}
