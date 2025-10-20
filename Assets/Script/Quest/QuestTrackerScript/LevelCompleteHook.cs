using UnityEngine;

public class LevelCompleteHook : MonoBehaviour
{
    [Tooltip("Quest Id to progress when a level is completed")]
    public string questId;

    // Call this method from your level-complete code
    public void OnLevelComplete()
    {
        if (!string.IsNullOrEmpty(questId))
            QuestManager.Instance?.AddProgress(questId, 1);
    }

    // Example: if you want to call automatically in editor, just call OnLevelComplete from your level manager
}
