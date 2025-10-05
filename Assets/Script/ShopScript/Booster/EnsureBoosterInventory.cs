using UnityEngine;

[DefaultExecutionOrder(-100)]
public class EnsureBoosterInventory : MonoBehaviour
{
    void Awake()
    {
        if (BoosterInventory.Instance == null)
        {
            var go = new GameObject("BoosterInventory");
            go.AddComponent<BoosterInventory>();
            DontDestroyOnLoad(go);
        }
    }
}
