using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ✅ FIXED: GameplayStarManager - Remove direct call to KulinoCoinRewardSystem
/// Manager untuk tracking bintang yang dikumpulkan di gameplay.
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

    /// <summary>
    /// ✅ UPDATED: CompleteLevelWithStars - KulinoCoinRewardSystem sudah auto-subscribe
    /// </summary>
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

        // ✅ Trigger event (LevelGameSession.OnLevelCompleted akan trigger)
        OnLevelComplete?.Invoke(collectedStars);

        // ❌ REMOVED: KulinoCoinRewardSystem.Instance.OnLevelComplete()
        // ✅ Sudah auto-trigger via LevelGameSession.OnLevelCompleted event subscription
        // KulinoCoinRewardSystem subscribe di Start(), jadi tidak perlu dipanggil manual

        Debug.Log($"[GameplayStarManager] ✓ Level complete with {collectedStars} stars");
    }

    public int GetCollectedStars() => collectedStars;
    public int GetTotalStars() => totalStarsInLevel;
}