// FragmentSpawner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentSpawner : MonoBehaviour
{
    [Header("References")]
    public FragmentPrefabRegistry registry;
    public LevelGameSession session;

    [Header("Spawn settings")]
    public int laneCount = 3;
    public float laneOffset = 2.5f;
    public float spawnY = 10f;
    public Transform spawnParent;
    public float spawnInterval = 1.0f;
    public LayerMask overlapMask;
    public float overlapRadius = 0.35f;

    void Start()
    {
        if (session == null) session = LevelGameSession.Instance;
        if (spawnParent == null) spawnParent = transform;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (session == null || session.currentLevel == null) continue;

            // choose a requirement that still has remaining
            var available = new List<(FragmentType t, int v)>();
            foreach (var r in session.currentLevel.requirements)
            {
                int rem = session.GetRemaining(r.type, r.colorVariant);
                if (rem > 0) available.Add((r.type, r.colorVariant));
            }
            if (available.Count == 0) continue;

            // pick random req
            var chosen = available[Random.Range(0, available.Count)];

            // pick a lane that is free (avoid overlap)
            int lane = Random.Range(0, laneCount);
            float center = (laneCount - 1) / 2f;
            float x = (lane - center) * laneOffset;
            Vector3 pos = new Vector3(x, spawnY, 0f);

            if (Physics2D.OverlapCircle(pos, overlapRadius, overlapMask)) continue;

            GameObject prefab = registry?.GetPrefab(chosen.t, chosen.v);
            if (prefab == null) continue;

            var go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
            // ensure it will move downward: if using PlanetMover set speed
            var mover = go.GetComponent<PlanetMover>();
            if (mover != null) mover.SetSpeed(Mathf.Abs(mover.speed)); // use prefab's speed or override

            // Note: we do not decrement here — decrement happens when player picks up (LevelGameSession)
        }
    }
}
