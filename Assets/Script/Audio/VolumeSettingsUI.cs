using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Volume control untuk Settings di MainMenu.
/// FIXED: Enhanced logging untuk debug button connection issue
/// </summary>
public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Music Volume")]
    public Slider musicSlider;

    [Header("SFX Volume")]
    public Slider sfxSlider;

    [Header("Buttons (Assign di Inspector untuk auto-setup)")]
    [Tooltip("Button Confirm (hijau) - akan auto-setup OnClick")]
    public Button confirmButton;

    [Tooltip("Button Cancel/Close (X merah) - akan auto-setup OnClick")]
    public Button cancelButton;

    [Header("Settings")]
    public bool updateRealtime = false;   // FALSE: hanya preview, tidak save langsung

    // Temporary storage untuk perubahan (sebelum confirm)
    private float tempMusicVolume;
    private float tempSfxVolume;

    // Original values (untuk cancel)
    private float originalMusicVolume;
    private float originalSfxVolume;

    void Awake()
    {
        // ✅ AUTO-SETUP BUTTONS di Awake (sebelum Start)
        SetupButtons();
    }

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

    /// <summary>
    /// ✅ NEW: Auto-setup buttons di Awake
    /// </summary>
    void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmChanges);
            Debug.Log("[VolumeSettingsUI] ✓ Confirm button auto-setup complete");
        }
        else
        {
            Debug.LogWarning("[VolumeSettingsUI] ⚠️ confirmButton not assigned in Inspector!");
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelChanges);
            Debug.Log("[VolumeSettingsUI] ✓ Cancel button auto-setup complete");
        }
        else
        {
            Debug.LogWarning("[VolumeSettingsUI] ⚠️ cancelButton not assigned in Inspector!");
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
        Debug.Log("=== [VolumeSettingsUI] ConfirmChanges() CALLED ===");

        if (SoundManager.Instance == null)
        {
            Debug.LogError("[VolumeSettingsUI] SoundManager.Instance is NULL!");
            return;
        }

        // Save ke SoundManager (akan auto-save ke PlayerPrefs)
        SoundManager.Instance.SetMusicVolume(tempMusicVolume);
        SoundManager.Instance.SetSFXVolume(tempSfxVolume);

        Debug.Log($"[VolumeSettingsUI] ✓✓✓ SAVED: Music={tempMusicVolume:F2}, SFX={tempSfxVolume:F2}");

        // Update original values (sekarang ini jadi values yang disimpan)
        originalMusicVolume = tempMusicVolume;
        originalSfxVolume = tempSfxVolume;

        // Play button click sound (bukan purchase success)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // Auto-close settings setelah confirm
        var settingsController = FindFirstObjectByType<Settings>();
        if (settingsController != null)
        {
            Debug.Log("[VolumeSettingsUI] Closing settings via Settings.CloseSettings()");
            settingsController.CloseSettings();
        }
        else
        {
            Debug.LogWarning("[VolumeSettingsUI] Settings controller not found!");
        }
    }

    /// <summary>
    /// CANCEL: Revert ke original values dan close settings
    /// Call dari button "Close" (button merah) atau tombol X
    /// </summary>
    public void CancelChanges()
    {
        Debug.Log("=== [VolumeSettingsUI] CancelChanges() CALLED ===");

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

        Debug.Log($"[VolumeSettingsUI] ✓ REVERTED to: Music={originalMusicVolume:F2}, SFX={originalSfxVolume:F2}");

        // Auto-close settings setelah cancel
        var settingsController = FindFirstObjectByType<Settings>();
        if (settingsController != null)
        {
            settingsController.CloseSettings();
        }
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

    [ContextMenu("Test: Confirm Changes")]
    void TestConfirm()
    {
        ConfirmChanges();
    }

    [ContextMenu("Test: Cancel Changes")]
    void TestCancel()
    {
        CancelChanges();
    }

    [ContextMenu("Test: Load Volumes")]
    void TestLoad()
    {
        LoadVolumes();
    }
}