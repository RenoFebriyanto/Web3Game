using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SOLUSI BARU: Manager yang benar-benar persistent
/// TIDAK ADA di scene hierarchy, dibuat via RuntimeInitializeOnLoadMethod
/// </summary>
public class PersistentGameManager : MonoBehaviour
{
    public static PersistentGameManager Instance { get; private set; }

    // References ke manager components
    public ShopManager ShopManager { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreatePersistentManager()
    {
        // Buat GameObject yang TIDAK ADA di scene
        GameObject go = new GameObject("[PersistentGameManager]");
        Instance = go.AddComponent<PersistentGameManager>();
        DontDestroyOnLoad(go);

        Debug.Log("[PersistentGameManager] ✓ Created via RuntimeInitialize (not in scene)");
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PersistentGameManager] Duplicate found, destroying");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe scene events
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[PersistentGameManager] ✓ Initialized");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PersistentGameManager] Scene loaded: {scene.name}");

        if (scene.name == "MainMenu")
        {
            InitializeMainMenuManagers();
        }
    }

    void InitializeMainMenuManagers()
    {
        // Find ShopManager di MainMenu scene
        ShopManager = FindFirstObjectByType<ShopManager>();

        if (ShopManager != null)
        {
            Debug.Log("[PersistentGameManager] ✓ Found ShopManager in MainMenu");

            // Populate shop
            Invoke(nameof(DelayedShopPopulate), 0.1f);
        }
        else
        {
            Debug.LogError("[PersistentGameManager] ❌ ShopManager not found in MainMenu!");
        }

        // Find QuestManager
        var questManager = FindFirstObjectByType<QuestManager>();
        if (questManager != null)
        {
            Debug.Log("[PersistentGameManager] ✓ Found QuestManager in MainMenu");
        }
    }

    void DelayedShopPopulate()
    {
        if (ShopManager != null)
        {
            ShopManager.PopulateShop();
            Debug.Log("[PersistentGameManager] ✓ Shop populated");
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[PersistentGameManager] Destroyed");
    }
}