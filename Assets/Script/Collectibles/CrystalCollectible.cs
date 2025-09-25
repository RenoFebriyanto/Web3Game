// Assets/Script/Collectibles/CrystalCollectible.cs
using UnityEngine;

public class CrystalCollectible : MonoBehaviour, IPoolable
{
    public int amount = 1;
    public string poolId = "crystal";
    public float lifetime = 12f;

    [Header("Visual Effects")]
    public ParticleSystem collectEffect;
    public float rotationSpeed = 90f;
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;

    [Header("Audio")]
    public AudioClip crystalClip;
    public string sfxSourceName = "SFXSource";

    private float spawnTime;
    private Vector3 originalPosition;
    private bool isCollected = false;

    void OnEnable()
    {
        spawnTime = Time.time;
        originalPosition = transform.position;
        isCollected = false;
    }

    public void OnSpawned()
    {
        spawnTime = Time.time;
        originalPosition = transform.position;
        isCollected = false;
    }

    void Update()
    {
        if (isCollected) return;

        // Lifetime check
        if (lifetime > 0 && Time.time - spawnTime >= lifetime)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        // Visual animations
        AnimateCrystal();
    }

    private void AnimateCrystal()
    {
        // Rotation animation
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        // Float animation
        float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = originalPosition + Vector3.up * floatOffset;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag("Player")) return;

        isCollected = true;

        // Notify MissionManager about crystal collection
        var missionManager = MissionManager.Instance;
        if (missionManager != null)
        {
            missionManager.OnCrystalCollected(amount);
        }

        // Also notify InGameManager if exists
        var gameManager = EnhancedInGameManager.Instance;
        if (gameManager != null)
        {
            // You can add a method to InGameManager to handle crystals
            gameManager.gameObject.SendMessage("OnCrystalCollected", amount, SendMessageOptions.DontRequireReceiver);
        }

        // Update PlayerEconomy (crystals as currency)
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddShards(amount); // Using shards as crystals
        }

        // Play collection effects
        PlayCollectionEffects();

        // Destroy/deactivate
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void PlayCollectionEffects()
    {
        // Play particle effect
        if (collectEffect != null)
        {
            var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // Play sound effect
        PlayCrystalSfx();

        // You can add screen shake, score popup, etc. here
    }

    private void PlayCrystalSfx()
    {
        if (crystalClip == null) return;

        var sfxObj = GameObject.Find(sfxSourceName);
        if (sfxObj != null)
        {
            var aud = sfxObj.GetComponent<AudioSource>();
            if (aud != null) aud.PlayOneShot(crystalClip);
            return;
        }

        // Fallback: create temp audio source
        var go = new GameObject("tmp_crystal_sfx");
        var a = go.AddComponent<AudioSource>();
        a.PlayOneShot(crystalClip);
        Destroy(go, crystalClip.length + 0.1f);
    }
}