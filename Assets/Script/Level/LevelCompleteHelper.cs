using UnityEngine;

public class LevelCompleteHelper : MonoBehaviour
{
    public void OnLevelComplete(int starsEarned)
    {
        // read selected level
        string id = PlayerPrefs.GetString("SelectedLevelId", "");
        int num = PlayerPrefs.GetInt("SelectedLevelNumber", -1);

        if (!string.IsNullOrEmpty(id))
        {
            LevelProgressManager.Instance?.SaveBestStars(id, starsEarned);
            LevelProgressManager.Instance?.UnlockNextLevel(num);
        }
    }
}
