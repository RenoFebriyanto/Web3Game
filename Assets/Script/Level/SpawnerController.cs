using UnityEngine;

/// <summary>
/// Helper script untuk control spawner (stop/start)
/// Attach ke GameObject yang sama dengan spawner atau buat GameObject terpisah
/// </summary>
public class SpawnerController : MonoBehaviour
{
    public static SpawnerController Instance { get; private set; }

    [Header("References")]
    public FixedGameplaySpawner spawner;

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
            spawner = FindFirstObjectByType<FixedGameplaySpawner>();
        }
    }

    void Start()
    {
        // Subscribe to level complete
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
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
    }

    public bool IsSpawning() => isSpawning;

    [ContextMenu("Stop Spawner")]
    void Context_Stop() => StopSpawner();

    [ContextMenu("Resume Spawner")]
    void Context_Resume() => ResumeSpawner();
}