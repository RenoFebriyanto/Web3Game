using UnityEngine;

/// <summary>
/// Manages gameplay difficulty (speed increase over time).
/// Opsional - jika tidak ada, akan gunakan base speed.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Speed Settings")]
    [Tooltip("Starting scroll speed")]
    public float baseSpeed = 3f;

    [Tooltip("Maximum scroll speed")]
    public float maxSpeed = 8f;

    [Tooltip("Time (seconds) to reach max speed")]
    public float speedRampDuration = 180f; // 3 minutes

    [Header("Speed Curve")]
    [Tooltip("How speed increases over time (0-1)")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Runtime Info (Read-Only)")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float elapsedTime;
    [SerializeField] private float speedProgress; // 0-1

    public float CurrentSpeed => currentSpeed;
    public float SpeedProgress => speedProgress;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DifficultyManager] Duplicate instance found! Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[DifficultyManager] Initialized: base={baseSpeed}, max={maxSpeed}, ramp={speedRampDuration}s");
    }

    void Start()
    {
        ResetDifficulty();
    }

    void Update()
    {
        // Increment time
        elapsedTime += Time.deltaTime;

        // Calculate progress (0 to 1)
        speedProgress = Mathf.Clamp01(elapsedTime / speedRampDuration);

        // Apply curve and calculate current speed
        float curveValue = speedCurve.Evaluate(speedProgress);
        currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, curveValue);
    }

    public void ResetDifficulty()
    {
        elapsedTime = 0f;
        speedProgress = 0f;
        currentSpeed = baseSpeed;
        Debug.Log($"[DifficultyManager] Reset to base speed: {baseSpeed}");
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, baseSpeed, maxSpeed);
    }

    public float GetSpeedMultiplier()
    {
        return currentSpeed / baseSpeed;
    }

    // ========================================
    // STATIC AUTO-CREATE METHOD
    // ========================================

    public static DifficultyManager EnsureExists()
    {
        if (Instance != null) return Instance;

        Debug.LogWarning("[DifficultyManager] Instance not found! Creating default DifficultyManager...");

        GameObject go = new GameObject("DifficultyManager");
        var manager = go.AddComponent<DifficultyManager>();

        // Set default values
        manager.baseSpeed = 3f;
        manager.maxSpeed = 8f;
        manager.speedRampDuration = 180f;

        Debug.Log("[DifficultyManager] ✓ Auto-created with default settings");

        return manager;
    }

    [ContextMenu("Reset Difficulty")]
    void Context_Reset() => ResetDifficulty();

    [ContextMenu("Set Max Speed")]
    void Context_SetMaxSpeed() => SetSpeed(maxSpeed);

    void OnValidate()
    {
        baseSpeed = Mathf.Max(0.5f, baseSpeed);
        maxSpeed = Mathf.Max(baseSpeed, maxSpeed);
        speedRampDuration = Mathf.Max(10f, speedRampDuration);
    }
}