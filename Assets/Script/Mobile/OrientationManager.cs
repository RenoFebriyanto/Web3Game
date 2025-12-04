using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED v2.0: OrientationManager - Mobile Detection Fixed
/// - Better mobile detection
/// - Proper landscape enforcement
/// - JavaScript sync
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
    public float checkInterval = 0.2f;
    
    [Tooltip("Minimum aspect ratio untuk landscape (width/height)")]
    public float minLandscapeAspect = 1.2f;

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
    [DllImport("__Internal")]
    private static extern bool IsPortraitMode();
    
    [DllImport("__Internal")]
    private static extern bool IsMobileBrowser();
    
    [DllImport("__Internal")]
    private static extern void ShowRotationPromptJS();
    
    [DllImport("__Internal")]
    private static extern void HideRotationPromptJS();
#endif

    void Awake()
    {
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
        
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[OrientationManager - PERSISTENT]";

        DetectPlatform();
        SetupOrientation();
        
        Log("✅ OrientationManager initialized and marked persistent");
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Log($"Scene loaded: {scene.name} - Re-checking orientation");
        
        CheckOrientation();
        
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
        if (rotationPromptCanvas != null)
        {
            rotationPromptCanvas.gameObject.SetActive(false);
        }

        if (rotationPromptText != null)
        {
            rotationPromptText.text = "📱 Please rotate your device\nto landscape mode";
        }

        CheckOrientation();
    }

    void Update()
    {
        if (Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            CheckOrientation();
        }
    }

    /// <summary>
    /// ✅ IMPROVED: Better mobile detection
    /// </summary>
    void DetectPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            isMobileDevice = IsMobileBrowser();
            Log($"✅ WebGL mobile detection: {isMobileDevice}");
        }
        catch
        {
            // Fallback to Unity's detection
            isMobileDevice = Application.isMobilePlatform || 
                           Screen.width < 768 || 
                           (Screen.width < Screen.height * 1.5f && Screen.width < 1024);
            Log($"⚠️ Fallback mobile detection: {isMobileDevice}");
        }
#elif UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
        Log("✅ Native mobile platform detected");
#else
        // ✅ Desktop detection
        isMobileDevice = Screen.width < 768;
        Log($"🖥️ Desktop mode (width: {Screen.width})");
#endif

        Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Log($"Screen: {Screen.width}x{Screen.height}");
        Log($"Aspect: {(float)Screen.width / Screen.height:F2}");
    }

    void SetupOrientation()
    {
#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Landscape;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Log("✓ Native orientation: Landscape");
#endif
        
        Log("✓ Orientation setup complete");
    }

    /// <summary>
    /// ✅ IMPROVED: Better orientation detection
    /// </summary>
    void CheckOrientation()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        aspectRatio = (float)screenWidth / screenHeight;

        bool wasLandscape = isLandscape;

        // ✅ IMPROVED: Multi-method detection
#if UNITY_WEBGL && !UNITY_EDITOR
        if (isMobileDevice)
        {
            try
            {
                // Method 1: JavaScript detection
                bool isPortrait = IsPortraitMode();
                isLandscape = !isPortrait;
                
                if (enableDebugLogs)
                {
                    Log($"📱 JS Portrait Mode: {isPortrait}");
                }
            }
            catch
            {
                // Method 2: Aspect ratio fallback
                isLandscape = aspectRatio >= minLandscapeAspect;
                
                if (enableDebugLogs)
                {
                    Log($"📐 Fallback aspect detection: {aspectRatio:F2} >= {minLandscapeAspect:F2} = {isLandscape}");
                }
            }
        }
        else
        {
            isLandscape = true; // Desktop always OK
        }
#else
        // Native or Editor: Use aspect ratio
        isLandscape = aspectRatio >= minLandscapeAspect;
        
        if (enableDebugLogs)
        {
            Log($"📐 Native aspect: {aspectRatio:F2} >= {minLandscapeAspect:F2} = {isLandscape}");
        }
#endif

        // ✅ Only process if mobile
        if (!isMobileDevice)
        {
            HidePrompt();
            return;
        }

        // ✅ Show/hide prompt
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

    void ShowPrompt()
    {
        // Unity Canvas
        if (rotationPromptCanvas != null && !rotationPromptCanvas.gameObject.activeSelf)
        {
            rotationPromptCanvas.gameObject.SetActive(true);
            Log("✓ Unity rotation prompt shown");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
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
        try
        {
            HideRotationPromptJS();
        }
        catch { }
#endif
    }

    public bool IsMobile() => isMobileDevice;
    public bool IsLandscapeMode() => isLandscape;
    public Vector2Int GetScreenSize() => new Vector2Int(screenWidth, screenHeight);
    public float GetAspectRatio() => aspectRatio;

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
        DetectPlatform();
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