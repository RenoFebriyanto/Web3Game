using UnityEngine;

/// <summary>
/// Centralized sound manager untuk button clicks, hover, dan music.
/// Singleton persistent across scenes.
/// Settings disimpan di PlayerPrefs.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;      // Background music
    public AudioSource sfxSource;        // Sound effects (button, UI)

    [Header("UI Sound Clips (Assign in Inspector)")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip purchaseSuccessSound;
    public AudioClip purchaseFailSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)]
    public float defaultSFXVolume = 1.0f;

    // PlayerPrefs keys
    const string PREF_MUSIC_VOLUME = "Kulino_MusicVolume_v1";
    const string PREF_SFX_VOLUME = "Kulino_SFXVolume_v1";

    // Runtime
    private float musicVolume;
    private float sfxVolume;

    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup audio sources
        SetupAudioSources();

        // Load saved volumes
        LoadVolumes();

        Debug.Log($"[SoundManager] Initialized: Music={musicVolume:F2}, SFX={sfxVolume:F2}");
    }

    void SetupAudioSources()
    {
        // Create music source if not assigned
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            GameObject sfxGO = new GameObject("SFXSource");
            sfxGO.transform.SetParent(transform);
            sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    void LoadVolumes()
    {
        // Load music volume (default 0.7)
        musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, defaultMusicVolume);
        musicVolume = Mathf.Clamp01(musicVolume);

        // Load SFX volume (default 1.0)
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, defaultSFXVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);

        // Apply to sources
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolume);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
        PlayerPrefs.Save();
    }

    // ========================================
    // PUBLIC API - Music Control
    // ========================================

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        SaveVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        SaveVolumes();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PauseMusic()
    {
        if (musicSource != null) musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null) musicSource.UnPause();
    }

    // ========================================
    // PUBLIC API - SFX Control
    // ========================================

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        if (sfxVolume <= 0f) return; // Muted

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // Convenience methods untuk UI sounds
    public void PlayButtonClick()
    {
        if (buttonClickSound != null)
        {
            PlaySFX(buttonClickSound);
        }
    }

    public void PlayButtonHover()
    {
        if (buttonHoverSound != null)
        {
            PlaySFX(buttonHoverSound);
        }
    }

    public void PlayPurchaseSuccess()
    {
        if (purchaseSuccessSound != null)
        {
            PlaySFX(purchaseSuccessSound);
        }
    }

    public void PlayPurchaseFail()
    {
        if (purchaseFailSound != null)
        {
            PlaySFX(purchaseFailSound);
        }
    }

    // ========================================
    // STATIC HELPERS (untuk easy access)
    // ========================================

    public static void Click()
    {
        Instance?.PlayButtonClick();
    }

    public static void Hover()
    {
        Instance?.PlayButtonHover();
    }

    public static void PurchaseSuccess()
    {
        Instance?.PlayPurchaseSuccess();
    }

    public static void PurchaseFail()
    {
        Instance?.PlayPurchaseFail();
    }

    // ========================================
    // DEBUG / CONTEXT MENU
    // ========================================

    [ContextMenu("Test Button Click")]
    void TestClick() => PlayButtonClick();

    [ContextMenu("Test Button Hover")]
    void TestHover() => PlayButtonHover();

    [ContextMenu("Test Purchase Success")]
    void TestPurchaseSuccess() => PlayPurchaseSuccess();

    [ContextMenu("Test Purchase Fail")]
    void TestPurchaseFail() => PlayPurchaseFail();

    [ContextMenu("Reset Volumes to Default")]
    void ResetVolumes()
    {
        SetMusicVolume(defaultMusicVolume);
        SetSFXVolume(defaultSFXVolume);
        Debug.Log("[SoundManager] Volumes reset to default");
    }
}