using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ MobileControlManager - FIXED v4.0
/// Sekarang re-bind & re-apply visibility TIAP scene load (bukan cuma sekali),
/// jadi gak nunggu restart buat kerja bener.
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

    void OnEnable()
    {
        // ✅ Subscribe ke event scene load, bukan cuma ngandelin Start() sekali doang
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        Setup();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ✅ Tiap scene (termasuk reload/restart Gameplay) → setup ulang
        Log($"Scene loaded: {scene.name} → re-setup mobile controls");
        Setup();
    }

    void Setup()
    {
        bool isMobile = CheckIsMobile();
        Log($"Browser: {(isMobile ? "MOBILE" : "DESKTOP")}");
        BindButtons();
        ApplyVisibility(isMobile);
    }

    bool CheckIsMobile()
    {
#if UNITY_EDITOR
        if (forceMobileInEditor)
        {
            Log("Editor mode: forceMobileInEditor = true → MOBILE");
            return true;
        }
        Log("Editor mode: forceMobileInEditor = false → DESKTOP");
        return false;
#elif UNITY_WEBGL
        try { return IsMobileBrowser() == 1; }
        catch { return false; }
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
        // ✅ includeInactive biar tetap ketemu walau Rocket lagi nonaktif sesaat
        return FindFirstObjectByType<PlayerLaneMovement>(FindObjectsInactive.Include);
    }

    Button FindButton(string btnName)
    {
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