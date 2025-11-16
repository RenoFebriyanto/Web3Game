// ShopItemUI.cs - FIXED VERSION
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public TMP_Text kulinoCoinPriceText; // ✅ NEW
    public Button buyButton;

    [Header("Optional: root objects for layout")]
    public GameObject coinIconRoot;
    public GameObject shardIconRoot;
    public GameObject kulinoCoinIconRoot; // ✅ NEW

    ShopItemData currentData;
    ShopManager manager;

    public void Setup(ShopItemData data, ShopManager shopManager)
    {
        currentData = data;
        manager = shopManager;

        if (data == null) return;

        // Set icon & name
        if (iconImage != null) iconImage.sprite = data.iconGrid;
        if (nameText != null) nameText.text = data.displayName;

        // ✅ LOGIC BARU: Prioritas Kulino Coin > Shard > Coin
        bool showKulinoCoin = data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0;
        bool showShard = !showKulinoCoin && data.allowBuyWithShards && data.shardPrice > 0;
        bool showCoin = !showKulinoCoin && !showShard && data.allowBuyWithCoins && data.coinPrice > 0;

        // Update price texts
        if (coinPriceText != null) 
            coinPriceText.text = showCoin ? data.coinPrice.ToString("N0") : "";
        
        if (shardPriceText != null) 
            shardPriceText.text = showShard ? data.shardPrice.ToString("N0") : "";
        
        if (kulinoCoinPriceText != null) 
            kulinoCoinPriceText.text = showKulinoCoin ? data.kulinoCoinPrice.ToString("F6") + " KC" : "";

        // Show/hide icon groups
        if (coinIconRoot != null) coinIconRoot.SetActive(showCoin);
        if (shardIconRoot != null) shardIconRoot.SetActive(showShard);
        if (kulinoCoinIconRoot != null) kulinoCoinIconRoot.SetActive(showKulinoCoin);

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
}