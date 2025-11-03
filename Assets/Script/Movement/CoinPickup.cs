using UnityEngine;

/// <summary>
/// FIXED: Play sound BEFORE destroy to prevent audio cut-off
/// </summary>
public class CoinPickup : MonoBehaviour
{
    public long value = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ✅ CRITICAL FIX: Play sound FIRST (before any destroy logic)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayCoinPickup();
            Debug.Log("[CoinPickup] ✓ Played coin pickup sound");
        }
        else
        {
            Debug.LogWarning("[CoinPickup] SoundManager.Instance is NULL!");
        }

        // Grant coins after sound
        if (PlayerEconomy.Instance != null)
        {
            long finalValue = value;

            if (BoosterManager.Instance != null)
            {
                finalValue = BoosterManager.Instance.ApplyCoinMultiplier(value);
            }

            PlayerEconomy.Instance.AddCoins(finalValue);

            if (finalValue != value)
            {
                Debug.Log($"[CoinPickup] Collected {finalValue} coins (base: {value}, multiplier active!)");
            }
        }

        // Destroy LAST (after sound & coin grant)
        Destroy(gameObject);
    }
}