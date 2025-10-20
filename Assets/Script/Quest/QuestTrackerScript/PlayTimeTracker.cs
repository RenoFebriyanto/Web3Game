using System.Collections.Generic;
using UnityEngine;

public class PlayTimeTracker : MonoBehaviour
{
    [Tooltip("Quest Ids to progress per minute (e.g. daily_play_5min etc)")]
    public List<string> questIdsToProgressPerMinute = new List<string>();

    [Tooltip("If false, stops tracking when app not focused")]
    public bool trackWhenInactive = false;

    float elapsed = 0f;

    void Update()
    {
        if (!trackWhenInactive && !Application.isFocused) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= 60f)
        {
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            elapsed -= minutes * 60f;
            foreach (var id in questIdsToProgressPerMinute)
                QuestManager.Instance?.AddProgress(id, minutes); // add minutes
        }
    }
}
