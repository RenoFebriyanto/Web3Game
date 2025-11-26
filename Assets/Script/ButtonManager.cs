using UnityEngine;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Simple button manager for switching side panels (Level / Quest / Shop).
/// Attach this script to a manager GameObject and wire Level/Quest/Shop in the Inspector.
/// Hook each UI Button's OnClick() to the corresponding ShowX method (ShowLevel/ShowQuest/ShowShop).
/// 
/// NEW: Added support for 3 economy buttons (Coin/Shard/Energy) to open Shop
/// NEW: Added Exit button logic for WebGL games
/// </summary>
public class ButtonManager : MonoBehaviour
{
    [Header("Panels (assign in Inspector)")]
    public GameObject Level;
    public GameObject Quest;
    public GameObject Shop;

    [Header("⌨️ Keyboard Shortcuts (Desktop Only)")]
[Tooltip("Enable keyboard shortcuts? (Auto-disabled on mobile)")]
public bool enableKeyboardShortcuts = true;

    [Header("Exit Settings (for WebGL)")]
    [Tooltip("URL to redirect when Exit button is clicked (for WebGL builds)")]
    public string exitURL = "https://your-game-website.com";

    [Tooltip("Enable exit confirmation popup")]
    public bool showExitConfirmation = true;

#if UNITY_WEBGL
    // Import JavaScript function untuk redirect (WebGL only)
    [DllImport("__Internal")]
    private static extern void RedirectToURL(string url);
#endif

    void Start()
    {
        // Default: show Level, hide others (adjust if you want a different default)
        SetActiveSafe(Level, true);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, false);
    }

    // Small helper to avoid null checks repetition
    void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ========================================
    // PANEL SWITCHING METHODS
    // ========================================

    // Public methods for UI Buttons (call these from Button OnClick)
    public void ShowLevel()
    {
        // if already active, do nothing (prevents flicker)
        if (Level != null && Level.activeSelf) return;

        SetActiveSafe(Level, true);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, false);

        Debug.Log("[ButtonManager] Switched to Level panel");
    }

    public void ShowQuest()
    {
        if (Quest != null && Quest.activeSelf) return;

        SetActiveSafe(Level, false);
        SetActiveSafe(Quest, true);
        SetActiveSafe(Shop, false);

        Debug.Log("[ButtonManager] Switched to Quest panel");
    }

    public void ShowShop()
    {
        if (Shop != null && Shop.activeSelf) return;

        SetActiveSafe(Level, false);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, true);

        Debug.Log("[ButtonManager] Switched to Shop panel");
    }

    // ========================================
    // ECONOMY BUTTONS (Coin/Shard/Energy)
    // ========================================

    /// <summary>
    /// Called when Coin button (top-right) is clicked.
    /// Opens Shop panel to buy coins.
    /// Hook this to Coin button's OnClick() in Inspector.
    /// </summary>
    public void OnCoinButtonClicked()
    {
        Debug.Log("[ButtonManager] Coin button clicked - Opening Shop");
        ShowShop();

        // Optional: Play click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// Called when Shard button (top-right) is clicked.
    /// Opens Shop panel to buy shards.
    /// Hook this to Shard button's OnClick() in Inspector.
    /// </summary>
    public void OnShardButtonClicked()
    {
        Debug.Log("[ButtonManager] Shard button clicked - Opening Shop");
        ShowShop();

        // Optional: Play click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// Called when Energy button (top-right) is clicked.
    /// Opens Shop panel to buy energy.
    /// Hook this to Energy button's OnClick() in Inspector.
    /// </summary>
    public void OnEnergyButtonClicked()
    {
        Debug.Log("[ButtonManager] Energy button clicked - Opening Shop");
        ShowShop();

        // Optional: Play click sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    // ========================================
    // EXIT BUTTON (WebGL)
    // ========================================

    /// <summary>
    /// Called when Exit button (bottom-left) is clicked.
    /// For WebGL: Redirects to exitURL
    /// For Editor/Standalone: Quits application
    /// Hook this to Exit button's OnClick() in Inspector.
    /// </summary>
    public void OnExitButtonClicked()
    {
        Debug.Log("[ButtonManager] Exit button clicked");

        if (showExitConfirmation)
        {
            // Show confirmation dialog (optional)
            // You can create a popup here if needed
            Debug.Log("[ButtonManager] Exit confirmation enabled - implement popup if needed");
        }

        ExitGame();
    }

    /// <summary>
    /// Exits the game based on platform
    /// </summary>
    void ExitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL build: Redirect to URL
        Debug.Log($"[ButtonManager] Redirecting to: {exitURL}");
        
        try
        {
            RedirectToURL(exitURL);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ButtonManager] Failed to redirect: {e.Message}");
            // Fallback: try using Application.OpenURL
            Application.OpenURL(exitURL);
        }
#elif UNITY_EDITOR
        // Editor: Stop play mode
        Debug.Log("[ButtonManager] Stopping Play Mode (Editor)");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Standalone build: Quit application
        Debug.Log("[ButtonManager] Quitting application");
        Application.Quit();
#endif
    }

    /// <summary>
    /// Alternative exit method without confirmation
    /// </summary>
    public void ExitGameDirect()
    {
        Debug.Log("[ButtonManager] Direct exit (no confirmation)");
        ExitGame();
    }

    void Update()
{
    // ✅ Skip keyboard input on mobile devices
    bool isMobile = Application.isMobilePlatform;
    if (isMobile || !enableKeyboardShortcuts)
    {
        return;
    }

    // Keyboard shortcuts (desktop only)
    if (Input.GetKeyDown(KeyCode.Alpha1)) ShowLevel();
    if (Input.GetKeyDown(KeyCode.Alpha2)) ShowQuest();
    if (Input.GetKeyDown(KeyCode.Alpha3)) ShowShop();

    // ESC untuk exit (testing)
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        Debug.Log("[ButtonManager] ESC pressed - triggering exit");
        OnExitButtonClicked();
    }
}
}