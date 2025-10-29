using UnityEngine;
using TMPro;

/// <summary>
/// Display countdown timer untuk booster yang aktif
/// Optional: attach ke text di samping booster button untuk show timer
/// Letakkan di: Assets/Script/Booster/BoosterTimerUI.cs
/// </summary>
public class BoosterTimerUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ID booster untuk display timer")]
    public string boosterId = "coin2x";

    [Header("UI")]
    public TMP_Text timerText;
    public GameObject timerRoot; // Root object untuk show/hide timer

    void Update()
    {
        if (BoosterManager.Instance == null) return;

        float remaining = BoosterManager.Instance.GetRemainingTime(boosterId);

        if (remaining > 0f)
        {
            // Show timer
            if (timerRoot != null) timerRoot.SetActive(true);

            // Format time: MM:SS or SS
            string timeStr = FormatTime(remaining);

            if (timerText != null)
            {
                timerText.text = timeStr;
            }
        }
        else
        {
            // Hide timer
            if (timerRoot != null) timerRoot.SetActive(false);

            if (timerText != null)
            {
                timerText.text = "";
            }
        }
    }

    string FormatTime(float seconds)
    {
        if (seconds >= 60f)
        {
            // Format as MM:SS
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes}:{secs:D2}";
        }
        else
        {
            // Format as SS.s
            return $"{seconds:F1}s";
        }
    }
}