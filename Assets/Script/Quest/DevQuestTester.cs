using UnityEngine;

public class DevQuestTester : MonoBehaviour
{
    public string testQuestId;
    public int addAmount = 1;

    public void AddProgressNow()
    {
        if (!string.IsNullOrEmpty(testQuestId))
            QuestManager.Instance?.AddProgress(testQuestId, addAmount);
    }

    public void ResetDailyNow()
    {
        QuestManager.Instance?.ResetDaily();
    }

    public void ResetWeeklyNow()
    {
        QuestManager.Instance?.ResetWeekly();
    }
}
