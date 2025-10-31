using UnityEngine;

/// <summary>
/// Data class untuk menampilkan bundle items di PopupClaimQuest
/// Digunakan untuk passing data icon, amount, dan display name
/// </summary>
[System.Serializable]
public class BundleItemData
{
    public Sprite icon;
    public int amount;
    public string displayName;

    public BundleItemData(Sprite icon, int amount, string displayName)
    {
        this.icon = icon;
        this.amount = amount;
        this.displayName = displayName;
    }
}