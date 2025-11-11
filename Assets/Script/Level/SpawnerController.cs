using UnityEngine;

/// <summary>
/// UPDATED: Helper script untuk control spawner (stop/start)
/// Sekarang support ProceduralSpawner
/// REPLACE: Assets/Script/Level/SpawnerController.cs
/// </summary>
public class SpawnerController : MonoBehaviour
{
    public static SpawnerController Instance { get; private set; }

    [Header("References")]
    [Tooltip("Assign ProceduralSpawner dari scene")]
    public ProceduralSpawner spawner;

    [Header("Status")]
    [SerializeField] private bool isSpawning = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find spawner
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<ProceduralSpawner>();
        }

        if (spawner == null)
        {
            Debug.LogWarning("[SpawnerController] ProceduralSpawner not found in scene!");
        }
    }

    void Start()
    {
        // Subscribe to level complete
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
            Debug.Log("[SpawnerController] ✓ Subscribed to level complete event");
        }
        else
        {
            Debug.LogWarning("[SpawnerController] LevelGameSession not found!");
        }
    }

    void OnDestroy()
    {
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.RemoveListener(OnLevelComplete);
        }
    }

    /// <summary>
    /// Called when level complete - stop spawner
    /// </summary>
    void OnLevelComplete()
    {
        StopSpawner();
        Debug.Log("[SpawnerController] Level complete - spawner stopped");
    }

    /// <summary>
    /// Stop spawner completely
    /// </summary>
    public void StopSpawner()
    {
        if (spawner != null)
        {
            spawner.enabled = false;
            isSpawning = false;
            Debug.Log("[SpawnerController] ✓ Spawner stopped");
        }
        else
        {
            Debug.LogWarning("[SpawnerController] Cannot stop spawner - reference is null!");
        }

        // Stop all coroutines related to spawning
        StopAllCoroutines();
    }

    /// <summary>
    /// Resume spawner
    /// </summary>
    public void ResumeSpawner()
    {
        if (spawner != null)
        {
            spawner.enabled = true;
            isSpawning = true;
            Debug.Log("[SpawnerController] ✓ Spawner resumed");
        }
        else
        {
            Debug.LogWarning("[SpawnerController] Cannot resume spawner - reference is null!");
        }
    }

    /// <summary>
    /// Check if spawner is currently active
    /// </summary>
    public bool IsSpawning() => isSpawning;

    [ContextMenu("Stop Spawner")]
    void Context_Stop() => StopSpawner();

    [ContextMenu("Resume Spawner")]
    void Context_Resume() => ResumeSpawner();

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== SPAWNER CONTROLLER STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Spawner Reference: {(spawner != null ? "OK" : "NULL")}");
        Debug.Log($"Is Spawning: {isSpawning}");
        Debug.Log($"Spawner Enabled: {(spawner != null ? spawner.enabled.ToString() : "N/A")}");
        Debug.Log("=================================");
    }
}