using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅ MobileControlManager - FIXED v3.0
/// Attach ke: GameManager
/// Assign di Inspector: drag Mobilecontrol → mobileControlsRoot
/// </summary>
public class MobileControlManager : MonoBehaviour
{
    [Header("Assign Mobilecontrol GameObject di sini")]
    public GameObject mobileControlsRoot;

    [Header("Editor Testing")]
    [Tooltip("Centang ini agar MobileControl AKTIF saat Play di Editor (untuk test resolusi HP)")]
    public bool forceMobileInEditor = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsMobileBrowser();
#endif

    void Start()
    {
        bool isMobile = CheckIsMobile();
        Log($"Browser: {(isMobile ? "MOBILE" : "DESKTOP")}");
        BindButtons();
        ApplyVisibility(isMobile);
    }

    bool CheckIsMobile()
    {
#if UNITY_EDITOR
        // Di Editor: gunakan forceMobileInEditor toggle dari Inspector
        if (forceMobileInEditor)
        {
            Log("Editor mode: forceMobileInEditor = true → MOBILE");
            return true;
        }
        Log("Editor mode: forceMobileInEditor = false → DESKTOP");
        return false;
#elif UNITY_WEBGL
        return IsMobileBrowser() == 1;
#else
        return Application.isMobilePlatform;
#endif
    }

    void BindButtons()
    {
        var movement = FindMovement();
        if (movement == null) { Log("❌ PlayerLaneMovement tidak ditemukan!"); return; }

        var btnLeft  = FindButton("BTNLEFT");
        var btnRight = FindButton("BTNRIGHT");

        if (btnLeft  != null) { btnLeft.onClick.RemoveAllListeners();  btnLeft.onClick.AddListener(movement.MoveLeft);  Log("✓ BTNLEFT  → MoveLeft()");  }
        if (btnRight != null) { btnRight.onClick.RemoveAllListeners(); btnRight.onClick.AddListener(movement.MoveRight); Log("✓ BTNRIGHT → MoveRight()"); }
    }

    PlayerLaneMovement FindMovement()
    {
        var rocket = GameObject.Find("Rocket");
        if (rocket != null)
        {
            var m = rocket.GetComponent<PlayerLaneMovement>();
            if (m != null) return m;
        }
        return FindFirstObjectByType<PlayerLaneMovement>();
    }

    Button FindButton(string btnName)
    {
        // Case-insensitive search di dalam mobileControlsRoot
        if (mobileControlsRoot != null)
        {
            foreach (Transform t in mobileControlsRoot.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, btnName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var btn = t.GetComponent<Button>();
                    if (btn != null) return btn;
                }
            }
        }
        // Fallback seluruh scene
        var go = GameObject.Find(btnName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    void ApplyVisibility(bool active)
    {
        if (mobileControlsRoot == null) { Log("❌ mobileControlsRoot belum diassign!"); return; }
        mobileControlsRoot.SetActive(active);
        Log($"MobileControl: {(active ? "AKTIF ✅" : "NONAKTIF ❌")}");
    }

    void Log(string msg) { if (enableDebugLogs) Debug.Log($"[MobileControlManager] {msg}"); }

    [ContextMenu("Simulate Mobile")]  void SimMobile()  => ApplyVisibility(true);
    [ContextMenu("Simulate Desktop")] void SimDesktop() => ApplyVisibility(false);
}