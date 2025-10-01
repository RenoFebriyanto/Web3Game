// Assets/Script/Movement/FragmentCollectible.cs
using UnityEngine;

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

        // Fix: ganti FindObjectOfType dengan FindFirstObjectByType
        var missionUI = FindFirstObjectByType<FragmentMissionUI>();
        if (missionUI != null)
        {
            missionUI.OnFragmentCollected(fragmentType, colorVariant);
        }

        Destroy(gameObject);
    }
}