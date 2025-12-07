using UnityEngine;
using System.Collections;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED: OrientationManager - Detection Only (no JS notification)
/// </summary>
[DefaultExecutionOrder(-800)]
public class OrientationManager : MonoBehaviour
{
    public static OrientationManager Instance { get; private set; }

    [Header("⚙️ Settings")]
    public float checkInterval = 0.3f;
    public float minLandscapeAspect = 1.2f;

    [Header("📊 Status (Read Only)")]
    [SerializeField] private bool isMobileDevice = false;
    [SerializeField] private bool isLandscape = true;
    [SerializeField] private int screenWidth = 0;
    [SerializeField] private int screenHeight = 0;
    [SerializeField] private float aspectRatio = 1.0f;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = false;

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
        SetupOrientation();
        Log("✅ Init");
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
            isMobileDevice = Application.isMobilePlatform || Screen.width < 768;
        }
#elif UNITY_ANDROID || UNITY_IOS
        isMobileDevice = true;
#else
        isMobileDevice = Screen.width < 768;
#endif
        Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
    }

    void SetupOrientation()
    {
#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Landscape;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
#endif
    }

    void CheckOrientation()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        aspectRatio = (float)screenWidth / screenHeight;

        bool wasLandscape = isLandscape;

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
                isLandscape = aspectRatio >= minLandscapeAspect;
            }
        }
        else
        {
            isLandscape = true;
        }
#else
        isLandscape = aspectRatio >= minLandscapeAspect;
#endif

        if (!isMobileDevice) return;

        if (!isLandscape && wasLandscape != isLandscape)
        {
            Log($"⚠️ PORTRAIT {screenWidth}x{screenHeight}");
        }
        else if (isLandscape && wasLandscape != isLandscape)
        {
            Log($"✓ LANDSCAPE {screenWidth}x{screenHeight}");
        }
    }

    public bool IsMobile() => isMobileDevice;
    public bool IsLandscapeMode() => isLandscape;
    public Vector2Int GetScreenSize() => new Vector2Int(screenWidth, screenHeight);
    public float GetAspectRatio() => aspectRatio;

    void Log(string m) { if (enableDebugLogs) Debug.Log($"[Orient] {m}"); }

    [ContextMenu("Status")]
    void PrintStatus()
    {
        Debug.Log("=== ORIENTATION ===");
        Debug.Log($"Platform: {(isMobileDevice ? "MOBILE" : "DESKTOP")}");
        Debug.Log($"Mode: {(isLandscape ? "LANDSCAPE" : "PORTRAIT")}");
        Debug.Log($"Screen: {screenWidth}x{screenHeight}");
        Debug.Log($"Aspect: {aspectRatio:F2}");
        Debug.Log("===================");
    }
}