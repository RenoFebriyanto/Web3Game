using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handle level complete dan show PopupClaimCoinKulino
/// Attach di GameObject di Gameplay scene
/// </summary>
public class LevelCompleteHandler : MonoBehaviour
{
    [Header("Popup Reference")]
    [Tooltip("Drag PopupClaimCoinKulino GameObject dari hierarchy")]
    public GameObject popupClaimCoin;

    [Header("Popup Components (auto-find dari popup)")]
    [Tooltip("Coin icon image - akan auto-find jika null")]
    public Image coinIconImage;

    [Tooltip("Desk item text - akan auto-find jika null")]
    public TMP_Text deskItemText;

    [Header("Reward Settings")]
    [Tooltip("Jumlah Kulino token reward per level complete")]
    public int kulinoRewardAmount = 1;

    [Header("Auto-Find Components")]
    [Tooltip("Auto-find LevelGameSession")]
    public bool autoFindSession = true;

    private LevelGameSession session;
    private bool levelCompleted = false;

    void Start()
    {
        // Find session
        if (autoFindSession)
        {
            session = FindFirstObjectByType<LevelGameSession>();
            if (session == null)
            {
                Debug.LogWarning("[LevelCompleteHandler] LevelGameSession not found!");
            }
        }

        // Subscribe to level complete event
        if (session != null)
        {
            session.OnLevelCompleted.AddListener(OnLevelComplete);
            Debug.Log("[LevelCompleteHandler] ✓ Subscribed to OnLevelCompleted event");
        }

        // Hide popup initially
        if (popupClaimCoin != null)
        {
            popupClaimCoin.SetActive(false);
        }

        // Auto-find components dari popup
        if (popupClaimCoin != null)
        {
            AutoFindPopupComponents();
        }
    }

    void OnDestroy()
    {
        if (session != null)
        {
            session.OnLevelCompleted.RemoveListener(OnLevelComplete);
        }
    }

    /// <summary>
    /// Called ketika level complete (semua fragments collected)
    /// </summary>
    void OnLevelComplete()
    {
        if (levelCompleted)
        {
            Debug.LogWarning("[LevelCompleteHandler] Level already completed!");
            return;
        }

        levelCompleted = true;
        Debug.Log("[LevelCompleteHandler] ✓ Level Complete! Showing popup...");

        // Setup popup content
        SetupPopupContent();

        // Show popup
        ShowPopup();
    }

    /// <summary>
    /// Setup popup text dan icon untuk Kulino Token reward
    /// </summary>
    void SetupPopupContent()
    {
        // Set desk item text ke "Kulino Token"
        if (deskItemText != null)
        {
            deskItemText.text = "Kulino Token";
            Debug.Log("[LevelCompleteHandler] ✓ Set popup text: Kulino Token");
        }

        // Coin icon sudah ditampilkan di hierarchy, tidak perlu ubah
        if (coinIconImage != null)
        {
            coinIconImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Show popup
    /// </summary>
    void ShowPopup()
    {
        if (popupClaimCoin == null)
        {
            Debug.LogError("[LevelCompleteHandler] PopupClaimCoin not assigned!");
            return;
        }

        popupClaimCoin.SetActive(true);
        Debug.Log("[LevelCompleteHandler] ✓ Popup shown");

        // Pause game (optional)
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Auto-find components dari popup GameObject
    /// </summary>
    void AutoFindPopupComponents()
    {
        if (popupClaimCoin == null) return;

        // Find coin icon (cari GameObject bernama "Image" atau yang ada Image component)
        if (coinIconImage == null)
        {
            // Cari child dengan nama "Image"
            Transform imageTransform = popupClaimCoin.transform.Find("Image");
            if (imageTransform != null)
            {
                coinIconImage = imageTransform.GetComponent<Image>();
                Debug.Log("[LevelCompleteHandler] ✓ Auto-found coin icon");
            }
        }

        // Find DeskItem text
        if (deskItemText == null)
        {
            // Cari child dengan nama "DeskItem"
            Transform deskTransform = FindChildRecursive(popupClaimCoin.transform, "DeskItem");
            if (deskTransform != null)
            {
                deskItemText = deskTransform.GetComponent<TMP_Text>();
                Debug.Log("[LevelCompleteHandler] ✓ Auto-found DeskItem text");
            }
        }
    }

    /// <summary>
    /// Recursive search untuk find child by name
    /// </summary>
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Public method untuk manual trigger (testing)
    /// </summary>
    [ContextMenu("Test Level Complete")]
    public void TestLevelComplete()
    {
        OnLevelComplete();
    }

    /// <summary>
    /// Hide popup (called dari button atau setelah transaksi)
    /// </summary>
    public void HidePopup()
    {
        if (popupClaimCoin != null)
        {
            popupClaimCoin.SetActive(false);
        }

        // Resume game
        Time.timeScale = 1f;
    }
}