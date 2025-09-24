using System.Collections.Generic;
using UnityEngine;

public class Pooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string id;
        public GameObject prefab;
        public int size = 20;
    }

    public Pool[] pools;
    private Dictionary<string, Queue<GameObject>> poolDict;

    void Awake()
    {
        poolDict = new Dictionary<string, Queue<GameObject>>();
        foreach (var p in pools)
        {
            var q = new Queue<GameObject>();
            for (int i = 0; i < p.size; i++)
            {
                var go = Instantiate(p.prefab, transform);
                go.SetActive(false);
                q.Enqueue(go);
            }
            poolDict[p.id] = q;
        }
    }

    public GameObject Spawn(string id, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!poolDict.ContainsKey(id))
        {
            Debug.LogWarning($"Pooler: no pool with id {id}");
            return null;
        }

        var q = poolDict[id];
        GameObject obj;
        if (q.Count > 0)
        {
            obj = q.Dequeue();
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.SetActive(true);
        }
        else
        {
            // fallback: instantiate new
            var prefab = System.Array.Find(pools, x => x.id == id)?.prefab;
            obj = Instantiate(prefab, pos, rot, parent);
        }

        // if pooled object has IPoolable interface, call OnSpawned
        var poolable = obj.GetComponent<IPoolable>();
        if (poolable != null) poolable.OnSpawned();

        return obj;
    }

    public void Despawn(string id, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        if (!poolDict.ContainsKey(id))
        {
            Debug.LogWarning($"Pooler: despawn to missing pool {id}");
            Destroy(obj);
            return;
        }
        poolDict[id].Enqueue(obj);
    }

    // convenience for pooling without specifying id (prefab name)
    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
    }
}

public interface IPoolable
{
    void OnSpawned();
}
