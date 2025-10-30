using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dual volume sliders untuk Pause Menu (Music & SFX).
/// Attach script ini ke Pause Menu root panel Anda.
/// </summary>
public class PauseMenuVolume : MonoBehaviour
{
    [Header("Music Volume")]
    public Slider musicSlider;
    public TMP_Text musicValueText;  // Optional: "70%"

    [Header("SFX Volume")]
    public Slider sfxSlider;
    public TMP_Text sfxValueText;    // Optional: "100%"

    [Header("Settings")]
    public bool updateRealtime = true;
    public bool showPercentage = true;

    void Start()
    {
        SetupSliders();
        LoadVolumes();
    }

    void SetupSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.wholeNumbers = false;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.wholeNumbers = false;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }
    }

    void OnEnable()
    {
        LoadVolumes();
    }

    void OnDestroy()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        }
    }

    void LoadVolumes()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[PauseMenuVolume] SoundManager.Instance is null!");
            return;
        }

        if (musicSlider != null)
        {
            musicSlider.value = SoundManager.Instance.MusicVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = SoundManager.Instance.SFXVolume;
        }

        UpdateMusicText(SoundManager.Instance.MusicVolume);
        UpdateSFXText(SoundManager.Instance.SFXVolume);
    }

    void OnMusicSliderChanged(float value)
    {
        if (!updateRealtime) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(value);
        }

        UpdateMusicText(value);
    }

    void OnSFXSliderChanged(float value)
    {
        if (!updateRealtime) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }

        UpdateSFXText(value);

        // Play test sound
        SoundManager.Click();
    }

    void UpdateMusicText(float value)
    {
        if (musicValueText == null) return;

        if (showPercentage)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            musicValueText.text = $"{percentage}%";
        }
        else
        {
            musicValueText.text = value.ToString("F2");
        }
    }

    void UpdateSFXText(float value)
    {
        if (sfxValueText == null) return;

        if (showPercentage)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            sfxValueText.text = $"{percentage}%";
        }
        else
        {
            sfxValueText.text = value.ToString("F2");
        }
    }
}