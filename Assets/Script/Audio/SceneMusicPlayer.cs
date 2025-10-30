using UnityEngine;

/// <summary>
/// Auto-play music saat scene load.
/// Attach di GameObject di MainMenu & Gameplay scenes.
/// </summary>
public class SceneMusicPlayer : MonoBehaviour
{
    [Header("Scene Music Type")]
    [Tooltip("Pilih type scene ini")]
    public SceneType sceneType = SceneType.MainMenu;

    [Header("Settings")]
    public bool playOnStart = true;
    public bool stopPreviousMusic = false;

    public enum SceneType
    {
        MainMenu,
        Gameplay
    }

    void Start()
    {
        if (playOnStart)
        {
            PlayMusic();
        }
    }

    void PlayMusic()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusicPlayer] SoundManager.Instance is null! Waiting...");
            Invoke(nameof(PlayMusic), 0.5f); // Retry after 0.5s
            return;
        }

        if (stopPreviousMusic)
        {
            SoundManager.Instance.StopMusic();
        }

        switch (sceneType)
        {
            case SceneType.MainMenu:
                SoundManager.Instance.PlayMainMenuMusic();
                Debug.Log("[SceneMusicPlayer] Playing MainMenu music");
                break;

            case SceneType.Gameplay:
                SoundManager.Instance.PlayGameplayMusic();
                Debug.Log("[SceneMusicPlayer] Playing Gameplay music");
                break;
        }
    }

    // Public methods untuk control dari luar (button, etc)
    public void PlayMainMenuMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMainMenuMusic();
        }
    }

    public void PlayGameplayMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGameplayMusic();
        }
    }

    public void StopMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
        }
    }
}