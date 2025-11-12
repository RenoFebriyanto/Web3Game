// Assets/Script/Movement/FragmentCollectible.cs
using UnityEngine;

/// <summary>
/// UPDATED: Fragment pickup dengan sound
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

        // ✅ NEW: Play fragment pickup sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFragmentPickup();
        }

        var missionUI = FindFirstObjectByType<FragmentMissionUI>();
        if (missionUI != null)
        {
            missionUI.OnFragmentCollected(fragmentType, colorVariant);
        }

        Destroy(gameObject);
    }
}