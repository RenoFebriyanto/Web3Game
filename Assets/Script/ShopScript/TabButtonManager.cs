using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TabButtonManager - 4 Filters Only
/// ALL | SHARD | ITEMS | BUNDLE
/// Version: 4.0 - Final Version
/// </summary>
public class TabButtonManager : MonoBehaviour
{
    [Header("⭐ 4 Filter Buttons (Final)")]
    public Button allButton;      // ALL - Show semua category
    public Button shardButton;    // SHARD - Hanya category Shard
    public Button itemsButton;    // ITEMS - Coin + Energy + Booster
    public Button bundleButton;   // BUNDLE - Hanya category Bundle

    [Header("Active Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);

    ShopManager shopManager;

    void Start()
    {
        shopManager = FindFirstObjectByType<ShopManager>();

        if (shopManager == null)
        {
            Debug.LogError("[TabButtonManager] ShopManager not found!");
            return;
        }

        // ✅ Setup 4 button listeners
        if (allButton != null)
        {
            allButton.onClick.AddListener(() => OnTabClicked(allButton, "All"));
        }

        if (shardButton != null)
        {
            shardButton.onClick.AddListener(() => OnTabClicked(shardButton, "Shard"));
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
        
        Debug.Log("[TabButtonManager] ✓ Initialized with 4 filter buttons");
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
            case "Shard":
                shopManager.ShowShard();
                break;
            case "Items":
                shopManager.ShowItems();
                break;
            case "Bundle":
                shopManager.ShowBundle();
                break;
        }

        Debug.Log($"[TabButtonManager] ✓ Switched to filter: {tab}");
    }

    void SetActiveButton(Button activeButton)
    {
        // Reset all 4 buttons
        SetButtonState(allButton, false);
        SetButtonState(shardButton, false);
        SetButtonState(itemsButton, false);
        SetButtonState(bundleButton, false);

        // Set active button
        SetButtonState(activeButton, true);
    }

    void SetButtonState(Button btn, bool active)
    {
        if (btn == null) return;

        // Change button image color
        var graphic = btn.GetComponent<Image>();
        if (graphic != null)
        {
            graphic.color = active ? activeColor : inactiveColor;
        }

        // Change button text color
        var text = btn.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.color = active ? activeColor : inactiveColor;
        }
    }
    
    // ==================== PUBLIC API ====================
    
    /// <summary>
    /// Manually set active filter (untuk call dari code)
    /// </summary>
    public void SetFilter(string filterName)
    {
        Button targetButton = filterName.ToLower() switch
        {
            "all" => allButton,
            "shard" => shardButton,
            "items" => itemsButton,
            "bundle" => bundleButton,
            _ => allButton
        };
        
        OnTabClicked(targetButton, filterName);
    }
}