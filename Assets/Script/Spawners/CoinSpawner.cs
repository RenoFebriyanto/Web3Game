using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public float spawnY = 12f;
    public float coinSpacing = 0.6f;
    public int maxCoinsInPattern = 7;
    public float worldScrollSpeed = 3f;

    [Header("pattern weights (higher = more likely)")]
    public int weightColumn = 3;
    public int weightV = 3;
    public int weightDiagonal = 2;
    public int weightZigzag = 1;
    public int weightCluster = 1;

    System.Random rng = new System.Random();

    void Start()
    {
        if (coinPrefab == null) Debug.LogError("[CoinSpawner] coinPrefab missing");
    }

    // Public API: spawn a pattern anchored to a lane center.
    public void SpawnPatternAtLane(int laneIndex)
    {
        int pattern = PickPattern();
        switch (pattern)
        {
            case 0: SpawnColumn(laneIndex); break;
            case 1: SpawnV(laneIndex); break;
            case 2: SpawnDiagonal(laneIndex, rng.Next(0, 2) == 0); break;
            case 3: SpawnZigZag(laneIndex); break;
            case 4: SpawnCluster(laneIndex); break;
            default: SpawnColumn(laneIndex); break;
        }
    }

    int PickPattern()
    {
        List<int> pool = new List<int>();
        // 0=column,1=V,2=diagonal,3=zigzag,4=cluster
        for (int i = 0; i < weightColumn; i++) pool.Add(0);
        for (int i = 0; i < weightV; i++) pool.Add(1);
        for (int i = 0; i < weightDiagonal; i++) pool.Add(2);
        for (int i = 0; i < weightZigzag; i++) pool.Add(3);
        for (int i = 0; i < weightCluster; i++) pool.Add(4);
        return pool[rng.Next(pool.Count)];
    }

    void SpawnCoinAt(Vector3 pos, int value = 1)
    {
        var coin = Instantiate(coinPrefab, pos, Quaternion.identity);
        var mover = coin.GetComponent<CoinMover>();
        if (mover != null) mover.SetSpeed(worldScrollSpeed);
        var col = coin.GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        var rc = coin.GetComponent<UpdatedCoinCollectible>();
        if (rc != null) rc.amount = value;
    }

    void SpawnColumn(int laneIndex)
    {
        int count = rng.Next(3, Mathf.Min(maxCoinsInPattern, 6));
        float startY = spawnY - 1.2f;
        float x = LanesManager.Instance.LaneToWorldX(laneIndex);
        for (int i = 0; i < count; i++)
            SpawnCoinAt(new Vector3(x, startY - i * coinSpacing, 0f));
    }

    void SpawnV(int laneIndex)
    {
        // center column then two diagonals outwards
        int height = rng.Next(3, 6);
        float xCenter = LanesManager.Instance.LaneToWorldX(laneIndex);
        float startY = spawnY - 1f;
        // center vertical (short)
        for (int i = 0; i < height; i++) SpawnCoinAt(new Vector3(xCenter, startY - i * coinSpacing, 0f));
        // left diag
        int sideSteps = Mathf.Min(laneIndex, 2); // ensure valid lane
        if (laneIndex - 1 >= 0)
        {
            for (int s = 0; s < height; s++)
                SpawnCoinAt(new Vector3(LanesManager.Instance.LaneToWorldX(laneIndex - 1) - 0.2f * s, startY - s * coinSpacing, 0f));
        }
        // right diag
        if (laneIndex + 1 < LanesManager.Instance.laneCount)
        {
            for (int s = 0; s < height; s++)
                SpawnCoinAt(new Vector3(LanesManager.Instance.LaneToWorldX(laneIndex + 1) + 0.2f * s, startY - s * coinSpacing, 0f));
        }
    }

    void SpawnDiagonal(int laneIndex, bool leftToRight)
    {
        int len = rng.Next(3, maxCoinsInPattern);
        int startLane = laneIndex;
        for (int i = 0; i < len; i++)
        {
            int lane = Mathf.Clamp(startLane + (leftToRight ? i : -i), 0, LanesManager.Instance.laneCount - 1);
            float x = LanesManager.Instance.LaneToWorldX(lane);
            SpawnCoinAt(new Vector3(x, spawnY - 0.8f - i * coinSpacing, 0f));
        }
    }

    void SpawnZigZag(int laneIndex)
    {
        int len = rng.Next(4, maxCoinsInPattern);
        int dir = rng.Next(0, 2) == 0 ? -1 : 1;
        int lane = laneIndex;
        for (int i = 0; i < len; i++)
        {
            lane = Mathf.Clamp(lane + (i % 2 == 0 ? dir : -dir), 0, LanesManager.Instance.laneCount - 1);
            float x = LanesManager.Instance.LaneToWorldX(lane);
            SpawnCoinAt(new Vector3(x, spawnY - 0.8f - i * coinSpacing, 0f));
        }
    }

    void SpawnCluster(int laneIndex)
    {
        // spawn coins in small cluster around laneIndex (use neighboring lanes too)
        int total = rng.Next(4, 9);
        for (int i = 0; i < total; i++)
        {
            int lane = Mathf.Clamp(laneIndex + rng.Next(-1, 2), 0, LanesManager.Instance.laneCount - 1);
            float x = LanesManager.Instance.LaneToWorldX(lane) + Random.Range(-0.2f, 0.2f);
            float y = spawnY - 0.8f - Random.Range(0f, (total / 2f) * coinSpacing);
            SpawnCoinAt(new Vector3(x, y, 0f));
        }
    }
}
