using UnityEngine;

/// <summary>
/// UPDATED: Added star collect sound
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

        // ✅ AUDIO: Play star collect sound
        SoundManager.StarCollect();

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

        Destroy(gameObject);
    }
}