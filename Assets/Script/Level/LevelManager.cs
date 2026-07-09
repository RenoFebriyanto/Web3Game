using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menyimpan informasi level aktif secara global dan memudahkan perpindahan scene.
/// Script ini memakai pola Singleton dan tetap hidup antar scene dengan DontDestroyOnLoad.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Current Level Info")]
    [SerializeField] private int currentLevelIndex;
    [SerializeField] private string currentLevelName = "";

    public int CurrentLevelIndex => currentLevelIndex;
    public string CurrentLevelName => currentLevelName;

    /// <summary>
    /// Dipanggil saat objek pertama kali dibuat. Menjamin hanya ada satu instance yang hidup.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UpdateCurrentLevelInfo();
    }

    /// <summary>
    /// Mendaftarkan listener saat objek aktif agar setiap scene yang dimuat dapat diperbarui.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    /// <summary>
    /// Menghapus listener saat objek non-aktif untuk mencegah memory leak.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// Dipanggil setiap kali scene selesai dimuat. Memperbarui indeks dan nama level aktif.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentLevelIndex = scene.buildIndex;
        currentLevelName = scene.name;

        Debug.Log($"[LevelManager] Scene updated: {currentLevelName} (Index: {currentLevelIndex})");
    }

    /// <summary>
    /// Mengambil informasi scene aktif saat ini dari SceneManager.
    /// </summary>
    private void UpdateCurrentLevelInfo()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        currentLevelIndex = currentScene.buildIndex;
        currentLevelName = currentScene.name;
    }

    /// <summary>
    /// Memuat scene berikutnya berdasarkan build index saat ini.
    /// </summary>
    public void LoadNextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;

        if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextLevelIndex);
        }
        else
        {
            Debug.LogWarning("[LevelManager] Tidak ada scene berikutnya yang tersedia.");
        }
    }
}
