using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manager untuk tracking bintang yang dikumpulkan di gameplay.
/// Letakkan di: Assets/Script/Movement/GameplayStarManager.cs
/// </summary>
public class GameplayStarManager : MonoBehaviour
{
    public static GameplayStarManager Instance { get; private set; }

    [Header("Star Settings")]
    public int totalStarsInLevel = 3;

    [Header("Events")]
    public UnityEvent<int> OnStarCollected;
    public UnityEvent<int> OnLevelComplete;

    private int collectedStars = 0;
    private bool levelCompleted = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void CollectStar(int amount = 1)
    {
        if (levelCompleted) return;

        collectedStars += amount;
        collectedStars = Mathf.Clamp(collectedStars, 0, totalStarsInLevel);

        Debug.Log($"[GameplayStarManager] Star collected! Total: {collectedStars}/{totalStarsInLevel}");

        OnStarCollected?.Invoke(collectedStars);
    }

    public void CompleteLevelWithStars()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        string levelId = PlayerPrefs.GetString("SelectedLevelId", "");
        int levelNum = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        if (!string.IsNullOrEmpty(levelId) && LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.SaveBestStars(levelId, collectedStars);
            LevelProgressManager.Instance.UnlockNextLevel(levelNum);
            Debug.Log($"[GameplayStarManager] Saved {collectedStars} stars for {levelId}");
        }

        OnLevelComplete?.Invoke(collectedStars);
    }

    public int GetCollectedStars() => collectedStars;
    public int GetTotalStars() => totalStarsInLevel;
}