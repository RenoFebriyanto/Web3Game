using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ MobileControlManager - FIXED v4.1
/// - Re-bind & re-apply visibility TIAP scene load (bukan cuma sekali).
/// - FIX double-trigger: tidak lagi bind onClick sendiri ke tombol; MobileButton.cs
///   (nempel langsung di BTNLEFT/BTNRIGHT) adalah satu-satunya jalur input gerak.
/// </summary>
public class MobileControlManager : MonoBehaviour
{
    [Header("Assign Mobilecontrol GameObject di sini")]
    public GameObject mobileControlsRoot;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    void OnEnable()
    {
        // ✅ Subscribe ke event scene load, bukan cuma ngandelin Start() sekali doang
        SceneManager.sceneLoaded += OnSceneLoaded;
        // ✅ Subscribe ke PlatformDetector, jadi kalau force-mobile di-toggle SAAT game
        // sudah jalan (bukan cuma sebelum Play), tombol langsung ikut berubah
        PlatformDetector.OnPlatformDetected += OnPlatformChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PlatformDetector.OnPlatformDetected -= OnPlatformChanged;
    }

    void OnPlatformChanged(bool isMobile)
    {
        Log($"PlatformDetector berubah → {(isMobile ? "MOBILE" : "DESKTOP")}, re-setup mobile controls");
        Setup();
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
        VerifyButtons();
        ApplyVisibility(isMobile);
    }

    bool CheckIsMobile()
    {
        // ✅ Sekarang baca dari PlatformDetector (single source of truth),
        // bukan deteksi sendiri lagi.
        if (PlatformDetector.Instance != null)
        {
            Log($"PlatformDetector → {(PlatformDetector.Instance.IsMobile ? "MOBILE" : "DESKTOP")}");
            return PlatformDetector.Instance.IsMobile;
        }

        Log("⚠️ PlatformDetector.Instance belum ada (pastikan ada di scene awal, execution order -1000). Fallback ke DESKTOP.");
        return false;
    }

    void VerifyButtons()
    {
        // ✅ FIX double-trigger: dulu di sini di-bind onClick ke MoveLeft/MoveRight,
        // PADAHAL MobileButton.cs yang nempel di BTNLEFT/BTNRIGHT sudah menangani
        // gerakan lewat OnPointerDown. Kalau dua-duanya aktif bareng → 1 tap = 2 trigger
        // (OnPointerDown langsung saat ditekan + onClick saat dilepas) → karakter geser 2x.
        //
        // Sekarang MobileButton.cs jadi SATU-SATUNYA jalur input gerak.
        // Fungsi ini cuma untuk verifikasi setup + bersih-bersih listener lama.

        var btnLeft  = FindButton("BTNLEFT");
        var btnRight = FindButton("BTNRIGHT");

        // Bersihkan onClick listener runtime yang mungkin ke-attach dari versi lama
        if (btnLeft  != null) btnLeft.onClick.RemoveAllListeners();
        if (btnRight != null) btnRight.onClick.RemoveAllListeners();

        if (btnLeft != null && btnLeft.GetComponent<MobileButton>() == null)
            Log("⚠️ BTNLEFT tidak punya komponen MobileButton! Gerakan mobile tidak akan berfungsi — attach MobileButton.cs ke GameObject BTNLEFT (Direction = Left).");
        else if (btnLeft != null)
            Log("✓ BTNLEFT punya MobileButton, siap dipakai");

        if (btnRight != null && btnRight.GetComponent<MobileButton>() == null)
            Log("⚠️ BTNRIGHT tidak punya komponen MobileButton! Gerakan mobile tidak akan berfungsi — attach MobileButton.cs ke GameObject BTNRIGHT (Direction = Right).");
        else if (btnRight != null)
            Log("✓ BTNRIGHT punya MobileButton, siap dipakai");
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