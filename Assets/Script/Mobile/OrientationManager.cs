using UnityEngine;
using System.Collections;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FINAL v3.0: Simplified OrientationManager
/// - Minimal Unity intervention
/// - Let HTML handle most of the work
/// - Only provide query functions
/// </summary>
[DefaultExecutionOrder(-800)]
public class OrientationManager : MonoBehaviour
{
    public static OrientationManager Instance { get; private set; }

    [Header("📊 Status (Read Only)")]
    [SerializeField] private bool isMobileDevice = false;
    [SerializeField] private bool isLandscape = true;
    [SerializeField] private int screenWidth = 0;
    [SerializeField] private int screenHeight = 0;
    [SerializeField] private float aspectRatio = 1.0f;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private float checkInterval = 1f; // Check every 1 second
    private float lastCheckTime = 0f;
    
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern bool IsPortraitMode();
    [DllImport("__Internal")] private static extern bool IsMobileBrowser();
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[OrientationManager]";

        DetectPlatform();
        Log("✅ Initialized v3.0");
    }

    void Start()
    {
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

    void DetectPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            isMobileDevice = IsMobileBrowser();
        }
        catch
        {
            // Fallback
            isMobileDevice = Application.isMobilePlatform || Screen.width < 900;
        }
#elif UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
#else
        isMobileDevice = Screen.width < 900;
#endif
        
        Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
    }

    void CheckOrientation()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        aspectRatio = (float)screenWidth / screenHeight;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (isMobileDevice)
        {
            try
            {
                bool portrait = IsPortraitMode();
                isLandscape = !portrait;
            }
            catch
            {
                isLandscape = aspectRatio >= 1.15f;
            }
        }
        else
        {
            isLandscape = true;
        }
#else
        isLandscape = aspectRatio >= 1.15f;
#endif

        // Only log on state change
        if (lastCheckTime > 0) // Skip first check
        {
            Log($"{(isLandscape ? "LANDSCAPE" : "PORTRAIT")} | {screenWidth}x{screenHeight}");
        }
    }

    // ===== PUBLIC API =====
    public bool IsMobile() => isMobileDevice;
    public bool IsLandscapeMode() => isLandscape;
    public Vector2Int GetScreenSize() => new Vector2Int(screenWidth, screenHeight);
    public float GetAspectRatio() => aspectRatio;

    void Log(string m) 
    { 
        if (enableDebugLogs) 
            Debug.Log($"[Orient] {m}"); 
    }

    [ContextMenu("📊 Print Status")]
    void PrintStatus()
    {
        Debug.Log("=== ORIENTATION STATUS ===");
        Debug.Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Mode: {(isLandscape ? "LANDSCAPE" : "PORTRAIT")}");
        Debug.Log($"Screen: {screenWidth}x{screenHeight}");
        Debug.Log($"Aspect: {aspectRatio:F2}");
        Debug.Log("=========================");
    }
}