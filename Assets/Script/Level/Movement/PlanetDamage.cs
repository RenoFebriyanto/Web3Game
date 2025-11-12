using UnityEngine;

/// <summary>
/// ✅ FIXED: Planet Damage dengan proper sound support
/// Supports Shield, SpeedBoost, dan collision sound
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlanetDamage : MonoBehaviour
{
    public int damage = 1;
    public string playerTag = "Player";

    [Header("Effects")]
    public GameObject destroyEffect; // VFX saat planet hancur
    public GameObject hitEffect; // VFX saat player kena damage

    [Header("Debug")]
    public bool enableDebugLogs = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (enableDebugLogs)
        {
            Debug.Log("[PlanetDamage] Collision detected with player");
        }

        // ========================================
        // CHECK 1: SPEEDBOOST - Planet hancur, player tidak kena damage
        // ========================================
        if (BoosterManager.Instance != null && BoosterManager.Instance.ShouldDestroyPlanet())
        {
            if (enableDebugLogs)
            {
                Debug.Log("[PlanetDamage] SpeedBoost active - Planet destroyed!");
            }

            // ✅ Play planet destroy sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlanetDestroy();
            }

            // Spawn destroy effect
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }

            // Destroy planet
            Destroy(gameObject);
            return;
        }

        // ========================================
        // CHECK 2: SHIELD - Absorb hit, planet hancur
        // ========================================
        if (BoosterManager.Instance != null && BoosterManager.Instance.TryAbsorbHit())
        {
            if (enableDebugLogs)
            {
                Debug.Log("[PlanetDamage] Shield absorbed hit!");
            }

            // ✅ Play shield break sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayShieldBreak();
            }

            // ✅ Play planet destroy sound (planet hancur karena shield absorb)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlanetDestroy();
            }

            // Spawn destroy effect
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }

            // Destroy planet (shield absorb = planet hancur)
            Destroy(gameObject);
            return;
        }

        // ========================================
        // CHECK 3: NORMAL DAMAGE - No shield, no speedboost
        // ========================================
        if (enableDebugLogs)
        {
            Debug.Log("[PlanetDamage] Normal collision - Player takes damage");
        }

        // ✅ Play planet collision sound (player kena damage)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlanetCollision();
        }
        else
        {
            Debug.LogWarning("[PlanetDamage] SoundManager.Instance is NULL! Cannot play collision sound.");
        }

        // Apply damage to player
        var ph = PlayerHealth.Instance;
        if (ph != null)
        {
            ph.TakeDamage(damage);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[PlanetDamage] Player took {damage} damage. Lives: {ph.currentLives}/{ph.maxLives}");
            }
        }
        else
        {
            Debug.LogWarning("[PlanetDamage] PlayerHealth.Instance is NULL!");
        }

        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, other.transform.position, Quaternion.identity);
        }

        // Planet tetap hidup (tidak hancur) setelah collision normal
        // Player yang kena damage
    }

    [ContextMenu("Test: Simulate Collision")]
    void TestCollision()
    {
        Debug.Log("[PlanetDamage] Test collision triggered");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlanetCollision();
            Debug.Log("✓ Played planet collision sound");
        }
        else
        {
            Debug.LogError("❌ SoundManager.Instance is NULL!");
        }
    }

    void OnValidate()
    {
        // Validate collider exists
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[PlanetDamage] {gameObject.name}: Missing Collider2D component!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[PlanetDamage] {gameObject.name}: Collider2D must be set to 'Is Trigger'!");
        }
    }
}