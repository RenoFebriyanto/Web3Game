using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects scene changes and ensures PlayerEconomy persists
/// Attach to EconomyManager prefab or create separate GameObject
/// </summary>
public class SceneChangeHandler : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneChangeHandler] Scene loaded: {scene.name}");

        // ✅ Verify PlayerEconomy still exists
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[SceneChangeHandler] ❌ PlayerEconomy is NULL after scene load!");

            // Try to recreate
            SceneBootstrapper.EnsureEconomyExists();
        }
        else
        {
            Debug.Log($"[SceneChangeHandler] ✓ PlayerEconomy exists: {PlayerEconomy.Instance.gameObject.name}");
        }

        // ✅ Force refresh UI di MainMenu
        if (scene.name == "MainMenu")
        {
            Debug.Log("[SceneChangeHandler] MainMenu detected, refreshing UI...");

            // Wait 1 frame untuk ensure semua Awake() selesai
            StartCoroutine(RefreshMainMenuUI());
        }
    }

    System.Collections.IEnumerator RefreshMainMenuUI()
    {
        yield return null; // Wait 1 frame

        var uiControllers = FindObjectsByType<EconomyUIController>(FindObjectsSortMode.None);

        foreach (var controller in uiControllers)
        {
            if (controller != null)
            {
                controller.gameObject.SetActive(false);
                controller.gameObject.SetActive(true);
            }
        }

        Debug.Log($"[SceneChangeHandler] ✓ Refreshed {uiControllers.Length} UI controllers");
    }
}
