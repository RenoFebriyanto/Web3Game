// CoinPickup.cs (example)
using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public long value = 10;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerEconomy.Instance != null) PlayerEconomy.Instance.AddCoins(value);
        
        Destroy(gameObject);
    }
}
