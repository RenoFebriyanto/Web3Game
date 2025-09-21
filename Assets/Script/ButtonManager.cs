using UnityEngine;

/// <summary>
/// Simple button manager for switching side panels (Level / Quest / Shop).
/// Attach this script to a manager GameObject and wire Level/Quest/Shop in the Inspector.
/// Hook each UI Button's OnClick() to the corresponding ShowX method (ShowLevel/ShowQuest/ShowShop).
/// </summary>
public class ButtonManager : MonoBehaviour
{
    [Header("Panels (assign in Inspector)")]
    public GameObject Level;
    public GameObject Quest;
    public GameObject Shop;

    void Start()
    {
        // Default: show Level, hide others (adjust if you want a different default)
        SetActiveSafe(Level, true);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, false);
    }

    // Small helper to avoid null checks repetition
    void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // Public methods for UI Buttons (call these from Button OnClick)
    public void ShowLevel()
    {
        // if already active, do nothing (prevents flicker)
        if (Level != null && Level.activeSelf) return;

        SetActiveSafe(Level, true);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, false);
    }

    public void ShowQuest()
    {
        if (Quest != null && Quest.activeSelf) return;

        SetActiveSafe(Level, false);
        SetActiveSafe(Quest, true);
        SetActiveSafe(Shop, false);
    }

    public void ShowShop()
    {
        if (Shop != null && Shop.activeSelf) return;

        SetActiveSafe(Level, false);
        SetActiveSafe(Quest, false);
        SetActiveSafe(Shop, true);
    }

    // optional: keyboard shortcuts for quick testing in Editor
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowLevel();
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowQuest();
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowShop();
    }
}
