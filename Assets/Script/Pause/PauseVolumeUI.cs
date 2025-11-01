using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Volume control untuk Pause Menu di gameplay.
/// UPDATED: Auto-save saat Resume (langsung apply perubahan)
/// </summary>
public class PauseVolumeUI : MonoBehaviour
{
    [Header("Music Volume")]
    public Slider musicSlider;

    [Header("SFX Volume")]
    public Slider sfxSlider;

    [Header("Settings")]
    public bool updateRealtime = true;   // TRUE: langsung apply perubahan

    void Start()
    {
        SetupSliders();
        LoadVolumes();
    }

    void OnEnable()
    {
        LoadVolumes(); // Refresh saat pause menu dibuka
    }

    void OnDisable()
    {
        // Auto-save saat pause menu closed (Resume button)
        SaveCurrentVolumes();

        // Cleanup listeners
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        }
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

    /// <summary>
    /// Load current volumes dari SoundManager (sinkron dengan MainMenu changes)
    /// </summary>
    void LoadVolumes()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[PauseVolumeUI] SoundManager.Instance is null!");
            return;
        }

        // Load current saved volumes (akan sync dengan perubahan dari MainMenu)
        if (musicSlider != null)
        {
            musicSlider.value = SoundManager.Instance.MusicVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = SoundManager.Instance.SFXVolume;
        }

        Debug.Log($"[PauseVolumeUI] Loaded volumes: Music={SoundManager.Instance.MusicVolume:F2}, SFX={SoundManager.Instance.SFXVolume:F2}");
    }

    /// <summary>
    /// Called saat music slider berubah
    /// </summary>
    void OnMusicSliderChanged(float value)
    {
        if (!updateRealtime) return;

        // Apply langsung (preview)
        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            SoundManager.Instance.musicSource.volume = value;
        }
    }

    /// <summary>
    /// Called saat SFX slider berubah
    /// </summary>
    void OnSFXSliderChanged(float value)
    {
        if (!updateRealtime) return;

        // Apply langsung (preview)
        if (SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
        {
            SoundManager.Instance.sfxSource.volume = value;
        }

        // Play test sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// Save current slider values ke PlayerPrefs
    /// Called saat Resume (OnDisable) atau manual
    /// </summary>
    void SaveCurrentVolumes()
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

        Debug.Log($"[PauseVolumeUI] Saved volumes: Music={musicSlider?.value:F2}, SFX={sfxSlider?.value:F2}");
    }

    // ========================================
    // PUBLIC API (optional manual controls)
    // ========================================

    /// <summary>
    /// Manual save (opsional, karena auto-save di OnDisable)
    /// </summary>
    public void ApplyVolumes()
    {
        SaveCurrentVolumes();
    }

    /// <summary>
    /// Reset to default
    /// </summary>
    public void ResetToDefault()
    {
        if (SoundManager.Instance == null) return;

        if (musicSlider != null)
        {
            musicSlider.value = SoundManager.Instance.defaultMusicVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = SoundManager.Instance.defaultSFXVolume;
        }

        // Auto-save via OnDisable saat Resume
        Debug.Log("[PauseVolumeUI] Reset to default");
    }

    [ContextMenu("Test: Load Volumes")]
    void TestLoad()
    {
        LoadVolumes();
    }

    [ContextMenu("Test: Save Volumes")]
    void TestSave()
    {
        SaveCurrentVolumes();
    }
}