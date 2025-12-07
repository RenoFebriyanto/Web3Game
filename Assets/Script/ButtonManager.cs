using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED v2.0: ButtonManager dengan Auto-Reassign setelah scene load
/// - Auto-reassign panel references setelah scene transition
/// - Persistent across scenes
/// - Fallback mechanism jika reference hilang
/// </summary>
public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance { get; private set; }

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

    [Header("🔧 Auto-Reassign Settings")]
    [Tooltip("GameObject names untuk auto-find (case-sensitive)")]
    public string levelPanelName = "LevelP";
    public string questPanelName = "QuestP";
    public string shopPanelName = "ContentShop";

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private bool isPersistent = false;

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void RedirectToURL(string url);
#endif

    void Awake()
    {
        // ✅ Singleton pattern
        if (Instance != null)
        {
            if (Instance != this)
            {
                Log("Duplicate ButtonManager found - destroying");
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;

        // ✅ Make persistent
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        isPersistent = true;
        gameObject.name = "[ButtonManager - PERSISTENT]";

        Log("✅ ButtonManager initialized as PERSISTENT");

        // ✅ Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Unsubscribe
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// ✅ CRITICAL: Re-assign panel references setelah scene load
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"=== Scene Loaded: {scene.name} ===");

        // ✅ Wait 1 frame untuk ensure semua GameObject sudah loaded
        StartCoroutine(ReassignPanelsDelayed());
    }

    System.Collections.IEnumerator ReassignPanelsDelayed()
    {
        yield return null; // Wait 1 frame
        yield return null; // Wait 1 more frame untuk safety

        ReassignPanels();
    }

    /// <summary>
    /// ✅ Re-assign panel references dengan fallback mechanism
    /// </summary>
    void ReassignPanels()
    {
        Log("🔄 Re-assigning panel references...");

        // ✅ Check existing references first
        bool levelValid = Level != null;
        bool questValid = Quest != null;
        bool shopValid = Shop != null;

        Log($"Current state: Level={levelValid}, Quest={questValid}, Shop={shopValid}");

        // ✅ Re-assign jika null atau destroyed
        if (!levelValid)
        {
            Level = FindPanelByName(levelPanelName);
            Log($"Level panel: {(Level != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        }

        if (!questValid)
        {
            Quest = FindPanelByName(questPanelName);
            Log($"Quest panel: {(Quest != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        }

        if (!shopValid)
        {
            Shop = FindPanelByName(shopPanelName);
            Log($"Shop panel: {(Shop != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        }

        // ✅ Set initial state (Level active, others hidden)
        if (Level != null && Quest != null && Shop != null)
        {
            SetActiveSafe(Level, true);
            SetActiveSafe(Quest, false);
            SetActiveSafe(Shop, false);
            Log("✅ All panels reassigned and set to default state");
        }
        else
        {
            LogWarning("⚠️ Some panels still missing after reassign!");
        }

        Log("===========================================");
    }

    /// <summary>
    /// ✅ Find panel GameObject by name (recursive search)
    /// </summary>
    GameObject FindPanelByName(string panelName)
    {
        if (string.IsNullOrEmpty(panelName))
        {
            return null;
        }

        // Method 1: Direct find (fastest)
        GameObject found = GameObject.Find(panelName);
        if (found != null)
        {
            Log($"Found '{panelName}' via GameObject.Find");
            return found;
        }

        // Method 2: Search in Canvas
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            Transform result = SearchInChildren(canvas.transform, panelName);
            if (result != null)
            {
                Log($"Found '{panelName}' in Canvas: {canvas.name}");
                return result.gameObject;
            }
        }

        LogWarning($"❌ Panel '{panelName}' not found!");
        return null;
    }

    /// <summary>
    /// ✅ Recursive search in children
    /// </summary>
    Transform SearchInChildren(Transform parent, string targetName)
    {
        if (parent.name == targetName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = SearchInChildren(parent.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    void Start()
    {
        // ✅ Initial setup
        ReassignPanels();
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
            Log("ESC pressed - triggering exit");
            OnExitButtonClicked();
        }
    }

    void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) 
        {
            go.SetActive(active);
        }
        else
        {
            LogWarning($"Cannot set active: GameObject is null");
        }
    }

    // ========================================
    // PANEL SWITCHING METHODS
    // ========================================

    public void ShowLevel()
    {
        // ✅ Validate references before use
        if (!ValidateReferences())
        {
            LogWarning("References invalid - attempting reassign...");
            ReassignPanels();
            return;
        }

        if (Level != null && Level.activeSelf) return;

        SetActiveSafe(Level, true);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, false);

        Log("Switched to Level panel");
    }

    public void ShowQuest()
    {
        if (!ValidateReferences())
        {
            LogWarning("References invalid - attempting reassign...");
            ReassignPanels();
            return;
        }

        if (Quest != null && Quest.activeSelf) return;

        SetActiveSafe(Level, false);
        SetActiveSafe(Quest, true);
        SetActiveSafe(Shop, false);

        Log("Switched to Quest panel");
    }

    public void ShowShop()
    {
        if (!ValidateReferences())
        {
            LogWarning("References invalid - attempting reassign...");
            ReassignPanels();
            return;
        }

        if (Shop != null && Shop.activeSelf) return;

        SetActiveSafe(Level, false);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, true);

        Log("Switched to Shop panel");
    }

    /// <summary>
    /// ✅ Validate all panel references
    /// </summary>
    bool ValidateReferences()
    {
        return Level != null && Quest != null && Shop != null;
    }

    // ========================================
    // ECONOMY BUTTONS (Coin/Shard/Energy)
    // ========================================

    public void OnCoinButtonClicked()
    {
        Log("Coin button clicked - Opening Shop");
        ShowShop();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    public void OnShardButtonClicked()
    {
        Log("Shard button clicked - Opening Shop");
        ShowShop();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    public void OnEnergyButtonClicked()
    {
        Log("Energy button clicked - Opening Shop");
        ShowShop();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    // ========================================
    // EXIT BUTTON (WebGL)
    // ========================================

    public void OnExitButtonClicked()
    {
        Log("Exit button clicked");

        if (showExitConfirmation)
        {
            Log("Exit confirmation enabled - implement popup if needed");
        }

        ExitGame();
    }

    void ExitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Log($"Redirecting to: {exitURL}");
        
        try
        {
            RedirectToURL(exitURL);
        }
        catch (System.Exception e)
        {
            LogError($"Failed to redirect: {e.Message}");
            Application.OpenURL(exitURL);
        }
#elif UNITY_EDITOR
        Log("Stopping Play Mode (Editor)");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Log("Quitting application");
        Application.Quit();
#endif
    }

    public void ExitGameDirect()
    {
        Log("Direct exit (no confirmation)");
        ExitGame();
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[ButtonManager] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[ButtonManager] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[ButtonManager] ❌ {message}");
    }

    // ========================================
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("🔄 Force Reassign Panels")]
    void Context_ForceReassign()
    {
        ReassignPanels();
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== BUTTONMANAGER STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Is Persistent: {isPersistent}");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"\nPanel References:");
        Debug.Log($"  Level: {(Level != null ? Level.name : "NULL")}");
        Debug.Log($"  Quest: {(Quest != null ? Quest.name : "NULL")}");
        Debug.Log($"  Shop: {(Shop != null ? Shop.name : "NULL")}");
        Debug.Log($"\nPanel Names:");
        Debug.Log($"  Level: {levelPanelName}");
        Debug.Log($"  Quest: {questPanelName}");
        Debug.Log($"  Shop: {shopPanelName}");
        Debug.Log("===========================");
    }

    [ContextMenu("🧪 Test: Find Panels")]
    void Context_TestFindPanels()
    {
        Debug.Log("=== TESTING PANEL SEARCH ===");
        
        GameObject level = FindPanelByName(levelPanelName);
        Debug.Log($"Level ({levelPanelName}): {(level != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        
        GameObject quest = FindPanelByName(questPanelName);
        Debug.Log($"Quest ({questPanelName}): {(quest != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        
        GameObject shop = FindPanelByName(shopPanelName);
        Debug.Log($"Shop ({shopPanelName}): {(shop != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        
        Debug.Log("============================");
    }
}