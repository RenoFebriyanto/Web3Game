using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// 📱 FIXED ORIENTATION MANAGER - Force Landscape untuk Mobile Web
/// v2.0 - WebGL Compatible
/// 
/// FITUR:
/// ✅ Force landscape via HTML/CSS (WebGL compatible)
/// ✅ JavaScript integration untuk rotation prompt
/// ✅ Auto-detect mobile browser
/// ✅ Rotation prompt overlay
/// 
/// CARA KERJA:
/// - Di WebGL: Gunakan JavaScript untuk detect orientation
/// - Di Native: Gunakan Screen.orientation
/// </summary>
[DefaultExecutionOrder(-800)]
public class OrientationManager : MonoBehaviour
{
    public static OrientationManager Instance { get; private set; }

    [Header("🎨 Rotation Prompt UI")]
    [Tooltip("Canvas untuk rotation prompt (buat di Unity)")]
    public Canvas rotationPromptCanvas;
    
    [Tooltip("Text instruksi")]
    public TextMeshProUGUI rotationPromptText;
    
    [Tooltip("Icon rotasi (optional)")]
    public Image rotationIcon;

    [Header("⚙️ Settings")]
    [Tooltip("Check interval (detik)")]
    public float checkInterval = 0.3f;
    
    [Tooltip("Minimum aspect ratio untuk landscape (width/height)")]
    public float minLandscapeAspect = 1.3f;

    [Header("📊 Status (Read-Only)")]
    [SerializeField] private bool isMobileDevice = false;
    [SerializeField] private bool isLandscape = true;
    [SerializeField] private int screenWidth = 0;
    [SerializeField] private int screenHeight = 0;
    [SerializeField] private float aspectRatio = 1.0f;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private float lastCheckTime = 0f;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    // Import JavaScript functions
    [DllImport("__Internal")]
    private static extern bool IsPortraitMode();
    
    [DllImport("__Internal")]
    private static extern bool IsMobileBrowser();
    
    [DllImport("__Internal")]
    private static extern void ShowRotationPromptJS();
    
    [DllImport("__Internal")]
    private static extern void HideRotationPromptJS();
#endif

    // Di OrientationManager.cs - REPLACE Awake() method

void Awake()
{
    // ✅ FIX: Improved singleton with scene persistence
    if (Instance != null)
    {
        if (Instance != this)
        {
            Debug.LogWarning($"[OrientationManager] Duplicate found on '{gameObject.name}' - destroying");
            Destroy(gameObject);
            return;
        }
    }

    Instance = this;
    
    // ✅ CRITICAL: Mark as persistent
    DontDestroyOnLoad(gameObject);
    gameObject.name = "[OrientationManager - PERSISTENT]";

    DetectPlatform();
    SetupOrientation();
    
    Log("✅ OrientationManager initialized and marked persistent");
}

// ✅ NEW: Add scene change handler
void OnEnable()
{
    UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
}

void OnDisable()
{
    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
}

// ✅ NEW: Re-check orientation when scene loads
void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
{
    Log($"Scene loaded: {scene.name} - Re-checking orientation");
    
    // Re-check orientation immediately
    CheckOrientation();
    
    // Double-check after delay (for UI setup)
    StartCoroutine(DelayedOrientationCheck());
}

IEnumerator DelayedOrientationCheck()
{
    yield return new WaitForSeconds(0.5f);
    CheckOrientation();
    Log("✓ Delayed orientation check complete");
}

    void Start()
    {
        // Hide prompt initially
        if (rotationPromptCanvas != null)
        {
            rotationPromptCanvas.gameObject.SetActive(false);
        }

        // Setup prompt text
        if (rotationPromptText != null)
        {
            rotationPromptText.text = "📱 Please rotate your device\nto landscape mode";
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
    // PLATFORM DETECTION
    // ========================================

    void DetectPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: Use JavaScript to detect mobile
        try
        {
            isMobileDevice = IsMobileBrowser();
        }
        catch
        {
            // Fallback
            isMobileDevice = Application.isMobilePlatform;
        }
#elif UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
#else
        isMobileDevice = Application.isMobilePlatform;
#endif

        Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
    }

    // ========================================
    // ORIENTATION SETUP
    // ========================================

    void SetupOrientation()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Native mobile: Use Unity's Screen.orientation
        Screen.orientation = ScreenOrientation.Landscape;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Log("✓ Native orientation: Landscape");
#endif
        
        // WebGL: Handled by HTML/CSS + JavaScript
        Log("✓ Orientation setup complete");
    }

    // ========================================
    // ORIENTATION CHECK
    // ========================================

    void CheckOrientation()
    {
        // Update screen info
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        aspectRatio = (float)screenWidth / screenHeight;

        bool wasLandscape = isLandscape;

        // Detect landscape
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: Use JavaScript
        if (isMobileDevice)
        {
            try
            {
                bool isPortrait = IsPortraitMode();
                isLandscape = !isPortrait;
            }
            catch
            {
                // Fallback: aspect ratio
                isLandscape = aspectRatio >= minLandscapeAspect;
            }
        }
        else
        {
            isLandscape = true; // Desktop always OK
        }
#else
        // Native or Editor: Use aspect ratio
        isLandscape = aspectRatio >= minLandscapeAspect;
#endif

        // Only process if mobile
        if (!isMobileDevice)
        {
            HidePrompt();
            return;
        }

        // Show/hide prompt
        if (!isLandscape)
        {
            ShowPrompt();
            
            if (wasLandscape != isLandscape)
            {
                LogWarning($"⚠️ PORTRAIT MODE - {screenWidth}x{screenHeight} (aspect: {aspectRatio:F2})");
            }
        }
        else
        {
            HidePrompt();
            
            if (wasLandscape != isLandscape)
            {
                Log($"✓ Landscape mode - {screenWidth}x{screenHeight} (aspect: {aspectRatio:F2})");
            }
        }
    }

    // ========================================
    // UI PROMPT
    // ========================================

    void ShowPrompt()
    {
        // Unity Canvas
        if (rotationPromptCanvas != null && !rotationPromptCanvas.gameObject.activeSelf)
        {
            rotationPromptCanvas.gameObject.SetActive(true);
            Log("✓ Unity rotation prompt shown");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // JavaScript prompt (backup)
        try
        {
            ShowRotationPromptJS();
        }
        catch { }
#endif
    }

    void HidePrompt()
    {
        // Unity Canvas
        if (rotationPromptCanvas != null && rotationPromptCanvas.gameObject.activeSelf)
        {
            rotationPromptCanvas.gameObject.SetActive(false);
            Log("✓ Unity rotation prompt hidden");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // JavaScript prompt
        try
        {
            HideRotationPromptJS();
        }
        catch { }
#endif
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public bool IsMobile() => isMobileDevice;
    public bool IsLandscapeMode() => isLandscape;
    public Vector2Int GetScreenSize() => new Vector2Int(screenWidth, screenHeight);
    public float GetAspectRatio() => aspectRatio;

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
        Debug.Log("=== ORIENTATION STATUS ===");
        Debug.Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Mode: {(isLandscape ? "LANDSCAPE ✓" : "PORTRAIT ⚠️")}");
        Debug.Log($"Screen: {screenWidth}x{screenHeight}");
        Debug.Log($"Aspect: {aspectRatio:F2} (min: {minLandscapeAspect:F2})");
        Debug.Log($"Prompt: {(rotationPromptCanvas != null && rotationPromptCanvas.gameObject.activeSelf ? "SHOWN" : "HIDDEN")}");
        Debug.Log("=========================");
    }

    [ContextMenu("🔄 Force Check")]
    void Context_ForceCheck()
    {
        CheckOrientation();
    }

    [ContextMenu("📱 Test: Force Mobile")]
    void Context_ForceMobile()
    {
        isMobileDevice = true;
        isLandscape = false;
        CheckOrientation();
        Debug.Log("✓ Forced mobile portrait mode for testing");
    }
}