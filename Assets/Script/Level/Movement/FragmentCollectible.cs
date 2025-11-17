using UnityEngine;

/// <summary>
/// ✅ UPDATED: Fragment pickup dengan animation system
/// </summary>
public class FragmentCollectible : MonoBehaviour
{
    [HideInInspector] public FragmentType fragmentType;
    [HideInInspector] public int colorVariant;

    public void Initialize(FragmentType type, int variant)
    {
        fragmentType = type;
        colorVariant = variant;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ✅ NEW: Get sprite untuk animation
        Sprite fragmentSprite = GetFragmentSprite();

        // ✅ NEW: Find mission box index
        int missionBoxIndex = FindMissionBoxIndex();

        // ✅ NEW: Trigger animation
        if (CollectibleAnimationManager.Instance != null && fragmentSprite != null)
        {
            CollectibleAnimationManager.Instance.AnimateFragmentCollect(
                transform.position,
                fragmentSprite,
                missionBoxIndex
            );
        }

        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFragmentPickup();
        }

        // Update mission progress
        var missionUI = FindFirstObjectByType<FragmentMissionUI>();
        if (missionUI != null)
        {
            missionUI.OnFragmentCollected(fragmentType, colorVariant);
        }

        // Destroy fragment object
        Destroy(gameObject);
    }

    Sprite GetFragmentSprite()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return sr.sprite;
        }

        return null;
    }

    int FindMissionBoxIndex()
    {
        var missionUI = FindFirstObjectByType<FragmentMissionUI>();
        if (missionUI == null) return 0;

        // Find matching requirement index
        if (missionUI.missionBoxes != null)
        {
            for (int i = 0; i < missionUI.missionBoxes.Count; i++)
            {
                // Check if this fragment matches requirement
                // (This is simplified - adjust based on your FragmentMissionUI implementation)
                return i; // Return first available box for now
            }
        }

        return 0;
    }
}