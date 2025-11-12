using UnityEngine;

/// <summary>
/// FIXED: Coin pickup dengan proper sound call
/// </summary>
public class CoinPickup : MonoBehaviour
{
    public long value = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (PlayerEconomy.Instance != null)
        {
            long finalValue = value;

            if (BoosterManager.Instance != null)
            {
                finalValue = BoosterManager.Instance.ApplyCoinMultiplier(value);
            }

            PlayerEconomy.Instance.AddCoins(finalValue);

            // ✅ FIXED: Play coin pickup sound - check SoundManager exists
            if (SoundManager.Instance != null)
            {
                Debug.Log("[CoinPickup] Playing coin pickup sound...");
                SoundManager.Instance.PlayCoinPickup();
            }
            else
            {
                Debug.LogWarning("[CoinPickup] SoundManager.Instance is NULL!");
            }

            if (finalValue != value)
            {
                Debug.Log($"[CoinPickup] Collected {finalValue} coins (base: {value}, multiplier active!)");
            }
        }

        Destroy(gameObject);
    }
}