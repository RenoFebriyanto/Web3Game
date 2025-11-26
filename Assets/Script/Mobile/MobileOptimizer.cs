using UnityEngine;

/// <summary>
/// 📱 MOBILE OPTIMIZER - Optimize Performance untuk Mobile Web
/// 
/// FITUR:
/// ✅ Auto-detect mobile & adjust quality
/// ✅ Lower target framerate untuk save battery
/// ✅ Disable unnecessary features
/// ✅ Optimize physics & rendering
/// 
/// SETUP:
/// 1. Attach ke GameObject di scene pertama
/// 2. Configure settings di Inspector
/// 3. Will auto-apply optimizations on mobile
/// </summary>
[DefaultExecutionOrder(-900)]
public class MobileOptimizer : MonoBehaviour
{
    public static MobileOptimizer Instance { get; private set; }

    [Header("📱 Platform Detection")]
    [Tooltip("Force mobile mode? (for testing)")]
    public bool forceMobileMode = false;

    [Header("🎮 Performance Settings")]
    [Tooltip("Target FPS untuk mobile")]
    public int mobileTargetFPS = 60;

    [Tooltip("Target FPS untuk desktop")]
    public int desktopTargetFPS = 60;

    [Header("🎨 Quality Settings")]
    [Tooltip("Quality level untuk mobile (0-5, 0=Very Low)")]
    [Range(0, 5)]
    public int mobileQualityLevel = 2; // Medium

    [Tooltip("Quality level untuk desktop")]
    [Range(0, 5)]
    public int desktopQualityLevel = 5; // Very High

    [Header("⚡ Optimization Toggles")]
    [Tooltip("Enable VSync? (disable untuk better performance)")]
    public bool enableVSync = false;

    [Tooltip("Optimize physics untuk mobile?")]
    public bool optimizePhysics = true;

    [Tooltip("Reduce particle effects?")]
    public bool reduceParticles = true;

    [Header("📊 Runtime Status")]
    [SerializeField] private bool isMobileDevice = false;
    [SerializeField] private int currentQuality = 5;
    [SerializeField] private int currentFPS = 60;
    [SerializeField] private float avgFrameTime = 0f;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;
    public bool showFPSCounter = false;

    // FPS calculation
    private float deltaTime = 0.0f;
    private float fpsUpdateInterval = 0.5f;
    private float fpsTimer = 0f;

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

        // Apply optimizations
        ApplyOptimizations();

        Log("✅ MobileOptimizer initialized");
    }

    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        fpsTimer += Time.deltaTime;
        if (fpsTimer >= fpsUpdateInterval)
        {
            avgFrameTime = deltaTime * 1000.0f;
            currentFPS = Mathf.RoundToInt(1.0f / deltaTime);
            fpsTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!showFPSCounter) return;

        // FPS counter
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

    // ========================================
    // PLATFORM DETECTION
    // ========================================

    void DetectPlatform()
    {
        if (forceMobileMode)
        {
            isMobileDevice = true;
            Log("Platform: FORCED MOBILE MODE");
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
#elif UNITY_WEBGL
        // WebGL: Check via Application.isMobilePlatform
        isMobileDevice = Application.isMobilePlatform;
#else
        isMobileDevice = false;
#endif

        Log($"Platform detected: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
    }

    // ========================================
    // APPLY OPTIMIZATIONS
    // ========================================

    void ApplyOptimizations()
    {
        Log("Applying optimizations...");

        // 1. Set target FPS
        SetTargetFramerate();

        // 2. Set quality level
        SetQualityLevel();

        // 3. Configure VSync
        ConfigureVSync();

        // 4. Optimize physics (if mobile)
        if (isMobileDevice && optimizePhysics)
        {
            OptimizePhysics();
        }

        // 5. Mobile-specific settings
        if (isMobileDevice)
        {
            ApplyMobileSettings();
        }

        Log("✅ Optimizations applied");
    }

    // ========================================
    // FRAMERATE
    // ========================================

    void SetTargetFramerate()
    {
        int targetFPS = isMobileDevice ? mobileTargetFPS : desktopTargetFPS;

        Application.targetFrameRate = targetFPS;
        currentFPS = targetFPS;

        Log($"✓ Target FPS: {targetFPS}");
    }

    // ========================================
    // QUALITY SETTINGS
    // ========================================

    void SetQualityLevel()
    {
        int quality = isMobileDevice ? mobileQualityLevel : desktopQualityLevel;

        QualitySettings.SetQualityLevel(quality, true);
        currentQuality = quality;

        Log($"✓ Quality level: {quality} ({QualitySettings.names[quality]})");
    }

    // ========================================
    // VSYNC
    // ========================================

    void ConfigureVSync()
    {
        // VSync: 0 = off, 1 = on, 2 = every second frame
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;

        Log($"✓ VSync: {(enableVSync ? "ENABLED" : "DISABLED")}");
    }

    // ========================================
    // PHYSICS OPTIMIZATION
    // ========================================

    void OptimizePhysics()
    {
        // Lower physics timestep untuk better performance
        Time.fixedDeltaTime = 0.02f; // 50 physics updates per second (default: 0.02)

        // Reduce physics iterations
        Physics2D.velocityIterations = 4; // Default: 8
        Physics2D.positionIterations = 2; // Default: 3

        Log("✓ Physics optimized for mobile");
    }

    // ========================================
    // MOBILE-SPECIFIC SETTINGS
    // ========================================

    void ApplyMobileSettings()
    {
        // Disable HDR (High Dynamic Range)
        Camera.main.allowHDR = false;

        // Disable MSAA (Multi-Sample Anti-Aliasing)
        QualitySettings.antiAliasing = 0;

        // Reduce shadow quality
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowResolution = ShadowResolution.Low;

        // Reduce particle quality
        if (reduceParticles)
        {
            QualitySettings.particleRaycastBudget = 32; // Default: 256
        }

        // Disable realtime reflections
        QualitySettings.realtimeReflectionProbes = false;

        // Lower LOD bias (reduce draw distance)
        QualitySettings.lodBias = 0.7f; // Default: 2.0

        Log("✓ Mobile-specific settings applied");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public bool IsMobile() => isMobileDevice;

    public int GetCurrentFPS() => currentFPS;

    public void SetQuality(int level)
    {
        level = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(level, true);
        currentQuality = level;
        Log($"Quality changed to: {level} ({QualitySettings.names[level]})");
    }

    public void ToggleFPSCounter()
    {
        showFPSCounter = !showFPSCounter;
        Log($"FPS counter: {(showFPSCounter ? "SHOWN" : "HIDDEN")}");
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[MobileOptimizer] {message}");
    }

    // ========================================
    // CONTEXT MENU
    // ========================================

    [ContextMenu("📊 Print Optimization Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== MOBILE OPTIMIZER STATUS ===");
        Debug.Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Quality Level: {currentQuality} ({QualitySettings.names[currentQuality]})");
        Debug.Log($"Target FPS: {Application.targetFrameRate}");
        Debug.Log($"Current FPS: {currentFPS}");
        Debug.Log($"Frame Time: {avgFrameTime:F1} ms");
        Debug.Log($"VSync: {(QualitySettings.vSyncCount > 0 ? "ENABLED" : "DISABLED")}");
        Debug.Log($"MSAA: {QualitySettings.antiAliasing}x");
        Debug.Log($"Shadows: {QualitySettings.shadows}");
        Debug.Log("==============================");
    }

    [ContextMenu("🎮 Toggle FPS Counter")]
    void Context_ToggleFPS()
    {
        ToggleFPSCounter();
    }

    [ContextMenu("📱 Force Mobile Mode")]
    void Context_ForceMobile()
    {
        forceMobileMode = true;
        DetectPlatform();
        ApplyOptimizations();
        Debug.Log("✓ Forced to mobile mode");
    }

    [ContextMenu("🖥️ Force Desktop Mode")]
    void Context_ForceDesktop()
    {
        forceMobileMode = false;
        isMobileDevice = false;
        ApplyOptimizations();
        Debug.Log("✓ Forced to desktop mode");
    }
}