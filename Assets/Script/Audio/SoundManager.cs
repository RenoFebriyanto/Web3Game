using UnityEngine;

/// <summary>
/// UPDATED: Support for multiple coin sounds, background music, and all game SFX.
/// Singleton persistent across scenes with PlayerPrefs save.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;      // Background music
    public AudioSource sfxSource;        // Sound effects (button, UI)

    [Header("Background Music")]
    public AudioClip musicMainMenu;      // Music untuk MainMenu scene
    public AudioClip musicGameplay;      // Music untuk Gameplay scene

    [Header("UI Sound Clips")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;

    [Header("Coin Pickup Sounds (Multiple Variants)")]
    [Tooltip("Drag multiple coin sounds untuk variety")]
    public AudioClip[] coinPickupSounds; // Array untuk random coin sounds

    [Header("Gameplay Sounds")]
    public AudioClip playerDamageSound;
    public AudioClip fragmentCollectSound;
    public AudioClip starCollectSound;
    public AudioClip planetDestroySound;  // Untuk SpeedBoost
    public AudioClip shieldAbsorbSound;   // Shield absorb hit
    public AudioClip boosterActivateSound; // Booster activation

    [Header("Quest/Shop Sounds")]
    public AudioClip purchaseSuccessSound;
    public AudioClip purchaseFailSound;
    public AudioClip questCompleteSound;
    public AudioClip levelCompleteSound;

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

        // Don't restart if same music already playing
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(musicMainMenu, true);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(musicGameplay, true);
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
    // SFX CONTROL
    // ========================================

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        if (sfxVolume <= 0f) return; // Muted

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ========================================
    // UI SOUNDS
    // ========================================

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

    // ========================================
    // COIN SOUNDS (RANDOM VARIANT)
    // ========================================

    public void PlayCoinPickup()
    {
        if (coinPickupSounds == null || coinPickupSounds.Length == 0)
        {
            Debug.LogWarning("[SoundManager] No coin pickup sounds assigned!");
            return;
        }

        // Pick random coin sound
        AudioClip randomCoin = coinPickupSounds[Random.Range(0, coinPickupSounds.Length)];

        if (randomCoin != null)
        {
            PlaySFX(randomCoin);
        }
    }

    // ========================================
    // GAMEPLAY SOUNDS
    // ========================================

    public void PlayPlayerDamage()
    {
        if (playerDamageSound != null)
        {
            PlaySFX(playerDamageSound);
        }
    }

    public void PlayFragmentCollect()
    {
        if (fragmentCollectSound != null)
        {
            PlaySFX(fragmentCollectSound);
        }
    }

    public void PlayStarCollect()
    {
        if (starCollectSound != null)
        {
            PlaySFX(starCollectSound);
        }
    }

    public void PlayPlanetDestroy()
    {
        if (planetDestroySound != null)
        {
            PlaySFX(planetDestroySound);
        }
    }

    public void PlayShieldAbsorb()
    {
        if (shieldAbsorbSound != null)
        {
            PlaySFX(shieldAbsorbSound);
        }
    }

    public void PlayBoosterActivate()
    {
        if (boosterActivateSound != null)
        {
            PlaySFX(boosterActivateSound);
        }
    }

    // ========================================
    // QUEST/SHOP SOUNDS
    // ========================================

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

    public void PlayQuestComplete()
    {
        if (questCompleteSound != null)
        {
            PlaySFX(questCompleteSound);
        }
    }

    public void PlayLevelComplete()
    {
        if (levelCompleteSound != null)
        {
            PlaySFX(levelCompleteSound);
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

    public static void CoinPickup()
    {
        Instance?.PlayCoinPickup();
    }

    public static void PlayerDamage()
    {
        Instance?.PlayPlayerDamage();
    }

    public static void FragmentCollect()
    {
        Instance?.PlayFragmentCollect();
    }

    public static void StarCollect()
    {
        Instance?.PlayStarCollect();
    }

    public static void PlanetDestroy()
    {
        Instance?.PlayPlanetDestroy();
    }

    public static void ShieldAbsorb()
    {
        Instance?.PlayShieldAbsorb();
    }

    public static void BoosterActivate()
    {
        Instance?.PlayBoosterActivate();
    }

    public static void PurchaseSuccess()
    {
        Instance?.PlayPurchaseSuccess();
    }

    public static void PurchaseFail()
    {
        Instance?.PlayPurchaseFail();
    }

    public static void QuestComplete()
    {
        Instance?.PlayQuestComplete();
    }

    public static void LevelComplete()
    {
        Instance?.PlayLevelComplete();
    }

    // ========================================
    // DEBUG / CONTEXT MENU
    // ========================================

    [ContextMenu("Test Button Click")]
    void TestClick() => PlayButtonClick();

    [ContextMenu("Test Button Hover")]
    void TestHover() => PlayButtonHover();

    [ContextMenu("Test Coin Pickup (Random)")]
    void TestCoin() => PlayCoinPickup();

    [ContextMenu("Test Player Damage")]
    void TestDamage() => PlayPlayerDamage();

    [ContextMenu("Play MainMenu Music")]
    void TestMainMenuMusic() => PlayMainMenuMusic();

    [ContextMenu("Play Gameplay Music")]
    void TestGameplayMusic() => PlayGameplayMusic();

    [ContextMenu("Reset Volumes to Default")]
    void ResetVolumes()
    {
        SetMusicVolume(defaultMusicVolume);
        SetSFXVolume(defaultSFXVolume);
        Debug.Log("[SoundManager] Volumes reset to default");
    }
}