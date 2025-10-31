using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Volume control untuk Pause Menu di gameplay.
/// 2 Sliders: Music & SFX
/// Sync dengan SoundManager (persistent).
/// </summary>
public class PauseVolumeUI : MonoBehaviour
{
    [Header("Music Volume")]
    public Slider musicSlider;
    public TMP_Text musicValueText;      // Optional: "70%"

    [Header("SFX Volume")]
    public Slider sfxSlider;
    public TMP_Text sfxValueText;        // Optional: "100%"

    [Header("Settings")]
    public bool updateRealtime = true;   // Update saat slider berubah
    public bool showPercentage = true;   // Show "70%" text

    void Start()
    {
        SetupSliders();
        LoadVolumes();
    }

    void OnEnable()
    {
        LoadVolumes();
    }

    void OnDisable()
    {
        // Save volumes saat pause menu closed (optional, karena sudah auto-save di SoundManager)
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

    /// <summary>
    /// Load current volumes dari SoundManager
    /// </summary>
    void LoadVolumes()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[PauseVolumeUI] SoundManager.Instance is null!");
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

    /// <summary>
    /// Called saat music slider berubah
    /// </summary>
    void OnMusicSliderChanged(float value)
    {
        if (!updateRealtime) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(value);
        }

        UpdateMusicText(value);
    }

    /// <summary>
    /// Called saat SFX slider berubah
    /// </summary>
    void OnSFXSliderChanged(float value)
    {
        if (!updateRealtime) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }

        UpdateSFXText(value);

        // Play test sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
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

    // ========================================
    // PUBLIC API (untuk button OnClick)
    // ========================================

    public void ApplyVolumes()
    {
        if (SoundManager.Instance == null) return;

        if (musicSlider != null)
        {
            SoundManager.Instance.SetMusicVolume(musicSlider.value);
        }

        if (sfxSlider != null)
        {
            SoundManager.Instance.SetSFXVolume(sfxSlider.value);
        }

        Debug.Log("[PauseVolumeUI] Volumes applied");
    }

    public void ResetToDefault()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.SetMusicVolume(SoundManager.Instance.defaultMusicVolume);
        SoundManager.Instance.SetSFXVolume(SoundManager.Instance.defaultSFXVolume);

        LoadVolumes();

        Debug.Log("[PauseVolumeUI] Volumes reset to default");
    }

    [ContextMenu("Test: Load Volumes")]
    void TestLoad()
    {
        LoadVolumes();
    }
}