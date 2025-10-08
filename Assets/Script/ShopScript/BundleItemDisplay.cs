using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display untuk satu item (single atau bundle).
/// STRUCTURE PENTING:
///   Root (border background Image)
///     └─ Icons (child Image untuk icon item) ← INI yang di-assign ke iconImage
///     └─ Count (TMP_Text untuk "x2", "x3", dll)
/// </summary>
public class BundleItemDisplay : MonoBehaviour
{
    [Header("Assign CHILD components, bukan root!")]
    public Image iconImage;      // CHILD image untuk icon (bukan background border)
    public TMP_Text countText;   // Text untuk count

    public void Setup(Sprite icon, int amount)
    {
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
            Debug.Log($"[BundleItemDisplay] Set icon: {icon.name}");
        }
        else
        {
            Debug.LogWarning($"[BundleItemDisplay] iconImage={(iconImage != null ? "assigned" : "NULL")}, icon={(icon != null ? icon.name : "NULL")}");
        }

        if (countText != null)
        {
            if (amount > 0)
            {
                countText.text = "x" + amount.ToString();
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }
}