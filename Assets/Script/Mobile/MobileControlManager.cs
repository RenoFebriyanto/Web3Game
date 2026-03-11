using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Deteksi mobile browser via jslib, lalu aktif/nonaktifkan
/// MobileControls GameObject di Scene Gameplay.
/// 
/// CARA SETUP:
/// 1. Attach script ini ke GameManager atau GameObject manapun di Scene Gameplay
/// 2. Drag assign "mobileControlsRoot" ke GameObject MobileControls di Hierarchy
/// </summary>
public class MobileControlManager : MonoBehaviour
{
    [Header("Assign MobileControls GameObject di sini")]
    [Tooltip("GameObject parent yang berisi BtnLeft dan BtnRight")]
    public GameObject mobileControlsRoot;

    // Import fungsi dari MobileOrientation.jslib
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsMobileBrowser();
#endif

    void Start()
    {
        bool isMobile = CheckIsMobile();
        ApplyMobileControls(isMobile);

        Debug.Log($"[MobileControlManager] Browser: {(isMobile ? "MOBILE" : "DESKTOP")} → MobileControls: {(isMobile ? "ACTIVE" : "INACTIVE")}");
    }

    bool CheckIsMobile()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return IsMobileBrowser() == 1;
#elif UNITY_EDITOR
        // Ganti ke true untuk test tampilan mobile di Editor
        return false;
#else
        return false;
#endif
    }

    void ApplyMobileControls(bool isMobile)
    {
        if (mobileControlsRoot == null)
        {
            Debug.LogWarning("[MobileControlManager] mobileControlsRoot belum di-assign!");
            return;
        }

        mobileControlsRoot.SetActive(isMobile);
    }
}