using UnityEngine;

/// <summary>
/// ✅ FIXED: Fragment pickup dengan proper mission box matching
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

        // Get sprite untuk animation
        Sprite fragmentSprite = GetFragmentSprite();

        // Find mission box index yang match dengan fragment ini
        int missionBoxIndex = FindMissionBoxIndex();

        // Trigger animation
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

    /// <summary>
    /// ✅ FIXED: Find mission box index yang match dengan fragment type & variant
    /// </summary>
    int FindMissionBoxIndex()
    {
        var missionUI = FindFirstObjectByType<FragmentMissionUI>();
        if (missionUI == null) 
        {
            Debug.LogWarning("[FragmentCollectible] FragmentMissionUI not found!");
            return 0;
        }

        // ✅ FIX: Cari box yang match dengan fragment type & variant
        if (LevelGameSession.Instance != null && LevelGameSession.Instance.currentLevel != null)
        {
            var requirements = LevelGameSession.Instance.currentLevel.requirements;
            
            if (requirements != null)
            {
                for (int i = 0; i < requirements.Count; i++)
                {
                    var req = requirements[i];
                    
                    // Match fragment type & variant dengan requirement
                    if (req.type == fragmentType && req.colorVariant == colorVariant)
                    {
                        Debug.Log($"[FragmentCollectible] ✓ Matched fragment {fragmentType} variant {colorVariant} to box {i}");
                        return i;
                    }
                }
            }
        }

        // Fallback: Return first box
        Debug.LogWarning($"[FragmentCollectible] No matching requirement for {fragmentType} variant {colorVariant}, using box 0");
        return 0;
    }
}