using UnityEngine;

/// <summary>
/// ✅ FIXED: Coin pickup HANYA trigger animation
/// TIDAK langsung add ke PlayerEconomy!
/// </summary>
public class CoinPickup : MonoBehaviour
{
    public long value = 1; // Changed to 1 (base value)

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ✅ CRITICAL: Calculate final value dengan booster multiplier
        long finalValue = value;
        
        if (BoosterManager.Instance != null && BoosterManager.Instance.coin2xActive)
        {
            finalValue = value * 2; // Coin2x active
        }

        // ✅ ANIMATION: Trigger popup animation
        if (CollectibleAnimationManager.Instance != null)
        {
            CollectibleAnimationManager.Instance.AnimateCoinCollect(
                transform.position, 
                (int)finalValue // Pass final value (1 or 2)
            );
        }
        else
        {
            // ❌ FALLBACK: Langsung add ke counter (no animation)
            Debug.LogWarning("[CoinPickup] CollectibleAnimationManager not found! Adding directly to counter.");
            
            if (CoinCounterUI.Instance != null)
            {
                CoinCounterUI.Instance.AddCoins(finalValue);
            }
        }

        // ✅ Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayCoinPickup();
        }
        else
        {
            Debug.LogWarning("[CoinPickup] SoundManager.Instance is NULL!");
        }

        // Destroy coin object
        Destroy(gameObject);
    }
}