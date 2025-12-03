using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ✅ SIMPLIFIED Mobile Input Helper - NO ROTATION DETECTION
/// Rotation detection handled by HTML (more reliable for WebGL)
/// This script only handles input detection and optimization
/// </summary>
[DefaultExecutionOrder(-900)]
public class MobileInputHelper : MonoBehaviour
{
    public static MobileInputHelper Instance { get; private set; }

    [Header("📱 Platform Detection")]
    [SerializeField] private bool autoDetectPlatform = true;
    [SerializeField] private bool forceMobileMode = false;

    [Header("📱 Touch Settings")]
    [Tooltip("Prevent accidental multi-touch")]
    [SerializeField] private bool preventMultiTouch = true;

    [Tooltip("Maximum time between taps for double-tap (ms)")]
    [SerializeField] private float doubleTapThreshold = 300f;

    [Header("⚙️ Performance")]
    [Tooltip("Enable touch optimization for low-end devices")]
    [SerializeField] private bool enableTouchOptimization = true;

    [Header("🐛 Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showFPSCounter = false;

    // Runtime state
    private bool isMobile = false;
    private float lastTapTime = 0f;
    private int currentTouchCount = 0;

    // FPS calculation
    private float deltaTime = 0.0f;
    private float fpsUpdateInterval = 0.5f;
    private float fpsTimer = 0f;
    private int currentFPS = 60;
    private float avgFrameTime = 0f;

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
        
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[MobileInputHelper - PERSISTENT]";

        // Detect platform
        DetectPlatform();

        Log("✅ MobileInputHelper initialized");
    }

    void Start()
    {
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

        // Mobile-specific optimizations
        if (isMobile && enableTouchOptimization)
        {
            HandleMobileOptimizations();
        }

        // Calculate FPS
        if (showFPSCounter)
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fpsTimer += Time.deltaTime;
            if (fpsTimer >= fpsUpdateInterval)
            {
                avgFrameTime = deltaTime * 1000.0f;
                currentFPS = Mathf.RoundToInt(1.0f / deltaTime);
                fpsTimer = 0f;
            }
        }
    }

    void OnGUI()
    {
        if (!showFPSCounter) return;

        int w = Screen.width;
        int h = Screen.height;

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = Color.yellow;

        Rect rect = new Rect(10, 10, w, h * 2 / 100);
        string text = $"FPS: {currentFPS} ({avgFrameTime:0.0} ms)";
        GUI.Label(rect, text, style);
    }

    void DetectPlatform()
    {
        if (forceMobileMode)
        {
            isMobile = true;
            Log("Platform: FORCED MOBILE MODE");
            return;
        }

        if (!autoDetectPlatform)
        {
            isMobile = false;
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        isMobile = true;
#elif UNITY_WEBGL
        isMobile = Application.isMobilePlatform;
#else
        isMobile = false;
#endif

        Log($"Auto-detected platform: {(isMobile ? "Mobile" : "Desktop")}");
    }

    void ConfigureMobileInput()
    {
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

        Log("✓ Mobile input configured");
    }

    void HandleMobileOptimizations()
    {
        if (preventMultiTouch && currentTouchCount > 1)
        {
            Log("⚠️ Multi-touch detected and prevented");
        }
    }

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

    public void VibrateDevice(long milliseconds = 50)
    {
        if (!isMobile) return;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        Log($"Device vibrated for {milliseconds}ms");
#endif
    }

    public void PlayTapFeedback()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    public void SetMobileMode(bool mobile)
    {
        isMobile = mobile;
        Log($"Platform mode set to: {(mobile ? "MOBILE" : "DESKTOP")}");

        if (mobile)
        {
            ConfigureMobileInput();
        }
    }

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

    public void ToggleFPSCounter()
    {
        showFPSCounter = !showFPSCounter;
        Log($"FPS counter: {(showFPSCounter ? "SHOWN" : "HIDDEN")}");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[MobileInputHelper] {message}");
    }

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
        Debug.Log("==================================");
    }

    [ContextMenu("📳 Test Vibration")]
    void Context_TestVibration()
    {
        VibrateDevice(100);
        Debug.Log("[MobileInputHelper] ✓ Vibration triggered (mobile only)");
    }

    [ContextMenu("🔄 Toggle FPS Counter")]
    void Context_ToggleFPS()
    {
        ToggleFPSCounter();
    }
}