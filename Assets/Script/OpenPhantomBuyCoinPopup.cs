using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED v2.0: Popup untuk beli Kulino Coin via Phantom Wallet
/// - Auto-redirect ke Jupiter DEX untuk swap SOL → Kulino Coin
/// - Support mobile & desktop
/// </summary>
public class OpenPhantomBuyCoinPopup : MonoBehaviour
{
    public static OpenPhantomBuyCoinPopup Instance { get; private set; }

    [Header("🎨 UI References")]
    public GameObject rootPopup;
    public GameObject blurOverlay;

    [Header("🔘 Buttons")]
    public Button continueButton;
    public Button cancelButton;

    [Header("📝 Text Display")]
    public TMP_Text titleText;
    public TMP_Text messageText;

    [Header("⚙️ Kulino Coin Settings")]
    [Tooltip("Mint address Kulino Coin")]
    public string kulinoCoinMint = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";

    [Header("🌐 DEX URLs")]
    [Tooltip("Desktop: Jupiter swap URL")]
    public string jupiterSwapURL = "https://jup.ag/swap/SOL-E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";

    [Tooltip("Mobile: Phantom browser deep link")]
    public string phantomBrowserURL = "https://phantom.app/ul/browse/https%3A%2F%2Fjup.ag%2Fswap%2FSOL-E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rootPopup == null)
            rootPopup = gameObject;

        SetupButtons();
    }

    void Start()
    {
        if (rootPopup != null)
            rootPopup.SetActive(false);

        if (blurOverlay != null)
            blurOverlay.SetActive(false);
    }

    void SetupButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        if (blurOverlay != null)
        {
            var blurBtn = blurOverlay.GetComponent<Button>();
            if (blurBtn == null)
            {
                blurBtn = blurOverlay.AddComponent<Button>();
                blurBtn.transition = Selectable.Transition.None;
            }
            blurBtn.onClick.RemoveAllListeners();
            blurBtn.onClick.AddListener(OnCancelClicked);
        }
    }

    /// <summary>
    /// ✅ FIXED: Show popup - support 2 atau 3 argumen
    /// Argumen ketiga (buttonText) optional untuk customize button text
    /// </summary>
    /// <param name="title">Popup title</param>
    /// <param name="message">Popup message</param>
    /// <param name="buttonText">Optional: Custom button text (default: "Continue")</param>
    public void Show(string title = "Not Enough Kulino Coin", string message = "Not Enough Kulino Coin, Go to buy some?", string buttonText = "")
    {
        if (rootPopup != null)
            rootPopup.SetActive(true);

        if (blurOverlay != null)
            blurOverlay.SetActive(true);

        // Set title
        if (titleText != null)
            titleText.text = title;

        // Set message
        if (messageText != null)
            messageText.text = message;

        // ✅ Optional: Update button text jika provided
        if (!string.IsNullOrEmpty(buttonText) && continueButton != null)
        {
            var btnText = continueButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = buttonText;
                Log($"✓ Button text updated: {buttonText}");
            }
        }

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        Log($"✓ Popup shown: {title}");
    }

    public void Close()
    {
        if (rootPopup != null)
            rootPopup.SetActive(false);

        if (blurOverlay != null)
            blurOverlay.SetActive(false);

        Log("✓ Popup closed");
    }

    void OnContinueClicked()
    {
        Log("Continue clicked → Opening DEX to buy Kulino Coin");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        OpenKulinoCoinSwap();
        Close();
    }

    void OnCancelClicked()
    {
        Log("Cancel clicked");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        Close();
    }

    /// <summary>
    /// ✅ FIXED: Open Jupiter DEX untuk swap SOL → Kulino Coin
    /// Auto-detect mobile/desktop
    /// </summary>
    void OpenKulinoCoinSwap()
    {
        bool isMobile = IsMobileDevice();
        string targetURL = isMobile ? phantomBrowserURL : jupiterSwapURL;

        Log($"Opening DEX: {(isMobile ? "MOBILE" : "DESKTOP")} → {targetURL}");

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // ✅ WebGL: Open in new tab
            string jsCode = $"window.open('{targetURL}', '_blank');";
            Application.ExternalEval(jsCode);
            
            Log($"✓ Opened: {targetURL}");
        }
        catch (System.Exception ex)
        {
            LogError($"Failed to open URL: {ex.Message}");
            
            // Fallback
            Application.OpenURL(targetURL);
        }
#else
        // ✅ Editor: Just open URL
        Application.OpenURL(targetURL);
        Log($"🧪 EDITOR: Opened URL: {targetURL}");
#endif
    }

    /// <summary>
    /// Detect mobile device
    /// </summary>
    bool IsMobileDevice()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#elif UNITY_WEBGL
        // Check via Application.isMobilePlatform
        if (Application.isMobilePlatform)
            return true;

        // Check screen size as fallback
        if (Screen.width < 1024)
            return true;

        return false;
#else
        return false;
#endif
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[PhantomBuyPopup] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[PhantomBuyPopup] ❌ {message}");
    }

    [ContextMenu("🧪 Test: Show Popup")]
    void Test_Show()
    {
        Show();
    }

    [ContextMenu("🧪 Test: Open Swap (Desktop)")]
    void Test_OpenSwapDesktop()
    {
        Application.OpenURL(jupiterSwapURL);
        Debug.Log($"[Test] Opened: {jupiterSwapURL}");
    }

    [ContextMenu("🧪 Test: Open Swap (Mobile)")]
    void Test_OpenSwapMobile()
    {
        Application.OpenURL(phantomBrowserURL);
        Debug.Log($"[Test] Opened: {phantomBrowserURL}");
    }
}