using UnityEngine;

/// <summary>
/// UPDATED: Star collectible dengan sound
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

        // ✅ NEW: Play star pickup sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStarPickup();
        }

        var starManager = FindFirstObjectByType<GameplayStarManager>();
        if (starManager != null)
        {
            starManager.CollectStar(starValue);
        }
        else
        {
            Debug.LogWarning("[StarCollectible] GameplayStarManager not found in scene!");
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}