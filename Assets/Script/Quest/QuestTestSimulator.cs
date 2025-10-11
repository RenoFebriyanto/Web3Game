using UnityEngine;

public class QuestTestSimulator : MonoBehaviour
{
    [Tooltip("Quest id yang akan ditambah progress")]
    public string questId = "daily_login_3";

    [Tooltip("Berapa kali menambah progress (mis. 3 untuk login 3 hari)")]
    public int times = 3;

    [ContextMenu("SimulateLogins")]
    public void SimulateLogins()
    {   
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestTestSimulator] QuestManager.Instance is null");
            return;
        }

        for (int i = 0; i < times; i++)
        {
            QuestManager.Instance.AddProgress(questId, 1);
            Debug.Log($"[QuestTestSimulator] Added progress {i + 1}/{times} to {questId}");
        }
    }

    [ContextMenu("SimulateOneLogin")]
    public void SimulateOne()
    {
        if (QuestManager.Instance == null) { Debug.LogWarning("QuestManager null"); return; }
        QuestManager.Instance.AddProgress(questId, 1);
        Debug.Log($"[QuestTestSimulator] Added 1 to {questId}");
    }
}
