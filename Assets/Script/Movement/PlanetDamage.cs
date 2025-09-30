// PlanetDamage.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlanetDamage : MonoBehaviour
{
    public int damage = 1;
    public string playerTag = "Player";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        var ph = PlayerHealth.Instance;
        if (ph != null)
        {
            ph.TakeDamage(damage);
            // optional: play sfx / effect here
        }
    }
}
