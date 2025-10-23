using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tempel script ini pada SETIAP crate image di Inspector.
/// Assign crateIndex sesuai urutan (0, 1, 2, 3 untuk 4 crates).
/// </summary>
[RequireComponent(typeof(Image))]
public class CrateButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Setup")]
    [Tooltip("Index crate ini (0-based). Crate pertama = 0, kedua = 1, dst.")]
    public int crateIndex = 0;

    [Header("References (auto-find jika null)")]
    public QuestChestController chestController;

    void Start()
    {
        // Auto-find QuestChestController jika belum di-assign
        if (chestController == null)
        {
            chestController = FindObjectOfType<QuestChestController>();
        }

        if (chestController == null)
        {
            Debug.LogError($"[CrateButton] QuestChestController not found! Assign manually or ensure it exists in scene.");
        }
    }

    /// <summary>
    /// Dipanggil saat crate di-klik (Unity Event System)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (chestController != null)
        {
            Debug.Log($"[CrateButton] Crate {crateIndex} clicked!");
            chestController.TryClaimCrate(crateIndex);
        }
        else
        {
            Debug.LogError($"[CrateButton] Cannot claim crate {crateIndex}: chestController is null!");
        }
    }

    /// <summary>
    /// Alternative: call this from Button.onClick if you prefer
    /// </summary>
    public void OnClicked()
    {
        if (chestController != null)
        {
            Debug.Log($"[CrateButton] Crate {crateIndex} clicked!");
            chestController.TryClaimCrate(crateIndex);
        }
    }
}