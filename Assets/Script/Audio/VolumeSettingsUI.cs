using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Volume control untuk Settings di MainMenu.
/// UPDATED: Support Confirm/Cancel - perubahan hanya disimpan saat Confirm
/// </summary>
public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Music Volume")]
    public Slider musicSlider;

    [Header("SFX Volume")]
    public Slider sfxSlider;

    [Header("Settings")]
    public bool updateRealtime = false;   // FALSE: hanya preview, tidak save langsung

    // Temporary storage untuk perubahan (sebelum confirm)
    private float tempMusicVolume;
    private float tempSfxVolume;

    // Original values (untuk cancel)
    private float originalMusicVolume;
    private float originalSfxVolume;

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
    /// Load volumes dari SoundManager dan update sliders + store original values
    /// </summary>
    void LoadVolumes()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSettingsUI] SoundManager.Instance is null!");
            return;
        }

        // Load current saved volumes
        float currentMusic = SoundManager.Instance.MusicVolume;
        float currentSfx = SoundManager.Instance.SFXVolume;

        // Store original values (untuk cancel)
        originalMusicVolume = currentMusic;
        originalSfxVolume = currentSfx;

        // Set temp values
        tempMusicVolume = currentMusic;
        tempSfxVolume = currentSfx;

        // Update sliders
        if (musicSlider != null)
        {
            musicSlider.value = currentMusic;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = currentSfx;
        }

        Debug.Log($"[VolumeSettingsUI] Loaded volumes: Music={currentMusic:F2}, SFX={currentSfx:F2}");
    }

    /// <summary>
    /// Called saat music slider berubah - hanya preview, tidak save
    /// </summary>
    void OnMusicSliderChanged(float value)
    {
        tempMusicVolume = value;

        // Preview audio (apply langsung ke AudioSource tapi jangan save)
        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            SoundManager.Instance.musicSource.volume = value;
        }
    }

    /// <summary>
    /// Called saat SFX slider berubah - hanya preview, tidak save
    /// </summary>
    void OnSFXSliderChanged(float value)
    {
        tempSfxVolume = value;

        // Preview audio (apply langsung ke AudioSource tapi jangan save)
        if (SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
        {
            SoundManager.Instance.sfxSource.volume = value;
        }

        // Play test sound saat adjust (feedback)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }
    }

    // ========================================
    // PUBLIC API (untuk button OnClick)
    // ========================================

    /// <summary>
    /// CONFIRM: Save perubahan ke PlayerPrefs dan close settings
    /// Call dari button "Confirm" (button hijau)
    /// </summary>
    public void ConfirmChanges()
    {
        if (SoundManager.Instance == null) return;

        // Save ke SoundManager (akan auto-save ke PlayerPrefs)
        SoundManager.Instance.SetMusicVolume(tempMusicVolume);
        SoundManager.Instance.SetSFXVolume(tempSfxVolume);

        Debug.Log($"[VolumeSettingsUI] Changes confirmed and saved: Music={tempMusicVolume:F2}, SFX={tempSfxVolume:F2}");

        // Play success sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }
    }

    /// <summary>
    /// CANCEL: Revert ke original values dan close settings
    /// Call dari button "Close" (button merah)
    /// </summary>
    public void CancelChanges()
    {
        if (SoundManager.Instance == null) return;

        // Restore original values (tanpa save)
        if (SoundManager.Instance.musicSource != null)
        {
            SoundManager.Instance.musicSource.volume = originalMusicVolume;
        }

        if (SoundManager.Instance.sfxSource != null)
        {
            SoundManager.Instance.sfxSource.volume = originalSfxVolume;
        }

        // Reset sliders ke original
        if (musicSlider != null)
        {
            musicSlider.value = originalMusicVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = originalSfxVolume;
        }

        tempMusicVolume = originalMusicVolume;
        tempSfxVolume = originalSfxVolume;

        Debug.Log($"[VolumeSettingsUI] Changes cancelled, restored to: Music={originalMusicVolume:F2}, SFX={originalSfxVolume:F2}");
    }

    /// <summary>
    /// Reset volumes ke default (tidak langsung save)
    /// </summary>
    public void ResetToDefault()
    {
        if (SoundManager.Instance == null) return;

        float defaultMusic = SoundManager.Instance.defaultMusicVolume;
        float defaultSfx = SoundManager.Instance.defaultSFXVolume;

        if (musicSlider != null)
        {
            musicSlider.value = defaultMusic;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = defaultSfx;
        }

        tempMusicVolume = defaultMusic;
        tempSfxVolume = defaultSfx;

        Debug.Log("[VolumeSettingsUI] Reset to default (preview only, not saved yet)");
    }

    [ContextMenu("Test: Load Volumes")]
    void TestLoad()
    {
        LoadVolumes();
    }
}