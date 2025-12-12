using UnityEngine;

/// <summary>
/// ✅ Camera Shake Effect - Trigger saat collision damage
/// Attach script ini ke Main Camera di Gameplay scene
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    [Tooltip("Durasi shake effect (detik)")]
    public float shakeDuration = 0.3f;

    [Tooltip("Intensitas shake (magnitude)")]
    public float shakeMagnitude = 0.2f;

    [Tooltip("Kecepatan shake berkurang (damping)")]
    public float dampingSpeed = 1.5f;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    // Runtime variables
    private Vector3 originalPosition;
    private float currentShakeDuration = 0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Store original camera position
        originalPosition = transform.localPosition;

        Log("✓ CameraShake initialized");
    }

    void Update()
    {
        // Update shake effect jika sedang aktif
        if (currentShakeDuration > 0)
        {
            // Generate random offset
            Vector3 shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            transform.localPosition = originalPosition + shakeOffset;

            // Decrease duration
            currentShakeDuration -= Time.deltaTime * dampingSpeed;

            // Clamp to 0
            if (currentShakeDuration <= 0f)
            {
                currentShakeDuration = 0f;
                transform.localPosition = originalPosition;

                Log("✓ Shake effect complete");
            }
        }
    }

    /// <summary>
    /// ✅ PUBLIC API: Trigger shake effect
    /// Call dari PlanetDamage saat collision
    /// </summary>
    public void TriggerShake()
    {
        if (currentShakeDuration > 0)
        {
            // Reset duration jika shake sudah aktif (extend shake)
            currentShakeDuration = shakeDuration;
        }
        else
        {
            // Start new shake
            originalPosition = transform.localPosition;
            currentShakeDuration = shakeDuration;
        }

        Log($"🎬 Shake triggered: duration={shakeDuration}s, magnitude={shakeMagnitude}");
    }

    /// <summary>
    /// Trigger shake dengan custom intensity
    /// </summary>
    public void TriggerShake(float intensity)
    {
        originalPosition = transform.localPosition;
        currentShakeDuration = shakeDuration;
        shakeMagnitude = intensity;

        Log($"🎬 Shake triggered (custom): intensity={intensity}");
    }

    /// <summary>
    /// Stop shake immediately
    /// </summary>
    public void StopShake()
    {
        currentShakeDuration = 0f;
        transform.localPosition = originalPosition;

        Log("⏹️ Shake stopped");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CameraShake] {message}");
    }

    // ========================================
    // CONTEXT MENU (Testing)
    // ========================================

    [ContextMenu("Test: Trigger Shake")]
    void Test_TriggerShake()
    {
        TriggerShake();
    }

    [ContextMenu("Test: Trigger Strong Shake")]
    void Test_TriggerStrongShake()
    {
        TriggerShake(0.5f);
    }

    [ContextMenu("Test: Stop Shake")]
    void Test_StopShake()
    {
        StopShake();
    }
}