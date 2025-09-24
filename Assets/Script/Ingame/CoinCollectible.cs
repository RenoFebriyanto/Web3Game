using UnityEngine;

public class CoinCollectible : MonoBehaviour, IPoolable
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

        // inform InGameManager / PlayerEconomy if you prefer
        var gm = InGameManager.Instance;
        if (gm != null)
        {
            gm.AddCoins(amount); // jika InGameManager memiliki AddCoins
        }
        else
        {
            // fallback ke PlayerEconomy (global)
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
