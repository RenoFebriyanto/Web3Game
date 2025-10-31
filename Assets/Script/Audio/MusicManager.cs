using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager untuk background music per scene.
/// Attach ke GameObject di SETIAP scene yang butuh background music.
/// Akan auto-play music saat scene loaded.
/// </summary>
public class MusicManager : MonoBehaviour
{
    [Header("Music for This Scene")]
    [Tooltip("Background music clip untuk scene ini")]
    public AudioClip backgroundMusic;

    [Header("Settings")]
    [Tooltip("Auto-play music saat scene start?")]
    public bool autoPlay = true;

    [Tooltip("Loop music?")]
    public bool loop = true;

    [Tooltip("Fade in duration (seconds)")]
    public float fadeInDuration = 1f;

    private bool isFading = false;
    private float fadeTimer = 0f;

    void Start()
    {
        if (autoPlay && backgroundMusic != null)
        {
            PlayMusic();
        }
    }

    /// <summary>
    /// Play music untuk scene ini
    /// </summary>
    public void PlayMusic()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[MusicManager] SoundManager.Instance is null!");
            return;
        }

        if (backgroundMusic == null)
        {
            Debug.LogWarning("[MusicManager] No background music assigned for this scene!");
            return;
        }

        // Check jika music yang sama sudah playing (skip if same)
        if (SoundManager.Instance.musicSource != null &&
            SoundManager.Instance.musicSource.clip == backgroundMusic &&
            SoundManager.Instance.musicSource.isPlaying)
        {
            Debug.Log("[MusicManager] Same music already playing, skipping...");
            return;
        }

        // Play music
        SoundManager.Instance.PlayMusic(backgroundMusic, loop);

        // Optional: Fade in
        if (fadeInDuration > 0f)
        {
            StartFadeIn();
        }

        Debug.Log($"[MusicManager] Playing background music: {backgroundMusic.name}");
    }

    /// <summary>
    /// Stop music
    /// </summary>
    public void StopMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
        }
    }

    /// <summary>
    /// Fade in music (smooth start)
    /// </summary>
    void StartFadeIn()
    {
        if (SoundManager.Instance == null || SoundManager.Instance.musicSource == null) return;

        isFading = true;
        fadeTimer = 0f;
        SoundManager.Instance.musicSource.volume = 0f;
    }

    void Update()
    {
        if (!isFading) return;

        fadeTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(fadeTimer / fadeInDuration);

        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            float targetVolume = SoundManager.Instance.MusicVolume;
            SoundManager.Instance.musicSource.volume = Mathf.Lerp(0f, targetVolume, progress);
        }

        if (progress >= 1f)
        {
            isFading = false;
        }
    }

    void OnDestroy()
    {
        // Optional: Stop music saat scene change (jika ingin music berhenti saat pindah scene)
        // Uncomment jika ingin behavior ini:
        // StopMusic();
    }

    // ========================================
    // PUBLIC API
    // ========================================

    [ContextMenu("Play Music Now")]
    public void PlayMusicNow()
    {
        PlayMusic();
    }

    [ContextMenu("Stop Music Now")]
    public void StopMusicNow()
    {
        StopMusic();
    }
}