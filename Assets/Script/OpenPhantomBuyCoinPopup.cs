using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ Popup untuk beli Kulino Coin via Phantom Wallet
/// Attach ke: OpenPhantomBuyCoin GameObject
/// </summary>
public class OpenPhantomBuyCoinPopup : MonoBehaviour
{
    public static OpenPhantomBuyCoinPopup Instance { get; private set; }

    [Header("🎨 UI References")]
    [Tooltip("Root GameObject popup")]
    public GameObject rootPopup;

    [Tooltip("Blur overlay background")]
    public GameObject blurOverlay;

    [Header("🔘 Buttons")]
    [Tooltip("Continue button (kuning) - buka Phantom")]
    public Button continueButton;

    [Tooltip("Cancel button (biru) - close popup")]
    public Button cancelButton;

    [Header("📝 Text Display (Optional)")]
    public TMP_Text titleText;
    public TMP_Text messageText;

    [Header("⚙️ Settings")]
    [Tooltip("URL untuk buy Kulino Coin")]
    public string buyKulinoCoinURL = "https://phantom.app/";

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenPhantomWallet(string url);
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find components
        if (rootPopup == null)
            rootPopup = gameObject;

        if (continueButton == null)
        {
            var continueObj = transform.Find("Continue");
            if (continueObj != null)
                continueButton = continueObj.GetComponent<Button>();
        }

        if (cancelButton == null)
        {
            var cancelObj = transform.Find("Cancel");
            if (cancelObj != null)
                cancelButton = cancelObj.GetComponent<Button>();
        }

        SetupButtons();
    }

    void Start()
    {
        // Hide popup initially
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

        // Setup blur overlay close
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
    /// Show popup dengan custom message
    /// </summary>
    public void Show(string title = "Not Enough Kulino Coin", string message = "Not Enough Kulino Coin, Go to buy some?")
    {
        if (rootPopup != null)
            rootPopup.SetActive(true);

        if (blurOverlay != null)
            blurOverlay.SetActive(true);

        // Update text
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        Log($"✓ Popup shown: {title}");
    }

    /// <summary>
    /// Close popup
    /// </summary>
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
        Log("Continue button clicked → Opening Phantom Wallet");

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // Open Phantom Wallet
        OpenPhantomWalletToBuy();

        // Close popup
        Close();
    }

    void OnCancelClicked()
    {
        Log("Cancel button clicked");

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        Close();
    }

    void OpenPhantomWalletToBuy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // ✅ Call JavaScript function untuk buka Phantom
            string jsCode = $"window.open('{buyKulinoCoinURL}', '_blank');";
            Application.ExternalEval(jsCode);
            
            Log($"✓ Opened Phantom Wallet: {buyKulinoCoinURL}");
        }
        catch (System.Exception ex)
        {
            LogError($"Failed to open Phantom: {ex.Message}");
            // Fallback
            Application.OpenURL(buyKulinoCoinURL);
        }
#else
        // Editor mode: just open URL
        Application.OpenURL(buyKulinoCoinURL);
        Log($"🧪 EDITOR: Opened URL: {buyKulinoCoinURL}");
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

    [ContextMenu("🧪 Test: Close Popup")]
    void Test_Close()
    {
        Close();
    }
}