using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }
    const string PREF_HIGHEST_UNLOCKED = "Kulino_HighestUnlockedLevel_v1";
    const string PREF_STARS_PREFIX = "Kulino_LevelStars_";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetHighestUnlocked()
    {
        return PlayerPrefs.GetInt(PREF_HIGHEST_UNLOCKED, 1);
    }

    public bool IsUnlocked(int levelNumber)
    {
        return levelNumber <= GetHighestUnlocked();
    }

    public void UnlockNextLevel(int currentLevelNumber)
    {
        int next = currentLevelNumber + 1;
        int curHigh = GetHighestUnlocked();
        if (next > curHigh)
        {
            PlayerPrefs.SetInt(PREF_HIGHEST_UNLOCKED, next);
            PlayerPrefs.Save();
            Debug.Log($"[LevelProgressManager] Unlocked level {next}");
        }
    }

    public void SaveBestStars(string levelId, int stars)
    {
        if (stars < 0) stars = 0;
        if (stars > 3) stars = 3;
        int prev = GetBestStars(levelId);
        if (stars > prev)
        {
            PlayerPrefs.SetInt(PREF_STARS_PREFIX + levelId, stars);
            PlayerPrefs.Save();
        }
    }

    public int GetBestStars(string levelId)
    {
        return PlayerPrefs.GetInt(PREF_STARS_PREFIX + levelId, 0);
    }
}
