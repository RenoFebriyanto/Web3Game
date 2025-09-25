using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public int maxHits = 3;
    int currentHits = 0;

    public event Action<int, int> OnHitChanged; // (currentHits, maxHits)

    void Start()
    {
        currentHits = 0;
        Broadcast();
    }

    public void ApplyHit(int amount = 1)
    {
        currentHits += amount;
        currentHits = Mathf.Clamp(currentHits, 0, maxHits);
        Debug.Log($"[PlayerHealth] Hit! {currentHits}/{maxHits}");
        Broadcast();

        if (currentHits >= maxHits)
        {
            Die();
        }
    }

    void Broadcast()
    {
        OnHitChanged?.Invoke(currentHits, maxHits);
    }

    void Die()
    {
        Debug.Log("[PlayerHealth] Player died!");
        // Fallback-safe notification to InGameManager if it exists (doesn't require method presence)
        var gm = FindObjectOfType<EnhancedInGameManager>();
        if (gm != null)
        {
            // SendMessage digunakan supaya tidak error bila InGameManager tidak punya OnPlayerDeath()
            gm.gameObject.SendMessage("OnPlayerDeath", SendMessageOptions.DontRequireReceiver);
        }

        // Default fallback behavior: disable movement and show debug
        var mover = GetComponent<PlayerLaneMovement>();
        if (mover != null) mover.enabled = false;

        // you can add VFX / disable sprite / show UI here
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }

    // optional helper to heal or reset
    public void ResetHealth()
    {
        currentHits = 0;
        Broadcast();
    }

    public int GetHits() => currentHits;
}
