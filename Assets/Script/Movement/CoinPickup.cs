using UnityEngine;

/// <summary>
/// UPDATED: Added random coin pickup sound
/// </summary>
public class CoinPickup : MonoBehaviour
{
    public long value = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (PlayerEconomy.Instance != null)
        {
            // Apply coin multiplier dari booster
            long finalValue = value;

            if (BoosterManager.Instance != null)
            {
                finalValue = BoosterManager.Instance.ApplyCoinMultiplier(value);
            }

            PlayerEconomy.Instance.AddCoins(finalValue);

            // ✅ AUDIO: Play random coin pickup sound
            SoundManager.CoinPickup();

            // Debug log untuk check multiplier
            if (finalValue != value)
            {
                Debug.Log($"[CoinPickup] Collected {finalValue} coins (base: {value}, multiplier active!)");
            }
        }

        Destroy(gameObject);
    }
}