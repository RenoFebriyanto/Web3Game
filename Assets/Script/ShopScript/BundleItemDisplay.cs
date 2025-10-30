using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display untuk satu item (single atau bundle).
/// FIXED: Support multiple hierarchy structures untuk icon
/// </summary>
public class BundleItemDisplay : MonoBehaviour
{
    [Header("Assign CHILD components (optional - will auto-find if null)")]
    public Image iconImage;      // CHILD image untuk icon (bukan background border)
    public TMP_Text countText;   // Text untuk count

    public void Setup(Sprite icon, int amount)
    {
        Debug.Log($"[BundleItemDisplay] Setup called: icon={(icon != null ? icon.name : "NULL")}, amount={amount}");

        // Auto-find iconImage if not assigned
        if (iconImage == null)
        {
            // Try find child named "Icons"
            Transform iconsTransform = transform.Find("Icons");
            if (iconsTransform != null)
            {
                iconImage = iconsTransform.GetComponent<Image>();
                Debug.Log($"[BundleItemDisplay] Found iconImage via Find('Icons'): {(iconImage != null ? "SUCCESS" : "FAILED")}");
            }

            // If still null, try GetComponentInChildren
            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>(true);
                Debug.Log($"[BundleItemDisplay] Found iconImage via GetComponentInChildren: {(iconImage != null ? iconImage.gameObject.name : "FAILED")}");
            }
        }

        // Set icon
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
            Debug.Log($"[BundleItemDisplay] ✓ Set icon: {icon.name} on {iconImage.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[BundleItemDisplay] ✗ Cannot set icon! iconImage={(iconImage != null ? iconImage.gameObject.name : "NULL")}, icon={(icon != null ? icon.name : "NULL")}");

            // Debug: print hierarchy
            Debug.Log($"[BundleItemDisplay] Hierarchy of {gameObject.name}:");
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var img = child.GetComponent<Image>();
                Debug.Log($"  Child {i}: {child.name} (Image: {(img != null ? "YES (" + (img.sprite != null ? img.sprite.name : "null sprite") + ")" : "NO")})");
            }
        }

        // Auto-find countText if not assigned
        if (countText == null)
        {
            // Try find child named "Amount"
            Transform amountTransform = transform.Find("Amount");
            if (amountTransform != null)
            {
                countText = amountTransform.GetComponent<TMP_Text>();
            }

            // If still null, try GetComponentInChildren
            if (countText == null)
            {
                countText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        // Set count text
        if (countText != null)
        {
            if (amount > 0)
            {
                countText.text = "x" + amount.ToString();
                countText.gameObject.SetActive(true);
                Debug.Log($"[BundleItemDisplay] ✓ Set count text: x{amount}");
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"[BundleItemDisplay] ✗ countText not found");
        }
    }
}