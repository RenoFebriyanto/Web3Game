using UnityEngine;

/// <summary>
/// UPDATED: Added planet destroy & shield absorb sounds
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

        // Check SpeedBoost - planet hancur
        if (BoosterManager.Instance != null && BoosterManager.Instance.ShouldDestroyPlanet())
        {
            Debug.Log("[PlanetDamage] Planet destroyed by SpeedBoost!");

            // ✅ AUDIO: Planet destroy sound
            SoundManager.PlanetDestroy();

            // Spawn effect
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
            return;
        }

        // Check Shield - absorb hit
        if (BoosterManager.Instance != null && BoosterManager.Instance.TryAbsorbHit())
        {
            Debug.Log("[PlanetDamage] Hit absorbed by Shield!");

            // ✅ AUDIO: Shield absorb sound
            SoundManager.ShieldAbsorb();

            return;
        }

        // Normal damage
        var ph = PlayerHealth.Instance;
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Debug.Log("[PlanetDamage] Player took damage!");
        }
    }
}