using UnityEngine;

/// <summary>
/// REPLACE: Assets/Script/Movement/PlanetDamage.cs
/// Updated dengan support untuk Shield dan SpeedBoost booster
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlanetDamage : MonoBehaviour
{
    public int damage = 1;
    public string playerTag = "Player";

    [Header("Destroy Effect (Optional)")]
    public GameObject destroyEffect; // VFX saat planet hancur

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Check SpeedBoost - planet hancur, player tidak kena damage
        if (BoosterManager.Instance != null && BoosterManager.Instance.ShouldDestroyPlanet())
        {
            Debug.Log("[PlanetDamage] Planet destroyed by SpeedBoost!");

            // Spawn effect
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }

            // Destroy planet
            Destroy(gameObject);
            return;
        }

        // Check Shield - absorb hit
        if (BoosterManager.Instance != null && BoosterManager.Instance.TryAbsorbHit())
        {
            Debug.Log("[PlanetDamage] Hit absorbed by Shield!");

            // Optional: play shield effect, sound, etc

            // Planet tetap hidup (tidak hancur), tapi hit diabsorb
            return;
        }

        // Normal damage - shield tidak aktif atau habis
        var ph = PlayerHealth.Instance;
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Debug.Log("[PlanetDamage] Player took damage!");
        }

        // Optional: play damage sound/effect here
    }
}