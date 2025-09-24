// Assets/Script/Level/LevelLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLoader
{
    // static holder for selected level
    public static string CurrentLevelId { get; private set; }
    public static int CurrentLevelNumber { get; private set; }

    public static void LoadLevel(string levelId, int number)
    {
        CurrentLevelId = levelId;
        CurrentLevelNumber = number;
        // optional: you can set a GameSession static object too
        Debug.Log($"[LevelLoader] Loading scene Gameplay for {levelId}");
        SceneManager.LoadScene("Gameplay"); // pastikan scene with name "Gameplay" ada di Build Settings
    }
}
