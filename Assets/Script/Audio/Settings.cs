using UnityEngine;

public class Settings : MonoBehaviour
{
    public GameObject settingsMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false); // Ensure the settings menu is hidden at the start
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (settingsMenu != null)
            {
                bool isActive = settingsMenu.activeSelf;
                settingsMenu.SetActive(!isActive); // Toggle the settings menu visibility
            }
        }
    }

    public void OpenSettings()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(true);
        }
    }
}
