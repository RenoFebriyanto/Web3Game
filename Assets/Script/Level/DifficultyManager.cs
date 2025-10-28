using UnityEngine;

/// <summary>
/// Manages gameplay difficulty progression (speed increase over time)
/// Singleton pattern - accessible from anywhere via DifficultyManager.Instance
/// 
/// Letakkan di: Assets/Script/Movement/DifficultyManager.cs
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

    [Header("Runtime Info (Debug)")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float elapsedTime;
    [SerializeField] private float speedProgress; // 0-1

    // Public getter for current speed
    public float CurrentSpeed => currentSpeed;
    public float SpeedProgress => speedProgress;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentSpeed = baseSpeed;
        elapsedTime = 0f;
        speedProgress = 0f;
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

    /// <summary>
    /// Reset difficulty to starting values (call on level restart)
    /// </summary>
    public void ResetDifficulty()
    {
        elapsedTime = 0f;
        speedProgress = 0f;
        currentSpeed = baseSpeed;
        Debug.Log("[DifficultyManager] Reset to base speed: " + baseSpeed);
    }

    /// <summary>
    /// Manually set speed (for testing or special events)
    /// </summary>
    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, baseSpeed, maxSpeed);
    }

    /// <summary>
    /// Get speed multiplier (useful for spawners)
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return currentSpeed / baseSpeed;
    }

    // ========================================
    // CONTEXT MENU DEBUG
    // ========================================

    [ContextMenu("Reset Difficulty")]
    void Debug_Reset()
    {
        ResetDifficulty();
    }

    [ContextMenu("Set Max Speed")]
    void Debug_SetMaxSpeed()
    {
        SetSpeed(maxSpeed);
        Debug.Log($"Speed set to MAX: {maxSpeed}");
    }

    [ContextMenu("Set Base Speed")]
    void Debug_SetBaseSpeed()
    {
        SetSpeed(baseSpeed);
        Debug.Log($"Speed set to BASE: {baseSpeed}");
    }

    void OnValidate()
    {
        // Ensure valid values in inspector
        baseSpeed = Mathf.Max(0.5f, baseSpeed);
        maxSpeed = Mathf.Max(baseSpeed, maxSpeed);
        speedRampDuration = Mathf.Max(10f, speedRampDuration);
    }
}