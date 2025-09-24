using UnityEngine;

public class LanesManager : MonoBehaviour
{
    public static LanesManager Instance { get; private set; }

    [Header("Lane setup")]
    public int laneCount = 3;
    public float laneOffset = 2.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public float LaneToWorldX(int laneIndex)
    {
        float center = (laneCount - 1) / 2f;
        return (laneIndex - center) * laneOffset;
    }
}
