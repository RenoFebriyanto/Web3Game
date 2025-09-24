using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    public int laneCount = 3;
    public float spawnY = 12f;
    public float spawnIntervalSeconds = 1.2f;
    public GameObject planetPrefab;
    public float worldScrollSpeed = 3f;
    public float coinSpawnProbability = 0.6f; // 60% planet spawn memicu coin spawn
    public CoinSpawner coinSpawner;

    float timer = 0f;

    void Start()
    {
        if (planetPrefab == null) Debug.LogError("planetPrefab null");
        if (coinSpawner == null) Debug.LogWarning("coinSpawner null - no coins will spawn");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnIntervalSeconds)
        {
            timer = 0;
            SpawnPlanet();
        }
    }

    void SpawnPlanet()
    {
        int laneIndex = Random.Range(0, laneCount);
        float x = LanesManager.Instance.LaneToWorldX(laneIndex);
        Vector3 pos = new Vector3(x, spawnY, 0);
        var go = Instantiate(planetPrefab, pos, Quaternion.identity);
        var mover = go.GetComponent<PlanetMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);

        // maybe spawn coins tied to lane occasionally
        if (coinSpawner != null && Random.value <= coinSpawnProbability)
        {
            coinSpawner.SpawnPatternAtLane(laneIndex);
        }
    }
}
