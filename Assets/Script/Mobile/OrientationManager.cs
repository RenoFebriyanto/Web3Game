using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 📱 ORIENTATION MANAGER - Force Landscape untuk Mobile Web
/// 
/// FITUR:
/// ✅ Auto-detect mobile device
/// ✅ Force landscape orientation
/// ✅ Show rotation prompt jika portrait
/// ✅ Works di WebGL build
/// 
/// SETUP:
/// 1. Attach ke GameObject di scene pertama (MainMenu)
/// 2. Assign rotationPromptCanvas di Inspector
/// 3. Build Settings → Player → Resolution & Presentation → Default Orientation = Landscape Left
/// </summary>
[DefaultExecutionOrder(-800)]
public class OrientationManager : MonoBehaviour
{
    public static OrientationManager Instance { get; private set; }

    [Header("🎨 Rotation Prompt UI")]
    [Tooltip("Canvas yang muncul saat device dalam portrait mode")]
    public Canvas rotationPromptCanvas;

    [Tooltip("Text untuk instruksi rotasi")]
    public TextMeshProUGUI rotationPromptText;

    [Header("⚙️ Settings")]
    [Tooltip("Force landscape mode?")]
    public bool forceLandscape = true;

    [Tooltip("Check interval (detik)")]
    public float checkInterval = 0.5f;

    [Header("📊 Status (Read-Only)")]
    [SerializeField] private bool isMobileDevice = false;
    [SerializeField] private bool isLandscape = true;
    [SerializeField] private int screenWidth = 0;
    [SerializeField] private int screenHeight = 0;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private float lastCheckTime = 0f;

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

        // Detect mobile
        DetectMobile();

        // Initial setup
        SetupOrientation();

        Log("✅ OrientationManager initialized");
    }

    void Start()
    {
        // Hide prompt initially
        if (rotationPromptCanvas != null)
        {
            rotationPromptCanvas.gameObject.SetActive(false);
        }

        // Initial check
        CheckOrientation();
    }

    void Update()
    {
        // Check orientation periodically
        if (Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            CheckOrientation();
        }
    }

    // ========================================
    // MOBILE DETECTION
    // ========================================

    void DetectMobile()
    {
#if UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
#elif UNITY_WEBGL
        // WebGL: Check user agent via JavaScript
        isMobileDevice = IsMobileWebGL();
#else
        isMobileDevice = false;
#endif

        Log($"Device type: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
    }

    bool IsMobileWebGL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Call JavaScript to check user agent
        return Application.isMobilePlatform;
#else
        return false;
#endif
    }

    // ========================================
    // ORIENTATION SETUP
    // ========================================

    void SetupOrientation()
    {
        if (!forceLandscape)
        {
            Log("Landscape mode disabled");
            return;
        }

        // Unity settings (for standalone builds)
#if UNITY_STANDALONE || UNITY_EDITOR
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Log("✓ Set orientation: Landscape (Standalone)");
#elif UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Landscape;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Log("✓ Set orientation: Landscape (Mobile)");
#endif

        // For WebGL, orientation is handled via HTML/CSS meta tags
        Log("✓ Orientation setup complete");
    }

    // ========================================
    // ORIENTATION CHECK
    // ========================================

    void CheckOrientation()
    {
        // Update screen dimensions
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        // Check if landscape
        bool wasLandscape = isLandscape;
        isLandscape = screenWidth > screenHeight;

        // Only process if mobile
        if (!isMobileDevice)
        {
            HideRotationPrompt();
            return;
        }

        // Show/hide rotation prompt
        if (!isLandscape)
        {
            ShowRotationPrompt();

            // Log only on change
            if (wasLandscape != isLandscape)
            {
                LogWarning($"⚠️ PORTRAIT MODE DETECTED - Screen: {screenWidth}x{screenHeight}");
            }
        }
        else
        {
            HideRotationPrompt();

            // Log only on change
            if (wasLandscape != isLandscape)
            {
                Log($"✓ Landscape mode - Screen: {screenWidth}x{screenHeight}");
            }
        }
    }

    // ========================================
    // UI PROMPT
    // ========================================

    void ShowRotationPrompt()
    {
        if (rotationPromptCanvas == null) return;

        if (!rotationPromptCanvas.gameObject.activeSelf)
        {
            rotationPromptCanvas.gameObject.SetActive(true);

            if (rotationPromptText != null)
            {
                rotationPromptText.text = "📱 Please rotate your device\nto landscape mode";
            }

            Log("✓ Rotation prompt shown");
        }

        // Pause game (optional)
        // Time.timeScale = 0f;
    }

    void HideRotationPrompt()
    {
        if (rotationPromptCanvas == null) return;

        if (rotationPromptCanvas.gameObject.activeSelf)
        {
            rotationPromptCanvas.gameObject.SetActive(false);
            Log("✓ Rotation prompt hidden");

            // Resume game (optional)
            // Time.timeScale = 1f;
        }
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public bool IsMobile() => isMobileDevice;
    public bool IsLandscapeMode() => isLandscape;
    public Vector2Int GetScreenSize() => new Vector2Int(screenWidth, screenHeight);

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[OrientationManager] {message}");
    }

    void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[OrientationManager] {message}");
    }

    // ========================================
    // CONTEXT MENU
    // ========================================

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== ORIENTATION MANAGER STATUS ===");
        Debug.Log($"Device: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Orientation: {(isLandscape ? "LANDSCAPE ✓" : "PORTRAIT ⚠️")}");
        Debug.Log($"Screen: {screenWidth}x{screenHeight}");
        Debug.Log($"Force Landscape: {forceLandscape}");
        Debug.Log($"Prompt Active: {(rotationPromptCanvas != null && rotationPromptCanvas.gameObject.activeSelf)}");
        Debug.Log("=================================");
    }

    [ContextMenu("🔄 Force Check")]
    void Context_ForceCheck()
    {
        CheckOrientation();
    }
}