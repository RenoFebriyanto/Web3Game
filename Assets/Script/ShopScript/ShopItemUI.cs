using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UPDATED ShopItemUI - Displays amount as title for currency items
/// Version: 2.0 - Smart Title Display
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public TMP_Text kulinoCoinPriceText;
    public Button buyButton;

    [Header("Optional: root objects for layout")]
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

        // ✅ SMART TITLE: Display amount untuk currency items (Coin, Shard, Energy)
        if (nameText != null)
        {
            string displayTitle = GetSmartTitle(data);
            nameText.text = displayTitle;
            Debug.Log($"[ShopItemUI] {data.itemId} title set to: {displayTitle}");
        }

        // ✅ Show ALL enabled payment methods
        bool hasKulinoCoin = data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0;
        bool hasShard = data.allowBuyWithShards && data.shardPrice > 0;
        bool hasCoin = data.allowBuyWithCoins && data.coinPrice > 0;

        bool isKulinoCoinOnly = hasKulinoCoin && !hasShard && !hasCoin;

        bool showKulinoCoin = hasKulinoCoin;
        bool showShard = hasShard && !isKulinoCoinOnly;
        bool showCoin = hasCoin && !isKulinoCoinOnly;

        Debug.Log($"[ShopItemUI] {data.displayName} Setup:");
        Debug.Log($"  - Coin: allow={data.allowBuyWithCoins}, price={data.coinPrice}, show={showCoin}");
        Debug.Log($"  - Shard: allow={data.allowBuyWithShards}, price={data.shardPrice}, show={showShard}");
        Debug.Log($"  - KC: allow={data.allowBuyWithKulinoCoin}, price={data.kulinoCoinPrice}, show={showKulinoCoin}");

        // Update price texts
        if (coinPriceText != null)
            coinPriceText.text = showCoin ? data.coinPrice.ToString("N0") : "";

        if (shardPriceText != null)
            shardPriceText.text = showShard ? data.shardPrice.ToString("N0") : "";

        if (kulinoCoinPriceText != null)
            kulinoCoinPriceText.text = showKulinoCoin ? $"{data.kulinoCoinPrice:F6} KC" : "";

        // Show/hide icon groups
        if (coinIconRoot != null) 
        {
            coinIconRoot.SetActive(showCoin);
            Debug.Log($"  - Coin Icon: {(showCoin ? "VISIBLE" : "HIDDEN")}");
        }
        
        if (shardIconRoot != null) 
        {
            shardIconRoot.SetActive(showShard);
            Debug.Log($"  - Shard Icon: {(showShard ? "VISIBLE" : "HIDDEN")}");
        }
        
        if (kulinoCoinIconRoot != null) 
        {
            kulinoCoinIconRoot.SetActive(showKulinoCoin);
            Debug.Log($"  - KC Icon: {(showKulinoCoin ? "VISIBLE" : "HIDDEN")}");
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
    /// ✅ NEW: Smart title - Show amount for currency items, original name for others
    /// </summary>
    string GetSmartTitle(ShopItemData data)
    {
        // Untuk currency items (Coin, Shard, Energy) - tampilkan nilai
        if (data.rewardType == ShopRewardType.Coin ||
            data.rewardType == ShopRewardType.Shard ||
            data.rewardType == ShopRewardType.Energy)
        {
            if (data.rewardAmount > 0)
            {
                return data.rewardAmount.ToString("N0"); // Contoh: "100" atau "1,000"
            }
        }

        // Untuk Booster dan Bundle - gunakan displayName
        return data.displayName;
    }
}