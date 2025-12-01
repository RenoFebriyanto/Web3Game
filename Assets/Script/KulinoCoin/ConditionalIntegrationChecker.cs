using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ Conditional Integration Checker
/// Only enable IntegrationChecker di Gameplay scene (yang ada GameManager)
/// Disable di MainMenu scene
/// 
/// SETUP:
/// 1. Attach script ini ke GameObject yang sama dengan IntegrationChecker
/// 2. Script ini akan auto-enable/disable IntegrationChecker based on scene
/// </summary>
[DefaultExecutionOrder(-100)]
public class ConditionalIntegrationChecker : MonoBehaviour
{
    [Header("⚙️ Settings")]
    [Tooltip("Scene names yang MEMERLUKAN IntegrationChecker (e.g., Gameplay)")]
    public string[] requiredScenes = new string[] 
    { 
        "Gameplay", 
        "GameplayScene",
        "Level" // Add scene names yang butuh GameManager
    };

    [Tooltip("Auto-disable di scene lain?")]
    public bool autoDisableInOtherScenes = true;

    private IntegrationChecker integrationChecker;

    void Awake()
    {
        integrationChecker = GetComponent<IntegrationChecker>();
        
        if (integrationChecker == null)
        {
            Debug.LogWarning("[ConditionalCheck] IntegrationChecker not found on this GameObject");
            enabled = false;
            return;
        }

        // Check current scene
        CheckScene(SceneManager.GetActiveScene());

        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene(scene);
    }

    void CheckScene(Scene scene)
    {
        string sceneName = scene.name;
        bool shouldEnable = false;

        // Check if current scene requires IntegrationChecker
        foreach (string requiredScene in requiredScenes)
        {
            if (sceneName.Contains(requiredScene) || sceneName.Equals(requiredScene, System.StringComparison.OrdinalIgnoreCase))
            {
                shouldEnable = true;
                break;
            }
        }

        if (integrationChecker != null)
        {
            if (shouldEnable)
            {
                integrationChecker.enabled = true;
                Debug.Log($"[ConditionalCheck] ✅ IntegrationChecker ENABLED in scene: {sceneName}");
            }
            else if (autoDisableInOtherScenes)
            {
                integrationChecker.enabled = false;
                Debug.Log($"[ConditionalCheck] ⏸️ IntegrationChecker DISABLED in scene: {sceneName} (GameManager not needed here)");
            }
        }
    }

    [ContextMenu("🔍 Check Current Scene")]
    void Context_CheckCurrentScene()
    {
        CheckScene(SceneManager.GetActiveScene());
    }
}