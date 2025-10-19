using UnityEngine;

/// <summary>
/// Track play time in minutes and add progress to one or more questIds.
/// Attach to a persistent GameObject (GameBootstrap / GameManager).
/// </summary>
public class PlayTimeTracker : MonoBehaviour
{
    [Tooltip("Quest IDs that should be progressed per minute. Example: daily_play_5min")]
    public string[] questIdsToAddPerMinute;

    float secondsAccum = 0f;
    public bool trackWhenActive = true; // set false to pause tracking

    void Update()
    {
        if (!trackWhenActive) return;
        if (questIdsToAddPerMinute == null || questIdsToAddPerMinute.Length == 0) return;

        secondsAccum += Time.deltaTime;
        if (secondsAccum >= 60f)
        {
            int minutes = Mathf.FloorToInt(secondsAccum / 60f);
            secondsAccum -= minutes * 60f;

            if (QuestManager.Instance != null)
            {
                foreach (var q in questIdsToAddPerMinute)
                {
                    if (string.IsNullOrEmpty(q)) continue;
                    QuestManager.Instance.AddProgress(q, minutes);
                }
            }
            else
            {
                Debug.LogWarning("[PlayTimeTracker] QuestManager.Instance is null. Progress not applied.");
            }

            Debug.Log($"[PlayTimeTracker] Added {minutes} minute(s) to {questIdsToAddPerMinute.Length} quest(s)");
        }
    }

    // optional helper to reset timer (for tests)
    public void ResetTimer() { secondsAccum = 0f; }
}
