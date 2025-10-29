using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script untuk 1 cooldown timer UI (prefab)
/// Attach ke prefab BoosterCooldown
/// Structure: Root -> Icon (Image) + Slider + TimerText (TMP)
/// </summary>
public class BoosterCooldownUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;          // Icon booster
    public Slider cooldownSlider;    // Slider untuk visualize time
    //public TMP_Text timerText;       // Text untuk countdown (optional)

    [Header("Runtime (Read-Only)")]
    [SerializeField] private string boosterId;
    [SerializeField] private float maxDuration;
    [SerializeField] private float remainingTime;

    public string BoosterId => boosterId;

    /// <summary>
    /// Initialize cooldown untuk booster tertentu
    /// </summary>
    public void Initialize(string id, Sprite icon, float duration)
    {
        boosterId = id;
        maxDuration = duration;
        remainingTime = duration;

        // Set icon
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }

        // Setup slider
        if (cooldownSlider != null)
        {
            cooldownSlider.maxValue = maxDuration;
            cooldownSlider.value = maxDuration; // Full di awal
        }

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (BoosterManager.Instance == null) return;

        // Get remaining time dari BoosterManager
        remainingTime = BoosterManager.Instance.GetRemainingTime(boosterId);

        // Update slider (dari full ke kosong)
        if (cooldownSlider != null)
        {
            cooldownSlider.value = remainingTime;
        }

        //// Update timer text
        //if (timerText != null)
        //{
        //    timerText.text = FormatTime(remainingTime);
        //}

        // Check if expired
        if (remainingTime <= 0f)
        {
            // Destroy this cooldown UI
            Destroy(gameObject);
        }
    }

    string FormatTime(float seconds)
    {
        if (seconds >= 60f)
        {
            // Format: MM:SS
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes}:{secs:D2}";
        }
        else
        {
            // Format: SS.s
            return $"{Mathf.CeilToInt(seconds)}s";
        }
    }
}