using UnityEngine;

/// <summary>
/// Helper untuk load fragment icons di preview
/// </summary>
public class FragmentPreviewManager : MonoBehaviour
{
    [Header("Fragment Icons - Manual Assignment")]
    [Tooltip("Assign fragment icons manually jika auto-load gagal")]
    public FragmentIconSet[] fragmentIcons;

    private static FragmentPreviewManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    /// <summary>
    /// Get fragment icon dari manual assignment
    /// </summary>
    public static Sprite GetFragmentIcon(FragmentType type, int variant)
    {
        if (instance == null || instance.fragmentIcons == null) return null;

        foreach (var iconSet in instance.fragmentIcons)
        {
            if (iconSet.type == type && iconSet.variant == variant)
            {
                return iconSet.icon;
            }
        }

        return null;
    }
}

[System.Serializable]
public class FragmentIconSet
{
    public FragmentType type;
    public int variant;
    public Sprite icon;
}