using UnityEngine;

public class PlanetDamage : MonoBehaviour
{
    public int damage = 1;
    public bool destroyOnHit = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Prefer langsung memanggil PlayerHealth pada player
        var ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.ApplyHit(damage);
        }
        else
        {
            // Fallback: kirim pesan ke InGameManager (jika ada) tanpa menyebabkan error bila tidak ada method-nya
            var gm = FindObjectOfType<InGameManager>();
            if (gm != null)
            {
                // Try to send a message; DontRequireReceiver agar tidak error bila method tidak ada
                gm.gameObject.SendMessage("PlayerHit", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        // optional visual/SFX here

        if (destroyOnHit) Destroy(gameObject);
    }
}
