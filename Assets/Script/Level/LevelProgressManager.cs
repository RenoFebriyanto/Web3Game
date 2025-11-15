using UnityEngine;

/// <summary>
/// ✅ UPDATED: Track played levels (untuk energy system)
/// - Level yang sudah pernah dimainkan = no energy cost
/// - Level baru = cost 10 energy
/// </summary>
public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }
    
    const string PREF_HIGHEST_UNLOCKED = "Kulino_HighestUnlockedLevel_v1";
    const string PREF_STARS_PREFIX = "Kulino_LevelStars_";
    
    // ✅ NEW: Track played levels (CSV format: "1,2,3,5,7")
    const string PREF_PLAYED_LEVELS = "Kulino_PlayedLevels_v1";

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========================================
    // UNLOCK SYSTEM (existing)
    // ========================================
    
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

    // ========================================
    // STARS SYSTEM (existing)
    // ========================================
    
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

    // ========================================
    // ✅ NEW: PLAYED LEVELS TRACKING (untuk energy system)
    // ========================================
    
    /// <summary>
    /// Check apakah level sudah pernah dimainkan (energy sudah dibayar)
    /// </summary>
    public bool HasPlayedLevel(int levelNumber)
    {
        string playedLevels = PlayerPrefs.GetString(PREF_PLAYED_LEVELS, "");
        
        if (string.IsNullOrEmpty(playedLevels)) 
            return false;

        string[] levels = playedLevels.Split(',');
        foreach (string lvl in levels)
        {
            if (int.TryParse(lvl.Trim(), out int num) && num == levelNumber)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Mark level as played (dipanggil saat level dimulai & energy dipotong)
    /// </summary>
    public void MarkLevelAsPlayed(int levelNumber)
    {
        // Check dulu apakah sudah di-mark
        if (HasPlayedLevel(levelNumber))
        {
            Debug.Log($"[LevelProgressManager] Level {levelNumber} already marked as played");
            return;
        }

        string playedLevels = PlayerPrefs.GetString(PREF_PLAYED_LEVELS, "");

        if (string.IsNullOrEmpty(playedLevels))
        {
            playedLevels = levelNumber.ToString();
        }
        else
        {
            playedLevels += "," + levelNumber.ToString();
        }

        PlayerPrefs.SetString(PREF_PLAYED_LEVELS, playedLevels);
        PlayerPrefs.Save();

        Debug.Log($"[LevelProgressManager] ✓ Marked level {levelNumber} as played (no energy cost next time)");
    }

    /// <summary>
    /// Check apakah level perlu bayar energy
    /// </summary>
    public bool NeedsEnergyCost(int levelNumber)
    {
        // Level 1 selalu gratis (tutorial/first level)
        if (levelNumber == 1)
            return false;

        // Level yang sudah pernah dimainkan = gratis
        if (HasPlayedLevel(levelNumber))
            return false;

        // Level baru yang belum pernah dimainkan = bayar energy
        return true;
    }

    /// <summary>
    /// Get energy cost untuk level
    /// </summary>
    public int GetEnergyCost(int levelNumber)
    {
        return NeedsEnergyCost(levelNumber) ? 10 : 0;
    }

    // ========================================
    // DEBUG / TESTING
    // ========================================
    
    [ContextMenu("Debug: Print Played Levels")]
    void Debug_PrintPlayedLevels()
    {
        string playedLevels = PlayerPrefs.GetString(PREF_PLAYED_LEVELS, "");
        Debug.Log($"=== PLAYED LEVELS ===");
        Debug.Log($"Data: {(string.IsNullOrEmpty(playedLevels) ? "NONE" : playedLevels)}");
        Debug.Log("=====================");
    }

    [ContextMenu("Debug: Clear Played Levels")]
    void Debug_ClearPlayedLevels()
    {
        PlayerPrefs.DeleteKey(PREF_PLAYED_LEVELS);
        PlayerPrefs.Save();
        Debug.Log("✓ Played levels cleared");
    }

    [ContextMenu("Debug: Test Level 5 Energy Cost")]
    void Debug_TestLevel5()
    {
        int cost = GetEnergyCost(5);
        bool needs = NeedsEnergyCost(5);
        bool hasPlayed = HasPlayedLevel(5);
        
        Debug.Log($"=== LEVEL 5 ENERGY CHECK ===");
        Debug.Log($"Has Played: {hasPlayed}");
        Debug.Log($"Needs Energy: {needs}");
        Debug.Log($"Cost: {cost} energy");
        Debug.Log("===========================");
    }
}