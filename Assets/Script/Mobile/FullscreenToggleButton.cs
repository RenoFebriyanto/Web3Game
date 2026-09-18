using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 🔳 FullscreenToggleButton - Satu tombol untuk masuk/keluar fullscreen,
/// jalan sama-sama di HP maupun laptop (WebGL).
///
/// CARA PAKAI:
/// 1. Attach script ini ke GameObject tombol (yang sudah punya component Button + Image).
///    Kalau iconnya ada di child terpisah, drag Image child itu ke field "iconImage".
/// 2. Isi field "spriteEnterFullscreen" dengan Zoom-In.png (kondisi awal / belum fullscreen).
/// 3. Isi field "spriteExitFullscreen" dengan Zoom-Out.png (kondisi setelah fullscreen aktif).
/// 4. Tidak perlu setting apa-apa lagi, tombol otomatis:
///    - Toggle Screen.fullScreen tiap ditekan
///    - Ganti sprite sesuai state
///    - Ikut sinkron kalau user keluar fullscreen manual (misal tekan ESC di browser)
///
/// CATATAN WEBGL:
/// - Screen.fullScreen di WebGL build otomatis manggil browser Fullscreen API
///   (canvas.requestFullscreen), tapi WAJIB dipanggil dari dalam event klik/tap
///   user (bukan dari Start/Update) — makanya di sini dipanggil dari OnClick tombol.
/// - Beberapa browser mobile (terutama Safari di iPhone) TIDAK mendukung
///   Fullscreen API untuk elemen biasa, jadi tombol ini mungkin tidak
///   berefek di iOS Safari — itu keterbatasan browser, bukan bug script.
///   Kalau nanti perlu, bisa ditambah fallback lewat plugin .jslib khusus iOS.
/// </summary>
[DefaultExecutionOrder(-700)]
[RequireComponent(typeof(Button))]
public class FullscreenToggleButton : MonoBehaviour
{
    [Header("🖼️ Sprite Icon")]
    [Tooltip("Image yang nampilin icon zoom-in/zoom-out. Kalau kosong, otomatis ambil dari GameObject ini.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Sprite saat BELUM fullscreen (kondisi awal). Tap tombol ini untuk MASUK fullscreen.")]
    [SerializeField] private Sprite spriteEnterFullscreen; // Zoom-In.png

    [Tooltip("Sprite saat SUDAH fullscreen. Tap tombol ini untuk KELUAR fullscreen.")]
    [SerializeField] private Sprite spriteExitFullscreen; // Zoom-Out.png

    [Header("🐛 Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private Button _button;
    private bool _lastFullscreenState;

    void Awake()
    {
        _button = GetComponent<Button>();

        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (iconImage == null)
            Debug.LogWarning("[FullscreenToggleButton] ⚠️ iconImage tidak diset dan tidak ketemu Image di GameObject ini — sprite tidak akan berubah.");
    }

    void OnEnable()
    {
        _button.onClick.AddListener(ToggleFullscreen);
    }

    void OnDisable()
    {
        _button.onClick.RemoveListener(ToggleFullscreen);
    }

    void Start()
    {
        // Set tampilan awal sesuai state fullscreen aktual saat game mulai
        _lastFullscreenState = Screen.fullScreen;
        UpdateIcon(_lastFullscreenState);
    }

    void Update()
    {
        // ✅ Sinkronisasi kalau fullscreen berubah dari luar tombol ini,
        // contoh: user tekan ESC di keyboard (desktop browser) untuk keluar fullscreen.
        if (Screen.fullScreen != _lastFullscreenState)
        {
            _lastFullscreenState = Screen.fullScreen;
            UpdateIcon(_lastFullscreenState);
            Log($"Fullscreen berubah dari luar tombol → {(_lastFullscreenState ? "FULLSCREEN" : "NORMAL")}");

            // ✅ FIX BLACKSCREEN: exit fullscreen via ESC juga rawan canvas
            // tidak resize otomatis di laptop — paksa sync ulang.
            if (!_lastFullscreenState)
                StartCoroutine(ForceCanvasResyncAfterExit());
        }
    }

    /// <summary>
    /// Dipanggil saat tombol ditekan. Sama untuk HP maupun laptop —
    /// Unity/browser yang urus perbedaan platformnya di baliknya.
    /// </summary>
    public void ToggleFullscreen()
    {
        bool goFullscreen = !Screen.fullScreen;
        Screen.fullScreen = goFullscreen;

        _lastFullscreenState = goFullscreen;
        UpdateIcon(goFullscreen);

        Log($"Tombol ditekan → {(goFullscreen ? "MASUK fullscreen" : "KELUAR fullscreen")}");

        // ✅ FIX BLACKSCREEN: saat KELUAR fullscreen, browser Fullscreen API
        // mengecilkan <canvas> secara async. Kalau Unity WebGL loader (ResizeObserver)
        // telat sinkron ukuran render buffer-nya (sering terjadi di laptop dengan
        // scaling/DPI beda), hasilnya 1 frame (atau lebih) canvas kosong = blackscreen.
        // Paksa browser & Unity resync ukurannya setelah transisi selesai.
        if (!goFullscreen)
            StartCoroutine(ForceCanvasResyncAfterExit());
    }

    /// <summary>
    /// Tunggu beberapa frame (transisi fullscreen di browser tidak instan),
    /// lalu paksa dispatch event 'resize' ke window supaya Unity WebGL loader
    /// (ResizeObserver internal) re-sync ukuran render buffer canvas ke ukuran
    /// CSS yang sebenarnya. Ini yang biasanya hilang dan menyebabkan blackscreen
    /// di laptop (di HP sering tidak kejadian karena Fullscreen API tidak selalu
    /// terpakai / browser mobile menangani resize secara native).
    /// </summary>
    private IEnumerator ForceCanvasResyncAfterExit()
    {
        // Tunggu 2 frame supaya browser benar-benar selesai resize elemen canvas
        // sebelum kita paksa Unity membaca ulang ukurannya.
        yield return null;
        yield return null;

#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval(@"
            try {
                var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
                if (canvas) {
                    // Paksa browser recalculate layout, lalu trigger resize event
                    // supaya ResizeObserver Unity loader jalan ulang.
                    canvas.style.width = '100%';
                    canvas.style.height = '100%';
                    void canvas.offsetHeight; // force reflow
                    window.dispatchEvent(new Event('resize'));
                    console.log('[FullscreenToggleButton] Forced canvas resync after fullscreen exit');
                }
            } catch (e) {
                console.error('[FullscreenToggleButton] Canvas resync failed:', e);
            }
        ");
#endif
        Log("✓ Forced canvas resync setelah keluar fullscreen (anti-blackscreen)");
    }

    private void UpdateIcon(bool isFullscreen)
    {
        if (iconImage == null) return;

        iconImage.sprite = isFullscreen ? spriteExitFullscreen : spriteEnterFullscreen;
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[FullscreenToggleButton] {message}");
    }

    [ContextMenu("🔳 Test Toggle Fullscreen")]
    private void Context_TestToggle()
    {
        ToggleFullscreen();
    }
}
