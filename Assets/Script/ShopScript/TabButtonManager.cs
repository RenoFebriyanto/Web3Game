using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TabButtonManager - UPDATED with Auto Scroll
/// Version: 5.0 - Auto Scroll Integration
/// </summary>
public class TabButtonManager : MonoBehaviour
{
    [Header("⭐ 4 Filter Buttons")]
    public Button allButton;
    public Button shardButton;
    public Button itemsButton;
    public Button bundleButton;

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

        // Setup button listeners
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
        
        Debug.Log("[TabButtonManager] ✓ Initialized with auto-scroll");
    }

    void OnTabClicked(Button clickedButton, string tab)
    {
        SetActiveButton(clickedButton);

        // ✅ Call ShopManager filter methods (sudah include ScrollToTop())
        switch (tab)
        {
            case "All":
                shopManager.ShowAll(); // ✅ Auto scroll
                break;
            case "Shard":
                shopManager.ShowShard(); // ✅ Auto scroll
                break;
            case "Items":
                shopManager.ShowItems(); // ✅ Auto scroll
                break;
            case "Bundle":
                shopManager.ShowBundle(); // ✅ Auto scroll
                break;
        }

        Debug.Log($"[TabButtonManager] ✓ Switched to filter: {tab} (with auto-scroll)");
    }

    void SetActiveButton(Button activeButton)
    {
        SetButtonState(allButton, false);
        SetButtonState(shardButton, false);
        SetButtonState(itemsButton, false);
        SetButtonState(bundleButton, false);

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

        var text = btn.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.color = active ? activeColor : inactiveColor;
        }
    }
    
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
