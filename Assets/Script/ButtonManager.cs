using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // ✅ TAMBAHKAN INI
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED v3.0: ButtonManager dengan Button Listener Reassignment
/// - Fixed: Button onClick listeners hilang setelah scene transition
/// - Auto-reassign button listeners setiap kali panel di-reassign
/// - Persistent across scenes
/// </summary>
public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance { get; private set; }

    [Header("Panels (assign in Inspector)")]
    public GameObject Level;
    public GameObject Quest;
    public GameObject Shop;

    [Header("⚡ Button References (assign di Inspector)")]
    [Tooltip("Assign button LEVEL dari hierarchy")]
    public UnityEngine.UI.Button levelButton;
    
    [Tooltip("Assign button QUEST dari hierarchy")]
    public UnityEngine.UI.Button questButton;
    
    [Tooltip("Assign button SHOP dari hierarchy")]
    public UnityEngine.UI.Button shopButton;

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
    
    [Header("🔘 Button Names untuk Auto-Find")]
    [Tooltip("Nama button LEVEL di hierarchy")]
    public string levelButtonName = "Level";
    
    [Tooltip("Nama button QUEST di hierarchy")]
    public string questButtonName = "Quest";
    
    [Tooltip("Nama button SHOP di hierarchy")]
    public string shopButtonName = "Shop";

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private bool isPersistent = false;

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void RedirectToURL(string url);
#endif

    void Awake()
    {
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

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        isPersistent = true;
        gameObject.name = "[ButtonManager - PERSISTENT]";

        Log("✅ ButtonManager initialized as PERSISTENT");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"=== Scene Loaded: {scene.name} ===");
        StartCoroutine(ReassignEverythingDelayed());
    }

    System.Collections.IEnumerator ReassignEverythingDelayed()
    {
        yield return null;
        yield return null;

        ReassignPanels();
        ReassignButtons(); // ✅ CRITICAL FIX
        RebindButtonListeners(); // ✅ CRITICAL FIX
    }

    void ReassignPanels()
{
    Log("🔄 Re-assigning panel references...");

    bool levelValid = Level != null;
    bool questValid = Quest != null;
    bool shopValid = Shop != null;

    Log($"Current state: Level={levelValid}, Quest={questValid}, Shop={shopValid}");

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

    // ✅ FIX: Set default state TANPA mengaktifkan semua panel dulu
    if (Level != null && Quest != null && Shop != null)
    {
        // ✅ Langsung set state yang benar
        Level.SetActive(true);
        Quest.SetActive(false);
        Shop.SetActive(false);
        
        Log("✅ Default state set: Level=ON, Quest=OFF, Shop=OFF");
    }
    else
    {
        LogWarning("⚠️ Some panels still missing after reassign!");
    }
}

    /// <summary>
    /// ✅ NEW: Re-assign button references
    /// </summary>
    void ReassignButtons()
    {
        Log("🔘 Re-assigning button references...");

        bool levelBtnValid = levelButton != null;
        bool questBtnValid = questButton != null;
        bool shopBtnValid = shopButton != null;

        Log($"Current button state: Level={levelBtnValid}, Quest={questBtnValid}, Shop={shopBtnValid}");

        if (!levelBtnValid)
        {
            levelButton = FindButtonByName(levelButtonName);
            Log($"Level button: {(levelButton != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        }

        if (!questBtnValid)
        {
            questButton = FindButtonByName(questButtonName);
            Log($"Quest button: {(questButton != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        }

        if (!shopBtnValid)
        {
            shopButton = FindButtonByName(shopButtonName);
            Log($"Shop button: {(shopButton != null ? "✓ FOUND" : "❌ NOT FOUND")}");
        }
    }

    /// <summary>
    /// ✅ CRITICAL FIX: Re-bind button onClick listeners
    /// </summary>
    void RebindButtonListeners()
    {
        Log("🔗 Re-binding button onClick listeners...");

        // ✅ LEVEL Button
        if (levelButton != null)
        {
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(ShowLevel);
            Log("✓ Level button listener bound");
        }
        else
        {
            LogWarning("❌ Level button is NULL!");
        }

        // ✅ QUEST Button
        if (questButton != null)
        {
            questButton.onClick.RemoveAllListeners();
            questButton.onClick.AddListener(ShowQuest);
            Log("✓ Quest button listener bound");
        }
        else
        {
            LogWarning("❌ Quest button is NULL!");
        }

        // ✅ SHOP Button
        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(ShowShop);
            Log("✓ Shop button listener bound");
        }
        else
        {
            LogWarning("❌ Shop button is NULL!");
        }

        Log("✅ All button listeners rebound");
    }

    GameObject FindPanelByName(string panelName)
    {
        if (string.IsNullOrEmpty(panelName))
        {
            return null;
        }

        GameObject found = GameObject.Find(panelName);
        if (found != null)
        {
            Log($"Found '{panelName}' via GameObject.Find");
            return found;
        }

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
    /// ✅ NEW: Find button by name
    /// </summary>
    UnityEngine.UI.Button FindButtonByName(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName))
        {
            return null;
        }

        // Method 1: Direct find
        GameObject found = GameObject.Find(buttonName);
        if (found != null)
        {
            UnityEngine.UI.Button btn = found.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                Log($"Found button '{buttonName}' via GameObject.Find");
                return btn;
            }
        }

        // Method 2: Search in Canvas
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            Transform result = SearchInChildren(canvas.transform, buttonName);
            if (result != null)
            {
                UnityEngine.UI.Button btn = result.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    Log($"Found button '{buttonName}' in Canvas: {canvas.name}");
                    return btn;
                }
            }
        }

        LogWarning($"❌ Button '{buttonName}' not found!");
        return null;
    }

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
        ReassignPanels();
        ReassignButtons();
        RebindButtonListeners();
    }

    void Update()
    {
        bool isMobile = Application.isMobilePlatform;
        if (isMobile || !enableKeyboardShortcuts)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowLevel();
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowQuest();
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowShop();

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

    public void ShowLevel()
    {
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

    if (Shop != null && Shop.activeSelf) 
    {
        Log("Shop already active");
        return;
    }

    Log("=== Opening Shop Panel ===");

    SetActiveSafe(Level, false);
    SetActiveSafe(Quest, false);
    SetActiveSafe(Shop, true);

    // ✅ TRIGGER initialization SETELAH panel aktif
    if (ShopManager.Instance != null)
    {
        // Trigger initialization jika belum
        if (!ShopManager.Instance.isInitialized)
        {
            StartCoroutine(ShopManager.Instance.EnsureInitialization());
        }
        else
        {
            ShopManager.Instance.ForceRebuildAllLayouts();
        }
    }

    Log("Shop panel activated");
}

        // ✅ TAMBAH METHOD BARU INI
        IEnumerator WaitForShopReady()
        {
            yield return null;
            yield return null;

            Log("Shop panel ready, triggering layout refresh");

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.ForceRebuildAllLayouts();
            }
        }

    bool ValidateReferences()
    {
        return Level != null && Quest != null && Shop != null;
    }

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

    [ContextMenu("🔄 Force Reassign All")]
    void Context_ForceReassignAll()
    {
        ReassignPanels();
        ReassignButtons();
        RebindButtonListeners();
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
        Debug.Log($"\nButton References:");
        Debug.Log($"  Level Button: {(levelButton != null ? levelButton.name : "NULL")}");
        Debug.Log($"  Quest Button: {(questButton != null ? questButton.name : "NULL")}");
        Debug.Log($"  Shop Button: {(shopButton != null ? shopButton.name : "NULL")}");
        Debug.Log($"\nPanel Names:");
        Debug.Log($"  Level: {levelPanelName}");
        Debug.Log($"  Quest: {questPanelName}");
        Debug.Log($"  Shop: {shopPanelName}");
        Debug.Log($"\nButton Names:");
        Debug.Log($"  Level: {levelButtonName}");
        Debug.Log($"  Quest: {questButtonName}");
        Debug.Log($"  Shop: {shopButtonName}");
        Debug.Log("===========================");
    }

    [ContextMenu("🔘 Test: Rebind Listeners")]
    void Context_TestRebind()
    {
        RebindButtonListeners();
        Debug.Log("✓ Button listeners rebound");
    }
}