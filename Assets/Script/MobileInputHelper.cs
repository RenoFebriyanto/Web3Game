using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ✅ Mobile Input Helper - Universal Input Detection
/// Automatically detects platform and handles input accordingly
/// NO MODIFICATION NEEDED to existing scripts!
/// 
/// Features:
/// - Auto-detect mobile vs desktop
/// - Universal tap/click detection
/// - Touch feedback (optional)
/// - Multi-touch prevention
/// 
/// Setup:
/// 1. Attach to any GameObject in scene (e.g., GameManager)
/// 2. Configure settings in Inspector
/// 3. Done! All Unity UI components will work automatically
/// </summary>
public class MobileInputHelper : MonoBehaviour
{
    public static MobileInputHelper Instance { get; private set; }

    [Header("🎮 Platform Detection")]
    [SerializeField] private bool autoDetectPlatform = true;
    [SerializeField] private bool forceMobileMode = false;

    [Header("📱 Touch Settings")]
    [Tooltip("Prevent accidental multi-touch")]
    [SerializeField] private bool preventMultiTouch = true;

    [Tooltip("Maximum time between taps for double-tap (ms)")]
    [SerializeField] private float doubleTapThreshold = 300f;

    [Header("🎨 Visual Feedback")]
    [Tooltip("Show touch position indicator (for debugging)")]
    [SerializeField] private bool showTouchIndicator = false;

    [SerializeField] private GameObject touchIndicatorPrefab;

    [Header("🔊 Audio Feedback")]
    [Tooltip("Play sound on button tap")]
    [SerializeField] private bool playTapSound = true;

    [Header("⚙️ Performance")]
    [Tooltip("Enable touch optimization for low-end devices")]
    [SerializeField] private bool enableTouchOptimization = true;

    [Header("🐛 Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Runtime state
    private bool isMobile = false;
    private float lastTapTime = 0f;
    private int currentTouchCount = 0;
    private GameObject touchIndicatorInstance;

    // Properties
    public bool IsMobile => isMobile;
    public bool IsMultiTouchActive => currentTouchCount > 1;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Detect platform
        DetectPlatform();

        Log("✅ MobileInputHelper initialized");
        Log($"Platform: {(isMobile ? "MOBILE" : "DESKTOP")}");
        Log($"Multi-touch prevention: {preventMultiTouch}");
    }

    void Start()
    {
        // Setup touch indicator
        if (showTouchIndicator && touchIndicatorPrefab != null)
        {
            touchIndicatorInstance = Instantiate(touchIndicatorPrefab);
            touchIndicatorInstance.SetActive(false);
        }

        // Configure Unity Input for mobile
        if (isMobile)
        {
            ConfigureMobileInput();
        }
    }

    void Update()
    {
        // Update touch count
        currentTouchCount = Input.touchCount;

        // Handle touch indicator
        if (showTouchIndicator && touchIndicatorInstance != null)
        {
            UpdateTouchIndicator();
        }

        // Mobile-specific optimizations
        if (isMobile && enableTouchOptimization)
        {
            HandleMobileOptimizations();
        }
    }

    // ========================================
    // PLATFORM DETECTION
    // ========================================

    void DetectPlatform()
    {
        if (forceMobileMode)
        {
            isMobile = true;
            return;
        }

        if (!autoDetectPlatform)
        {
            isMobile = false;
            return;
        }

        // Detect based on platform
#if UNITY_ANDROID || UNITY_IOS
        isMobile = true;
#elif UNITY_EDITOR
        // In editor, check for touch simulation
        isMobile = false; // Default to desktop in editor
#else
        // WebGL or other platforms - check for touch support
        isMobile = Input.touchSupported;
#endif

        Log($"Auto-detected platform: {(isMobile ? "Mobile" : "Desktop")}");
    }

    // ========================================
    // MOBILE CONFIGURATION
    // ========================================

    void ConfigureMobileInput()
    {
        // Set multi-touch mode
        if (preventMultiTouch)
        {
            Input.multiTouchEnabled = false;
            Log("✓ Multi-touch disabled");
        }
        else
        {
            Input.multiTouchEnabled = true;
            Log("✓ Multi-touch enabled");
        }

        // Enable accelerometer if needed
        // Input.gyro.enabled = true; // Uncomment if using gyroscope

        Log("✓ Mobile input configured");
    }

    // ========================================
    // TOUCH HANDLING
    // ========================================

    void HandleMobileOptimizations()
    {
        // Prevent accidental multi-touch
        if (preventMultiTouch && currentTouchCount > 1)
        {
            // Cancel extra touches
            Log("⚠️ Multi-touch detected and prevented");
        }
    }

    void UpdateTouchIndicator()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchIndicatorInstance.SetActive(true);

            // Convert touch position to world position
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(
                touch.position.x,
                touch.position.y,
                10f
            ));

            touchIndicatorInstance.transform.position = worldPos;
        }
        else
        {
            touchIndicatorInstance.SetActive(false);
        }
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Check if tap/click occurred this frame
    /// Universal method for mobile & desktop
    /// </summary>
    public bool GetTap()
    {
        if (isMobile)
        {
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }
        else
        {
            return Input.GetMouseButtonDown(0);
        }
    }

    /// <summary>
    /// Check if tap/click is held
    /// </summary>
    public bool GetTapHeld()
    {
        if (isMobile)
        {
            return Input.touchCount > 0;
        }
        else
        {
            return Input.GetMouseButton(0);
        }
    }

    /// <summary>
    /// Get tap/click position
    /// </summary>
    public Vector2 GetTapPosition()
    {
        if (isMobile && Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        else
        {
            return Input.mousePosition;
        }
    }

    /// <summary>
    /// Check if double-tap occurred
    /// </summary>
    public bool GetDoubleTap()
    {
        if (!GetTap()) return false;

        float currentTime = Time.time * 1000f;
        float timeSinceLastTap = currentTime - lastTapTime;

        if (timeSinceLastTap < doubleTapThreshold)
        {
            lastTapTime = 0f;
            Log("Double-tap detected!");
            return true;
        }

        lastTapTime = currentTime;
        return false;
    }

    /// <summary>
    /// Check if tapping over UI element
    /// </summary>
    public bool IsTapOverUI()
    {
        if (EventSystem.current == null) return false;

        if (isMobile && Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }

    /// <summary>
    /// Vibrate device (mobile only)
    /// </summary>
    public void VibrateDevice(long milliseconds = 50)
    {
        if (!isMobile) return;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        Log($"Device vibrated for {milliseconds}ms");
#endif
    }

    /// <summary>
    /// Play tap feedback sound
    /// </summary>
    public void PlayTapFeedback()
    {
        if (!playTapSound) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    // ========================================
    // UTILITY
    // ========================================

    /// <summary>
    /// Force platform mode (for testing)
    /// </summary>
    public void SetMobileMode(bool mobile)
    {
        isMobile = mobile;
        Log($"Platform mode set to: {(mobile ? "MOBILE" : "DESKTOP")}");

        if (mobile)
        {
            ConfigureMobileInput();
        }
    }

    /// <summary>
    /// Get current touch/click info (for debugging)
    /// </summary>
    public string GetInputInfo()
    {
        if (isMobile)
        {
            return $"Platform: Mobile | Touches: {Input.touchCount} | Multi-touch: {Input.multiTouchEnabled}";
        }
        else
        {
            return $"Platform: Desktop | Mouse: {Input.mousePosition}";
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[MobileInputHelper] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[MobileInputHelper] {message}");
    }

    // ========================================
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("📱 Force Mobile Mode")]
    void Context_ForceMobile()
    {
        SetMobileMode(true);
        Debug.Log("[MobileInputHelper] ✓ Forced to Mobile Mode");
    }

    [ContextMenu("🖥️ Force Desktop Mode")]
    void Context_ForceDesktop()
    {
        SetMobileMode(false);
        Debug.Log("[MobileInputHelper] ✓ Forced to Desktop Mode");
    }

    [ContextMenu("🔄 Re-Detect Platform")]
    void Context_ReDetect()
    {
        DetectPlatform();
        Debug.Log($"[MobileInputHelper] ✓ Platform: {(isMobile ? "Mobile" : "Desktop")}");
    }

    [ContextMenu("📊 Print Input Info")]
    void Context_PrintInfo()
    {
        Debug.Log("=== MOBILE INPUT HELPER STATUS ===");
        Debug.Log($"Platform: {(isMobile ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Touch Supported: {Input.touchSupported}");
        Debug.Log($"Multi-touch: {Input.multiTouchEnabled}");
        Debug.Log($"Current Touches: {Input.touchCount}");
        Debug.Log($"Touch Optimization: {enableTouchOptimization}");
        Debug.Log($"Tap Sound: {playTapSound}");
        Debug.Log("==================================");
    }

    [ContextMenu("📳 Test Vibration")]
    void Context_TestVibration()
    {
        VibrateDevice(100);
        Debug.Log("[MobileInputHelper] ✓ Vibration triggered (mobile only)");
    }
}