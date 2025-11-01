using UnityEngine;

/// <summary>
/// FIXED: Settings controller - hanya open via button, bukan Mouse0
/// Attach ke GameObject di scene, assign settingsMenu GameObject
/// </summary>
public class Settings : MonoBehaviour
{
    [Header("Settings Menu")]
    public GameObject settingsMenu;

    void Start()
    {
        // Pastikan settings menu hidden di start
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }
    }

    /// <summary>
    /// Open settings menu (call dari Button.onClick)
    /// </summary>
    public void OpenSettings()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(true);
            Debug.Log("[Settings] Settings menu opened");
        }
    }

    /// <summary>
    /// Close settings menu (call dari Button.onClick - button close/cancel)
    /// </summary>
    public void CloseSettings()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
            Debug.Log("[Settings] Settings menu closed");
        }
    }

    /// <summary>
    /// Toggle settings menu (optional - untuk button gear toggle)
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsMenu != null)
        {
            bool isActive = settingsMenu.activeSelf;
            settingsMenu.SetActive(!isActive);
            Debug.Log($"[Settings] Settings menu toggled: {!isActive}");
        }
    }
}