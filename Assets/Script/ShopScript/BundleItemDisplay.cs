using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display component untuk satu item dalam bundle.
/// Prefab structure: Root (border) -> Icon (Image) -> Count (TMP_Text)
/// </summary>
public class BundleItemDisplay : MonoBehaviour
{
    public Image iconImage;      // Icon item
    public TMP_Text countText;   // Text "x2", "x3", etc

    public void Setup(Sprite icon, int amount)
    {
        if (iconImage != null)
            iconImage.sprite = icon;

        if (countText != null)
            countText.text = "x" + amount.ToString();
    }
}