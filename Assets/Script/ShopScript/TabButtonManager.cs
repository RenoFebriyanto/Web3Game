using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages tab button visual states (optional)
/// </summary>
public class TabButtonManager : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button allButton;
    public Button itemsButton;
    public Button bundleButton;

    [Header("Active Colors (optional)")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);

    ShopManager shopManager;

    void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();

        if (shopManager == null)
        {
            Debug.LogError("[TabButtonManager] ShopManager not found!");
            return;
        }

        // Setup button listeners
        if (allButton != null)
        {
            allButton.onClick.AddListener(() => OnTabClicked(allButton, "All"));
        }

        if (itemsButton != null)
        {
            itemsButton.onClick.AddListener(() => OnTabClicked(itemsButton, "Items"));
        }

        if (bundleButton != null)
        {
            bundleButton.onClick.AddListener(() => OnTabClicked(bundleButton, "Bundle"));
        }

        // Set ALL as active by default
        SetActiveButton(allButton);
    }

    void OnTabClicked(Button clickedButton, string tab)
    {
        SetActiveButton(clickedButton);

        // Call ShopManager filter methods
        switch (tab)
        {
            case "All":
                shopManager.ShowAll();
                break;
            case "Items":
                shopManager.ShowItems();
                break;
            case "Bundle":
                shopManager.ShowBundle();
                break;
        }
    }

    void SetActiveButton(Button activeButton)
    {
        // Reset all buttons
        SetButtonState(allButton, false);
        SetButtonState(itemsButton, false);
        SetButtonState(bundleButton, false);

        // Set active button
        SetButtonState(activeButton, true);
    }

    void SetButtonState(Button btn, bool active)
    {
        if (btn == null) return;

        var graphic = btn.GetComponent<Image>();
        if (graphic != null)
        {
            graphic.color = active ? activeColor : inactiveColor;
        }

        // Optional: juga ubah text color
        var text = btn.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.color = active ? activeColor : inactiveColor;
        }
    }
}