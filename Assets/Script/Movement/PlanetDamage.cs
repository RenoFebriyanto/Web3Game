using UnityEngine;

/// <summary>
/// FIXED: Add sounds for planet destroy (speedboost) and player damage
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlanetDamage : MonoBehaviour
{
    public int damage = 1;
    public string playerTag = "Player";

    [Header("Destroy Effect (Optional)")]
    public GameObject destroyEffect;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // ========================================
        // CASE 1: SpeedBoost - Planet Destroyed
        // ========================================
        if (BoosterManager.Instance != null && BoosterManager.Instance.ShouldDestroyPlanet())
        {
            Debug.Log("[PlanetDamage] Planet destroyed by SpeedBoost!");

            // ✅ FIX: Play planet destroy sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlanetDestroy();
                Debug.Log("[PlanetDamage] ✓ Played planet destroy sound");
            }

            // Spawn effect
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }

            // Destroy planet
            Destroy(gameObject);
            return;
        }

        // ========================================
        // CASE 2: Shield - Hit Absorbed
        // ========================================
        if (BoosterManager.Instance != null && BoosterManager.Instance.TryAbsorbHit())
        {
            Debug.Log("[PlanetDamage] Hit absorbed by Shield!");

            // ✅ FIX: Play shield break sound (already handled in BoosterManager.TryAbsorbHit)
            // Shield break sound dimainkan di BoosterManager.TryAbsorbHit()

            // Planet tetap hidup, hit diabsorb
            return;
        }

        // ========================================
        // CASE 3: Normal Damage - Player Hit
        // ========================================
        var ph = PlayerHealth.Instance;
        if (ph != null)
        {
            // ✅ FIX: Play planet collision sound BEFORE taking damage
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlanetCollision();
                Debug.Log("[PlanetDamage] ✓ Played planet collision sound");
            }

            ph.TakeDamage(damage);
            Debug.Log("[PlanetDamage] Player took damage!");
        }
    }
}