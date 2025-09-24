using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource sfxSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void PlaySfx(AudioClip clip)
    {
        if (Instance == null || Instance.sfxSource == null || clip == null) return;
        Instance.sfxSource.PlayOneShot(clip);
    }
}
