// Assets/Script/Level/LevelProgressManager.cs
using System.Collections.Generic;
using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    const string PREF_STARS_PREFIX = "Kulino_LevelStars_";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            Debug.Log($"[LevelProgressManager] Saved better stars for {levelId} = {stars}");
        }
    }

    public int GetBestStars(string levelId)
    {
        return PlayerPrefs.GetInt(PREF_STARS_PREFIX + levelId, 0);
    }

    // called after completion to unlock next level (connects with LevelManager in scene)
    public void OnLevelCompletedUnlockNext(string levelId)
    {
        // parse number from id if using "level_X"
        int num = -1;
        if (levelId.StartsWith("level_"))
        {
            int.TryParse(levelId.Substring("level_".Length), out num);
        }

        var lm = FindObjectOfType<LevelManager>();
        if (lm != null && num > 0)
        {
            string nextId = $"level_{num + 1}";
            // jika LevelManager punya UnlockNextLevel(string)
            lm.UnlockNextLevel(nextId);
        }

    }
}
