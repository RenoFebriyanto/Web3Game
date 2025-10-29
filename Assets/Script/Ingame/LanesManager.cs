using UnityEngine;

/// <summary>
/// Simple lanes manager dengan auto-setup.
/// Jika tidak ada di scene, akan dibuat otomatis dengan default values.
/// </summary>
public class LanesManager : MonoBehaviour
{
    public static LanesManager Instance { get; private set; }

    [Header("Lane Setup")]
    [Tooltip("Jumlah lane (default: 3)")]
    public int laneCount = 3;

    [Tooltip("Jarak antar lane (default: 2.5)")]
    public float laneOffset = 2.5f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LanesManager] Duplicate instance found! Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[LanesManager] Initialized: {laneCount} lanes, offset {laneOffset}");
    }

    /// <summary>
    /// Convert lane index (0, 1, 2) to world X position
    /// </summary>
    public float LaneToWorldX(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= laneCount)
        {
            Debug.LogWarning($"[LanesManager] Invalid lane index: {laneIndex}. Clamping to valid range.");
            laneIndex = Mathf.Clamp(laneIndex, 0, laneCount - 1);
        }

        float centerLane = (laneCount - 1) / 2f;
        float worldX = (laneIndex - centerLane) * laneOffset;

        return worldX;
    }

    /// <summary>
    /// Get lane index from world X position (useful for collision detection)
    /// </summary>
    public int WorldXToLane(float worldX)
    {
        float centerLane = (laneCount - 1) / 2f;
        int lane = Mathf.RoundToInt((worldX / laneOffset) + centerLane);
        return Mathf.Clamp(lane, 0, laneCount - 1);
    }

    /// <summary>
    /// Check if lane index is valid
    /// </summary>
    public bool IsValidLane(int laneIndex)
    {
        return laneIndex >= 0 && laneIndex < laneCount;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw lane lines
        Gizmos.color = Color.cyan;
        for (int i = 0; i < laneCount; i++)
        {
            float x = LaneToWorldX(i);
            Vector3 top = new Vector3(x, 10f, 0f);
            Vector3 bottom = new Vector3(x, -10f, 0f);
            Gizmos.DrawLine(top, bottom);
        }

        // Draw labels
#if UNITY_EDITOR
        for (int i = 0; i < laneCount; i++)
        {
            float x = LaneToWorldX(i);
            Vector3 pos = new Vector3(x, 8f, 0f);
            UnityEditor.Handles.Label(pos, $"Lane {i}", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.cyan },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });
        }
#endif
    }

    // ========================================
    // STATIC AUTO-CREATE METHOD
    // ========================================

    /// <summary>
    /// Ensure LanesManager exists. Creates one if missing.
    /// </summary>
    public static LanesManager EnsureExists()
    {
        if (Instance != null) return Instance;

        Debug.LogWarning("[LanesManager] Instance not found! Creating default LanesManager...");

        GameObject go = new GameObject("LanesManager");
        var manager = go.AddComponent<LanesManager>();

        // Set default values
        manager.laneCount = 3;
        manager.laneOffset = 2.5f;

        Debug.Log("[LanesManager] ✓ Auto-created with default settings");

        return manager;
    }
}