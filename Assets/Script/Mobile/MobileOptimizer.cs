using UnityEngine;

/// <summary>
/// 📱 MOBILE OPTIMIZER - FIXED v2.0 with Screen Resize Handler
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
    public int mobileQualityLevel = 2;

    [Tooltip("Quality level untuk desktop")]
    [Range(0, 5)]
    public int desktopQualityLevel = 5;

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
    [SerializeField] private int lastScreenWidth = 0;
    [SerializeField] private int lastScreenHeight = 0;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;
    public bool showFPSCounter = false;

    // FPS calculation
    private float deltaTime = 0.0f;
    private float fpsUpdateInterval = 0.5f;
    private float fpsTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        DetectPlatform();
        ApplyOptimizations();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

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

        // ✅ NEW: Check for screen size change
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            OnScreenSizeChanged();
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
        string text = $"FPS: {currentFPS} ({avgFrameTime:0.0} ms) | {w}x{h}";
        GUI.Label(rect, text, style);
    }

    void DetectPlatform()
    {
        if (forceMobileMode)
        {
            isMobileDevice = true;
            Log("Platform: FORCED MOBILE MODE (local override)");
            return;
        }

        // ✅ Baca dari PlatformDetector (single source of truth) alih-alih deteksi sendiri
        if (PlatformDetector.Instance != null)
        {
            isMobileDevice = PlatformDetector.Instance.IsMobile;
            Log($"Platform dari PlatformDetector: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        }
        else
        {
            isMobileDevice = Application.isMobilePlatform;
            Log($"⚠️ PlatformDetector.Instance belum ada, fallback Application.isMobilePlatform → {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        }
    }

    void ApplyOptimizations()
    {
        Log("Applying optimizations...");

        SetTargetFramerate();
        SetQualityLevel();
        ConfigureVSync();

        if (isMobileDevice && optimizePhysics)
        {
            OptimizePhysics();
        }

        if (isMobileDevice)
        {
            ApplyMobileSettings();
        }

        Log("✅ Optimizations applied");
    }

    void SetTargetFramerate()
    {
        int targetFPS = isMobileDevice ? mobileTargetFPS : desktopTargetFPS;
        Application.targetFrameRate = targetFPS;
        currentFPS = targetFPS;
        Log($"✓ Target FPS: {targetFPS}");
    }

    void SetQualityLevel()
    {
        int quality = isMobileDevice ? mobileQualityLevel : desktopQualityLevel;
        QualitySettings.SetQualityLevel(quality, true);
        currentQuality = quality;
        Log($"✓ Quality level: {quality} ({QualitySettings.names[quality]})");
    }

    void ConfigureVSync()
    {
        QualitySettings.vSyncCount = enableVSync ? 1 : 0;
        Log($"✓ VSync: {(enableVSync ? "ENABLED" : "DISABLED")}");
    }

    void OptimizePhysics()
    {
        Time.fixedDeltaTime = 0.02f;
        Physics2D.velocityIterations = 4;
        Physics2D.positionIterations = 2;
        Log("✓ Physics optimized for mobile");
    }

    void ApplyMobileSettings()
    {
        if (Camera.main != null)
        {
            Camera.main.allowHDR = false;
        }

        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowResolution = ShadowResolution.Low;

        if (reduceParticles)
        {
            QualitySettings.particleRaycastBudget = 32;
        }

        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.lodBias = 0.7f;

        Log("✓ Mobile-specific settings applied");
    }

    // ✅ NEW: Handle screen resize (called from JavaScript)
    public void OnScreenResize(string unused = "")
    {
        OnScreenSizeChanged();
    }

    // ✅ NEW: Internal screen size change handler
    void OnScreenSizeChanged()
    {
        int newWidth = Screen.width;
        int newHeight = Screen.height;

        Log($"📐 Screen resized: {lastScreenWidth}x{lastScreenHeight} → {newWidth}x{newHeight}");

        lastScreenWidth = newWidth;
        lastScreenHeight = newHeight;

        // ✅ Force canvas update
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval(@"
            if (typeof window.unityInstance !== 'undefined') {
                try {
                    var canvas = document.getElementById('unity-canvas');
                    if (canvas) {
                        canvas.style.width = '100%';
                        canvas.style.height = '100%';
                        console.log('[Unity] Canvas resized to:', canvas.clientWidth, 'x', canvas.clientHeight);
                    }
                } catch(e) {
                    console.error('[Unity] Canvas resize failed:', e);
                }
            }
        ");
#endif

        // ✅ Notify OrientationManager if exists
        if (OrientationManager.Instance != null)
        {
            Log("✓ Notifying OrientationManager of resize");
        }
    }

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

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[MobileOptimizer] {message}");
    }

    [ContextMenu("📊 Print Optimization Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== MOBILE OPTIMIZER STATUS ===");
        Debug.Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Quality Level: {currentQuality} ({QualitySettings.names[currentQuality]})");
        Debug.Log($"Target FPS: {Application.targetFrameRate}");
        Debug.Log($"Current FPS: {currentFPS}");
        Debug.Log($"Frame Time: {avgFrameTime:F1} ms");
        Debug.Log($"Screen Size: {Screen.width}x{Screen.height}");
        Debug.Log($"VSync: {(QualitySettings.vSyncCount > 0 ? "ENABLED" : "DISABLED")}");
        Debug.Log($"MSAA: {QualitySettings.antiAliasing}x");
        Debug.Log($"Shadows: {QualitySettings.shadows}");
        Debug.Log("==============================");
    }

    [ContextMenu("🔄 Toggle FPS Counter")]
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