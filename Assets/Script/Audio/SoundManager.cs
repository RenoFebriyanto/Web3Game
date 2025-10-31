using UnityEngine;

/// <summary>
/// EXTENDED: SoundManager dengan support untuk semua gameplay sounds
/// Tambahan: Coin pickup variants, gameplay SFX, collision sounds
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("UI Sound Clips")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip purchaseSuccessSound;
    public AudioClip purchaseFailSound;

    [Header("=== GAMEPLAY SOUNDS ===")]

    [Header("Coin Pickup (5 Variants - Random)")]
    public AudioClip coinPickup1;
    public AudioClip coinPickup2;
    public AudioClip coinPickup3;
    public AudioClip coinPickup4;
    

    [Header("Collectibles")]
    public AudioClip fragmentPickupSound;
    public AudioClip starPickupSound;

    [Header("Booster Sounds")]
    public AudioClip boosterActivateSound;      // Ketika klik booster button
    public AudioClip shieldActivateSound;       // Shield visual muncul
    public AudioClip shieldBreakSound;          // Shield hancur

    [Header("Collision Sounds")]
    public AudioClip planetCollisionSound;      // Planet hit player (damage)
    public AudioClip planetDestroySound;        // Shield absorb hit (planet hancur)

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)]
    public float defaultSFXVolume = 1.0f;

    const string PREF_MUSIC_VOLUME = "Kulino_MusicVolume_v1";
    const string PREF_SFX_VOLUME = "Kulino_SFXVolume_v1";

    private float musicVolume;
    private float sfxVolume;

    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        LoadVolumes();

        Debug.Log($"[SoundManager] Initialized: Music={musicVolume:F2}, SFX={sfxVolume:F2}");
    }

    void SetupAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

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
        musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, defaultMusicVolume);
        musicVolume = Mathf.Clamp01(musicVolume);

        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, defaultSFXVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);

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
    // MUSIC CONTROL
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
    // SFX CONTROL (EXISTING)
    // ========================================

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        if (sfxVolume <= 0f) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

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
    // NEW: GAMEPLAY SOUNDS
    // ========================================

    /// <summary>
    /// Play random coin pickup sound (5 variants)
    /// </summary>
    public void PlayCoinPickup()
    {
        AudioClip[] coinSounds = { coinPickup1, coinPickup2, coinPickup3, coinPickup4 };

        // Filter out null clips
        var validSounds = System.Array.FindAll(coinSounds, clip => clip != null);

        if (validSounds.Length == 0)
        {
            Debug.LogWarning("[SoundManager] No coin pickup sounds assigned!");
            return;
        }

        // Play random variant
        AudioClip randomCoin = validSounds[Random.Range(0, validSounds.Length)];
        PlaySFX(randomCoin);
    }

    public void PlayFragmentPickup()
    {
        if (fragmentPickupSound != null)
        {
            PlaySFX(fragmentPickupSound);
        }
    }

    public void PlayStarPickup()
    {
        if (starPickupSound != null)
        {
            PlaySFX(starPickupSound);
        }
    }

    public void PlayBoosterActivate()
    {
        if (boosterActivateSound != null)
        {
            PlaySFX(boosterActivateSound);
        }
    }

    public void PlayShieldActivate()
    {
        if (shieldActivateSound != null)
        {
            PlaySFX(shieldActivateSound);
        }
    }

    public void PlayShieldBreak()
    {
        if (shieldBreakSound != null)
        {
            PlaySFX(shieldBreakSound);
        }
    }

    public void PlayPlanetCollision()
    {
        if (planetCollisionSound != null)
        {
            PlaySFX(planetCollisionSound);
        }
    }

    public void PlayPlanetDestroy()
    {
        if (planetDestroySound != null)
        {
            PlaySFX(planetDestroySound);
        }
    }

    // ========================================
    // STATIC HELPERS
    // ========================================

    public static void Click() => Instance?.PlayButtonClick();
    public static void Hover() => Instance?.PlayButtonHover();
    public static void PurchaseSuccess() => Instance?.PlayPurchaseSuccess();
    public static void PurchaseFail() => Instance?.PlayPurchaseFail();

    // ========================================
    // DEBUG / CONTEXT MENU
    // ========================================

    [ContextMenu("Test Button Click")]
    void TestClick() => PlayButtonClick();

    [ContextMenu("Test Coin Pickup (Random)")]
    void TestCoinPickup() => PlayCoinPickup();

    [ContextMenu("Test Shield Activate")]
    void TestShieldActivate() => PlayShieldActivate();

    [ContextMenu("Test Planet Collision")]
    void TestPlanetCollision() => PlayPlanetCollision();

    [ContextMenu("Reset Volumes to Default")]
    void ResetVolumes()
    {
        SetMusicVolume(defaultMusicVolume);
        SetSFXVolume(defaultSFXVolume);
        Debug.Log("[SoundManager] Volumes reset to default");
    }
}