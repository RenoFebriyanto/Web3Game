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
            Debug.LogError("Pause root object is not assigned in the inspector.");
        }
        else
        {
            pauseRootObject.SetActive(false);
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        pauseRootObject.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        pauseRootObject.SetActive(false);
    }

    public void ReplayGame()
    {
        Time.timeScale = 1;

        // ✅ PENTING: Pastikan DontDestroyOnLoad objects tetap ada
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void HomeGame()
    {
        Time.timeScale = 1;

        // ✅ Load MainMenu tanpa destroy persistent objects
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}