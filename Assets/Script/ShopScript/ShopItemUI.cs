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

        // ✅ FIX: Priority - Kulino Coin > Shard > Coin
        bool hasKulinoCoin = data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0;
        bool hasShard = data.allowBuyWithShards && data.shardPrice > 0;
        bool hasCoin = data.allowBuyWithCoins && data.coinPrice > 0;

        // Tentukan mana yang harus ditampilkan (prioritas: KC > Shard > Coin)
        bool showKulinoCoin = hasKulinoCoin;
        bool showShard = !showKulinoCoin && hasShard;
        bool showCoin = !showKulinoCoin && !showShard && hasCoin;

        Debug.Log($"[ShopItemUI] {data.displayName} -> KC:{showKulinoCoin} Shard:{showShard} Coin:{showCoin}");

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

        Debug.Log($"[ShopItemUI] {data.displayName} - KC:{showKulinoCoin} Shard:{showShard} Coin:{showCoin}");

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