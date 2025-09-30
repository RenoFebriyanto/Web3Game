using UnityEngine;

public class LevelGameSession : MonoBehaviour
{
    [Header("Optional reference to level database (for lookup)")]
    public LevelDatabase database;

    [HideInInspector]
    public LevelConfig currentLevel;

    void Awake()
    {
        string id = PlayerPrefs.GetString("SelectedLevelId", "");
        int num = PlayerPrefs.GetInt("SelectedLevelNumber", -1);

        if (!string.IsNullOrEmpty(id) && database != null)
        {
            currentLevel = database.GetById(id);
        }
        else if (num > 0 && database != null)
        {
            currentLevel = database.GetByNumber(num);
        }

        if (currentLevel == null)
        {
            Debug.LogWarning("[LevelGameSession] No selected level found - fallback to level 1 if available.");
            if (database != null) currentLevel = database.GetByNumber(1);
        }

        Debug.Log("[LevelGameSession] Loaded level: " + (currentLevel != null ? currentLevel.id : "NULL"));
    }
}
