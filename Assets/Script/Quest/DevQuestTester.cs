// DevQuestTester.cs
// Put this in Assets/Script/Quest/DevQuestTester.cs
using UnityEngine;

/// <summary>
/// Small helper component for testing quest flows in editor/runtime:
/// - Add progress
/// - Claim quest
/// - Reset daily
/// Use ContextMenu or call from inspector play mode.
/// </summary>
public class DevQuestTester : MonoBehaviour
{
    public string questId;
    public int addAmount = 1;

    public void Add()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.AddProgress(questId, addAmount);
        else Debug.LogWarning("[DevQuestTester] QuestManager.Instance is null.");
    }

    public void Claim()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.ClaimReward(questId);
        else Debug.LogWarning("[DevQuestTester] QuestManager.Instance is null.");
    }

    [ContextMenu("ResetDaily")]
    public void ResetDaily()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.ResetDaily();
        else Debug.LogWarning("[DevQuestTester] QuestManager.Instance is null.");
    }
}
