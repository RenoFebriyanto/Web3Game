using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Popup Alert untuk menampilkan pesan "Not Enough Energy"
/// Singleton pattern - hanya 1 instance di scene
/// Auto-hide setelah beberapa detik
/// </summary>
public class PopUpAlert : MonoBehaviour
{
    public static PopUpAlert Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("GameObject PopupAlert (dari hierarchy)")]
    public GameObject popupAlertObject;

    [Header("Text Reference")]
    [Tooltip("Text untuk message (optional - jika ingin custom text)")]
    public TMP_Text messageText;

    [Header("Settings")]
    [Tooltip("Duration sebelum auto-hide (detik)")]
    public float autoHideDuration = 2f;

    [Tooltip("Default message untuk not enough energy")]
    public string notEnoughEnergyMessage = "Not Enough Energy";

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private Coroutine autoHideCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Hide popup initially
        HidePopup();
    }

    /// <summary>
    /// Show "Not Enough Energy" popup
    /// </summary>
    public void ShowNotEnoughEnergy()
    {
        ShowPopup(notEnoughEnergyMessage);
    }

    /// <summary>
    /// Show popup dengan custom message
    /// </summary>
    public void ShowPopup(string message)
    {
        if (popupAlertObject == null)
        {
            LogError("popupAlertObject not assigned!");
            return;
        }

        // Update message text
        if (messageText != null && !string.IsNullOrEmpty(message))
        {
            messageText.text = message;
        }

        // Show popup
        popupAlertObject.SetActive(true);

        Log($"✓ Popup shown: {message}");

        // Start auto-hide timer
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }

        autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
    }

    /// <summary>
    /// Hide popup immediately
    /// </summary>
    public void HidePopup()
    {
        if (popupAlertObject != null)
        {
            popupAlertObject.SetActive(false);
        }

        // Stop auto-hide coroutine
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        Log("✓ Popup hidden");
    }

    /// <summary>
    /// Auto-hide popup setelah delay
    /// </summary>
    IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDuration);

        HidePopup();

        Log($"✓ Popup auto-hidden after {autoHideDuration}s");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[PopUpAlert] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[PopUpAlert] {message}");
    }

    [ContextMenu("Test: Show Not Enough Energy")]
    void Test_ShowNotEnoughEnergy()
    {
        ShowNotEnoughEnergy();
    }

    [ContextMenu("Test: Hide Popup")]
    void Test_HidePopup()
    {
        HidePopup();
    }
}