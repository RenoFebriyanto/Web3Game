using UnityEngine;

/// <summary>
/// Bintang yang bisa dikumpulkan player di gameplay.
/// Tempel script ini pada prefab Star.
/// Tag prefab sebagai "Collectible" atau "Star".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StarCollectible : MonoBehaviour
{
    [Header("Settings")]
    public int starValue = 1; // 1 bintang
    public GameObject collectEffect; // Optional VFX saat collected

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Notify star manager
        var starManager = FindFirstObjectByType<GameplayStarManager>();
        if (starManager != null)
        {
            starManager.CollectStar(starValue);
        }
        else
        {
            Debug.LogWarning("[StarCollectible] GameplayStarManager not found in scene!");
        }

        // Optional: spawn collect effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Destroy star
        Destroy(gameObject);
    }
}