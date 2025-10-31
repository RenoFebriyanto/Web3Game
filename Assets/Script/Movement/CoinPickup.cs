using UnityEngine;

/// <summary>
/// UPDATED: Coin pickup dengan random sound (5 variants)
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

            // ✅ NEW: Play random coin pickup sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCoinPickup();
            }

            if (finalValue != value)
            {
                Debug.Log($"[CoinPickup] Collected {finalValue} coins (base: {value}, multiplier active!)");
            }
        }

        Destroy(gameObject);
    }
}