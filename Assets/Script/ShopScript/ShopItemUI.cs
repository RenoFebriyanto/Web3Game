// ShopItemUI.cs - UPDATED untuk display harga Rupiah
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI refs")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public TMP_Text kulinoCoinPriceText;
    public Button buyButton;

    [Header("Root objects")]
    public GameObject coinIconRoot;
    public GameObject shardIconRoot;
    public GameObject kulinoCoinIconRoot;

    ShopItemData currentData;
    ShopManager manager;

    public void Setup(ShopItemData data, ShopManager shopManager)
    {
        currentData = data;
        manager = shopManager;

        if (data == null) return;

        // Set icon
        if (iconImage != null) iconImage.sprite = data.iconGrid;

        // ✅ SMART TITLE
        if (nameText != null)
        {
            string displayTitle = GetSmartTitle(data);
            nameText.text = displayTitle;
        }

        // ✅ CRITICAL: Check if item uses Rupiah pricing (SHARD items)
        if (data.UseRupiahPricing)
        {
            SetupRupiahPricing(data);
        }
        else
        {
            SetupRegularPricing(data);
        }

        // Buy button
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                manager?.ShowBuyPreview(currentData, this);
            });
        }
    }

    /// <summary>
    /// ✅ NEW: Setup untuk Shard items dengan harga Rupiah
    /// </summary>
    void SetupRupiahPricing(ShopItemData data)
    {
        Debug.Log($"[ShopItemUI] {data.displayName} uses RUPIAH pricing: Rp {data.rupiahPrice:N0}");

        // ✅ Tampilkan harga Rupiah di KC price text
        if (kulinoCoinPriceText != null)
        {
            kulinoCoinPriceText.text = $"Rp {data.rupiahPrice:N0}";
        }

        // ✅ Show ONLY KC icon (untuk Rupiah display)
        if (coinIconRoot != null) coinIconRoot.SetActive(false);
        if (shardIconRoot != null) shardIconRoot.SetActive(false);
        if (kulinoCoinIconRoot != null) kulinoCoinIconRoot.SetActive(true);

        Debug.Log($"[ShopItemUI] {data.displayName} - Showing Rupiah price: Rp {data.rupiahPrice:N0}");
    }

    /// <summary>
    /// ✅ Setup untuk items lain (Coin, Energy, Booster, Bundle)
    /// </summary>
    void SetupRegularPricing(ShopItemData data)
    {
        bool hasKulinoCoin = data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0;
        bool hasShard = data.allowBuyWithShards && data.shardPrice > 0;
        bool hasCoin = data.allowBuyWithCoins && data.coinPrice > 0;

        bool isKulinoCoinOnly = hasKulinoCoin && !hasShard && !hasCoin;

        bool showKulinoCoin = hasKulinoCoin;
        bool showShard = hasShard && !isKulinoCoinOnly;
        bool showCoin = hasCoin && !isKulinoCoinOnly;

        // Update price texts
        if (coinPriceText != null)
            coinPriceText.text = showCoin ? data.coinPrice.ToString("N0") : "";

        if (shardPriceText != null)
            shardPriceText.text = showShard ? data.shardPrice.ToString("N0") : "";

        if (kulinoCoinPriceText != null)
            kulinoCoinPriceText.text = showKulinoCoin ? $"{data.kulinoCoinPrice:F6} KC" : "";

        // Show/hide icon groups
        if (coinIconRoot != null) coinIconRoot.SetActive(showCoin);
        if (shardIconRoot != null) shardIconRoot.SetActive(showShard);
        if (kulinoCoinIconRoot != null) kulinoCoinIconRoot.SetActive(showKulinoCoin);
    }

    string GetSmartTitle(ShopItemData data)
    {
        if (data.rewardType == ShopRewardType.Coin ||
            data.rewardType == ShopRewardType.Shard ||
            data.rewardType == ShopRewardType.Energy)
        {
            if (data.rewardAmount > 0)
            {
                return data.rewardAmount.ToString("N0");
            }
        }

        return data.displayName;
    }
}