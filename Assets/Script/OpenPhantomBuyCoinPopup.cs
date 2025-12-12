using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    public string kulinoCoinMint = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";

    [Header("🌐 DEX URLs")]
    public string jupiterSwapURL = "https://jup.ag/swap/SOL-E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
    public string phantomBrowserURL = "https://phantom.app/ul/browse/https%3A%2F%2Fjup.ag%2Fswap%2FSOL-E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    // ✅ Store callback
    private Action onContinueCallback;

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
    /// ✅ FIXED: Show with callback support
    /// </summary>
    public void Show(string title, string message, Action onContinue = null)
    {
        if (rootPopup != null)
            rootPopup.SetActive(true);

        if (blurOverlay != null)
            blurOverlay.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        // Store callback
        onContinueCallback = onContinue;

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

        onContinueCallback = null;

        Log("✓ Popup closed");
    }

    void OnContinueClicked()
    {
        Log("Continue clicked");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // ✅ Execute callback if provided
        if (onContinueCallback != null)
        {
            onContinueCallback.Invoke();
        }
        else
        {
            // Default: Open DEX
            OpenKulinoCoinSwap();
        }

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

    void OpenKulinoCoinSwap()
    {
        bool isMobile = IsMobileDevice();
        string targetURL = isMobile ? phantomBrowserURL : jupiterSwapURL;

        Log($"Opening DEX: {(isMobile ? "MOBILE" : "DESKTOP")} → {targetURL}");

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string jsCode = $"window.open('{targetURL}', '_blank');";
            Application.ExternalEval(jsCode);
            Log($"✓ Opened: {targetURL}");
        }
        catch (System.Exception ex)
        {
            LogError($"Failed to open URL: {ex.Message}");
            Application.OpenURL(targetURL);
        }
#else
        Application.OpenURL(targetURL);
        Log($"🧪 EDITOR: Opened URL: {targetURL}");
#endif
    }

    bool IsMobileDevice()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#elif UNITY_WEBGL
        if (Application.isMobilePlatform)
            return true;
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
}