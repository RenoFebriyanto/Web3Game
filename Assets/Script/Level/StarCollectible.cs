using UnityEngine;

/// <summary>
/// ✅ UPDATED: Star pickup dengan simple popup animation
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StarCollectible : MonoBehaviour
{
    [Header("Settings")]
    public int starValue = 1;
    public GameObject collectEffect;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ✅ NEW: Trigger popup animation
        if (CollectibleAnimationManager.Instance != null)
        {
            CollectibleAnimationManager.Instance.AnimateStarCollect(transform.position);
        }

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStarPickup();
        }

        // Update star manager
        var starManager = FindFirstObjectByType<GameplayStarManager>();
        if (starManager != null)
        {
            starManager.CollectStar(starValue);
        }
        else
        {
            Debug.LogWarning("[StarCollectible] GameplayStarManager not found in scene!");
        }

        // Spawn collect effect (if assigned)
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Destroy star object
        Destroy(gameObject);
    }
}