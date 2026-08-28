using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// ✅ PlatformDetector - SATU-SATUNYA sumber kebenaran untuk status mobile/desktop.
///
/// Sebelumnya MobileControlManager, MobileInputHelper, MobileOptimizer, dan
/// OrientationManager masing-masing punya deteksi sendiri (dengan fallback yang
/// beda-beda) → bisa saling tidak sinkron (misal tombol dianggap "mobile" tapi
/// quality setting dianggap "desktop").
///
/// SEKARANG: deteksi cuma dilakukan SEKALI di sini, lalu semua script lain
/// tinggal baca PlatformDetector.Instance.IsMobile.
///
/// SETUP:
/// 1. Taruh GameObject kosong di scene awal (bootstrap scene), attach script ini.
/// 2. Pastikan execution order-nya paling awal (sudah di-set -1000 lewat atribut
///    di bawah), supaya Awake()-nya jalan SEBELUM MobileInputHelper (-900),
///    MobileOptimizer (-900), dan OrientationManager (-800).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class PlatformDetector : MonoBehaviour
{
    public static PlatformDetector Instance { get; private set; }

    [Header("Editor Testing")]
    [Tooltip("Centang ini agar dianggap MOBILE saat Play di Editor (untuk test tampilan/kontrol HP)")]
    public bool forceMobileInEditor = true;

    [Header("Manual Override (opsional, semua platform)")]
    [Tooltip("Kalau dicentang, override hasil auto-detect dengan nilai manualIsMobile di bawah")]
    public bool manualOverride = false;
    public bool manualIsMobile = false;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    /// <summary>Status final: true = mobile, false = desktop. Sudah final begitu Awake() selesai.</summary>
    public bool IsMobile { get; private set; }

    /// <summary>Event ini dipanggil sekali setelah deteksi selesai (atau tiap kali Redetect() dipanggil).</summary>
    public delegate void PlatformChanged(bool isMobile);
    public static event PlatformChanged OnPlatformDetected;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsMobileBrowser();
#endif

    void Awake()
    {
        // Singleton + persist antar scene, sama seperti manager mobile lainnya
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[PlatformDetector]";

        Detect();
    }

    void Detect()
    {
        if (manualOverride)
        {
            IsMobile = manualIsMobile;
            Log($"Manual override aktif → {(IsMobile ? "MOBILE" : "DESKTOP")}");
        }
#if UNITY_EDITOR
        else if (forceMobileInEditor)
        {
            IsMobile = true;
            Log("Editor mode: forceMobileInEditor = true → MOBILE");
        }
        else
        {
            IsMobile = false;
            Log("Editor mode: forceMobileInEditor = false → DESKTOP");
        }
#elif UNITY_ANDROID || UNITY_IOS
        else
        {
            IsMobile = true;
            Log("Native mobile build (Android/iOS) → MOBILE");
        }
#elif UNITY_WEBGL
        else
        {
            try
            {
                IsMobile = IsMobileBrowser() == 1;
                Log($"JS IsMobileBrowser() → {(IsMobile ? "MOBILE" : "DESKTOP")}");
            }
            catch
            {
                IsMobile = Application.isMobilePlatform || Screen.width < 900;
                Log($"⚠️ JS plugin gagal, fallback isMobilePlatform/Screen.width → {(IsMobile ? "MOBILE" : "DESKTOP")}");
            }
        }
#else
        else
        {
            IsMobile = Application.isMobilePlatform;
            Log($"Fallback Application.isMobilePlatform → {(IsMobile ? "MOBILE" : "DESKTOP")}");
        }
#endif

        OnPlatformDetected?.Invoke(IsMobile);
    }

    /// <summary>Panggil ini kalau perlu deteksi ulang secara manual (misal setelah ganti override lewat kode lain).</summary>
    public void Redetect()
    {
        Detect();
    }

    void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[PlatformDetector] {msg}");
    }

    [ContextMenu("📱 Force Mobile")]
    void Context_ForceMobile()
    {
        manualOverride = true;
        manualIsMobile = true;
        Detect();
    }

    [ContextMenu("🖥️ Force Desktop")]
    void Context_ForceDesktop()
    {
        manualOverride = true;
        manualIsMobile = false;
        Detect();
    }

    [ContextMenu("🔄 Re-Detect (clear override)")]
    void Context_ReDetect()
    {
        manualOverride = false;
        Detect();
    }
}
