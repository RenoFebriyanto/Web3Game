// Assets/Script/Collectibles/UpdatedCoinCollectible.cs
// This is an updated version of your existing CoinCollectible with mission support
using UnityEngine;

public class UpdatedCoinCollectible : MonoBehaviour, IPoolable
{
    public int amount = 1;
    public string poolId = "coin";

    public float lifetime = 12f;
    float spawnTime;

    [Header("Audio")]
    public AudioClip coinClip; // assign di prefab
    public string sfxSourceName = "SFXSource"; // nama GameObject yang punya AudioSource

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    public void OnSpawned()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (lifetime > 0 && Time.time - spawnTime >= lifetime)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // NEW: Notify MissionManager about coin collection (highest priority)
        var missionManager = MissionManager.Instance;
        if (missionManager != null)
        {
            missionManager.OnCoinCollected(amount);
        }

        // Inform InGameManager (updated to use new EnhancedInGameManager)
        var gm = EnhancedInGameManager.Instance;
        if (gm != null)
        {
            gm.AddCoins(amount); // This will also update MissionManager
        }
        else
        {
            // Fallback to old InGameManager if it exists
            
            
                // Final fallback to PlayerEconomy (global)
                if (PlayerEconomy.Instance != null)
                {
                    PlayerEconomy.Instance.AddCoins(amount);
                }
            
        }

        // play coin SFX via a central SFX AudioSource
        PlayCoinSfx();

        // return / destroy
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void PlayCoinSfx()
    {
        if (coinClip == null) return;
        var sfxObj = GameObject.Find(sfxSourceName);
        if (sfxObj != null)
        {
            var aud = sfxObj.GetComponent<AudioSource>();
            if (aud != null) aud.PlayOneShot(coinClip);
            return;
        }
        // fallback: create temp audio source at runtime
        var go = new GameObject("tmp_coin_sfx");
        var a = go.AddComponent<AudioSource>();
        a.PlayOneShot(coinClip);
        Destroy(go, coinClip.length + 0.1f);
    }
}