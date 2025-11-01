using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    [Header("Pause Menu")]
    public GameObject pauseRootObject;

    private void Awake()
    {
        if (pauseRootObject == null)
        {
            Debug.LogError("[Pause] pauseRootObject is not assigned!");
        }
        else
        {
            pauseRootObject.SetActive(false);
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0;

        if (pauseRootObject != null)
        {
            pauseRootObject.SetActive(true);
        }

        Debug.Log("[Pause] Game paused");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;

        if (pauseRootObject != null)
        {
            pauseRootObject.SetActive(false);
        }

        Debug.Log("[Pause] Game resumed");
    }

    public void ReplayGame()
    {
        Time.timeScale = 1;

        Debug.Log("[Pause] Reloading scene...");

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void HomeGame()
    {
        Time.timeScale = 1;

        Debug.Log("[Pause] Loading MainMenu...");

        // Load MainMenu
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}