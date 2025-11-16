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

        // Set icon & name
        if (iconImage != null) iconImage.sprite = data.iconGrid;
        if (nameText != null) nameText.text = data.displayName;

        // ✅ NEW LOGIC: Show ALL enabled payment methods
        bool hasKulinoCoin = data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0;
        bool hasShard = data.allowBuyWithShards && data.shardPrice > 0;
        bool hasCoin = data.allowBuyWithCoins && data.coinPrice > 0;

        // Jika item HANYA bisa dibeli dengan Kulino Coin, sembunyikan yang lain
        bool isKulinoCoinOnly = hasKulinoCoin && !hasShard && !hasCoin;

        // Tampilkan semua yang enabled, kecuali jika Kulino Coin only
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
}