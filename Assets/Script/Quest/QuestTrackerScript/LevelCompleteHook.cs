using UnityEngine;

/// <summary>
/// Call OnLevelComplete() from your level-end logic so quest progresses.
/// </summary>
public class LevelCompleteHook : MonoBehaviour
{
    [Tooltip("Quest id to increment when a level is completed")]
    public string questId = "daily_complete_5levels";

    public void OnLevelComplete()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AddProgress(questId, 1);
            Debug.Log($"[LevelCompleteHook] +1 to {questId}");
        }
        else Debug.LogWarning("[LevelCompleteHook] QuestManager.Instance is null");
    }
}
