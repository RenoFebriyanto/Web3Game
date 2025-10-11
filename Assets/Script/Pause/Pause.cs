using UnityEngine;

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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    

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
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    public void HomeGame()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
