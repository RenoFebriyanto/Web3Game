using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller untuk volume settings (music & SFX).
/// Bisa dipakai di MainMenu Settings atau Pause Menu di Gameplay.
/// Sync dengan SoundManager (persistent).
/// </summary>
public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Music Volume")]
    public Slider musicSlider;
    public TMP_Text musicValueText;      // Optional: show percentage "70%"

    [Header("SFX Volume")]
    public Slider sfxSlider;
    public TMP_Text sfxValueText;        // Optional: show percentage "100%"

    [Header("Settings")]
    public bool updateRealtime = true;   // Update saat slider berubah (vs only on release)
    public bool showPercentage = true;   // Show "70%" text

    void Start()
    {
        // Setup sliders
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.wholeNumbers = false;

            // Add listener
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.wholeNumbers = false;

            // Add listener
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        // Load current values
        LoadVolumes();
    }

    void OnDestroy()
    {
        // Remove listeners
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        }
    }

    void OnEnable()
    {
        // Refresh saat panel dibuka
        LoadVolumes();
    }

    /// <summary>
    /// Load volumes dari SoundManager dan update sliders
    /// </summary>
    void LoadVolumes()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSettingsUI] SoundManager.Instance is null!");
            return;
        }

        // Music volume
        if (musicSlider != null)
        {
            musicSlider.value = SoundManager.Instance.MusicVolume;
        }

        // SFX volume
        if (sfxSlider != null)
        {
            sfxSlider.value = SoundManager.Instance.SFXVolume;
        }

        // Update text displays
        UpdateMusicText(SoundManager.Instance.MusicVolume);
        UpdateSFXText(SoundManager.Instance.SFXVolume);
    }

    /// <summary>
    /// Called saat music slider berubah
    /// </summary>
    void OnMusicSliderChanged(float value)
    {
        if (!updateRealtime) return;

        // Update SoundManager
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(value);
        }

        // Update text
        UpdateMusicText(value);
    }

    /// <summary>
    /// Called saat SFX slider berubah
    /// </summary>
    void OnSFXSliderChanged(float value)
    {
        if (!updateRealtime) return;

        // Update SoundManager
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(value);
        }

        // Update text
        UpdateSFXText(value);

        // Play test sound saat adjust (feedback)
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

    /// <summary>
    /// Apply volumes (jika updateRealtime = false)
    /// </summary>
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

        Debug.Log("[VolumeSettingsUI] Volumes applied");
    }

    /// <summary>
    /// Reset volumes ke default
    /// </summary>
    public void ResetToDefault()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.SetMusicVolume(SoundManager.Instance.defaultMusicVolume);
        SoundManager.Instance.SetSFXVolume(SoundManager.Instance.defaultSFXVolume);

        LoadVolumes(); // Refresh sliders

        Debug.Log("[VolumeSettingsUI] Volumes reset to default");
    }

    /// <summary>
    /// Mute music
    /// </summary>
    public void MuteMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(0f);
        }

        LoadVolumes();
    }

    /// <summary>
    /// Unmute music (restore to 0.7)
    /// </summary>
    public void UnmuteMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicVolume(0.7f);
        }

        LoadVolumes();
    }

    /// <summary>
    /// Mute SFX
    /// </summary>
    public void MuteSFX()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(0f);
        }

        LoadVolumes();
    }

    /// <summary>
    /// Unmute SFX (restore to 1.0)
    /// </summary>
    public void UnmuteSFX()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(1.0f);
        }

        LoadVolumes();
    }

    // ========================================
    // CONTEXT MENU (testing)
    // ========================================

    [ContextMenu("Test: Load Volumes")]
    void TestLoad()
    {
        LoadVolumes();
    }

    [ContextMenu("Test: Apply Volumes")]
    void TestApply()
    {
        ApplyVolumes();
    }

    [ContextMenu("Test: Reset to Default")]
    void TestReset()
    {
        ResetToDefault();
    }
}