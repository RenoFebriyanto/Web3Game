// Assets/Script/Movement/FragmentCollectible.cs
using UnityEngine;

public class FragmentCollectible : MonoBehaviour
{
    [HideInInspector] public FragmentType fragmentType;
    [HideInInspector] public int colorVariant;

    // Method ini dipanggil saat spawner create fragment
    public void Initialize(FragmentType type, int variant)
    {
        fragmentType = type;
        colorVariant = variant;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var missionUI = FindObjectOfType<FragmentMissionUI>();
        if (missionUI != null)
        {
            missionUI.OnFragmentCollected(fragmentType, colorVariant);
        }

        Destroy(gameObject);
    }
}