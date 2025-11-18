using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UPDATED TabButtonManager dengan filter Coin, Shard, Energy, Booster, Bundle
/// Version: 2.0 - Category Filters
/// </summary>
public class TabButtonManager : MonoBehaviour
{
    [Header("🆕 Category Tab Buttons")]
    public Button allButton;
    public Button coinButton;
    public Button shardButton;
    public Button energyButton;
    public Button boosterButton;
    public Button bundleButton;

    [Header("Active Colors (optional)")]
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

        // ✅ Setup button listeners untuk semua filter
        if (allButton != null)
        {
            allButton.onClick.AddListener(() => OnTabClicked(allButton, "All"));
        }

        if (coinButton != null)
        {
            coinButton.onClick.AddListener(() => OnTabClicked(coinButton, "Coin"));
        }

        if (shardButton != null)
        {
            shardButton.onClick.AddListener(() => OnTabClicked(shardButton, "Shard"));
        }

        if (energyButton != null)
        {
            energyButton.onClick.AddListener(() => OnTabClicked(energyButton, "Energy"));
        }

        if (boosterButton != null)
        {
            boosterButton.onClick.AddListener(() => OnTabClicked(boosterButton, "Booster"));
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
            case "Coin":
                shopManager.ShowCoin();
                break;
            case "Shard":
                shopManager.ShowShard();
                break;
            case "Energy":
                shopManager.ShowEnergy();
                break;
            case "Booster":
                shopManager.ShowBooster();
                break;
            case "Bundle":
                shopManager.ShowBundle();
                break;
        }

        Debug.Log($"[TabButtonManager] Switched to: {tab}");
    }

    void SetActiveButton(Button activeButton)
    {
        // Reset all buttons
        SetButtonState(allButton, false);
        SetButtonState(coinButton, false);
        SetButtonState(shardButton, false);
        SetButtonState(energyButton, false);
        SetButtonState(boosterButton, false);
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