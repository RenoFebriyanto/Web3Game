// ShopItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Image iconImage;        // child image in backgroundItem (big icon)
    public TMP_Text nameText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public Button buyButton;       // assign child buy button here

    [Header("Optional: root objects that contain coin/shard icon + price (for layout)")]
    public GameObject coinIconRoot;   // e.g. PriceShop/Coin (gameobject root)
    public GameObject shardIconRoot;  // e.g. PriceShop/Shard (gameobject root)

    ShopItemData currentData;
    ShopManager manager;

    public void Setup(ShopItemData data, ShopManager shopManager)
    {
        currentData = data;
        manager = shopManager;

        if (data == null) return;

        if (iconImage != null) iconImage.sprite = data.iconGrid;
        if (nameText != null) nameText.text = data.displayName;

        // price texts
        if (coinPriceText != null) coinPriceText.text = data.coinPrice > 0 ? data.coinPrice.ToString("N0") : "";
        if (shardPriceText != null) shardPriceText.text = data.shardPrice > 0 ? data.shardPrice.ToString("N0") : "";

        // show/hide coin icon group depending on price / permission
        if (coinIconRoot != null)
        {
            bool showCoin = data.allowBuyWithCoins && data.coinPrice > 0;
            coinIconRoot.SetActive(showCoin);
        }

        // show/hide shard icon group depending on price / permission
        if (shardIconRoot != null)
        {
            bool showShard = data.allowBuyWithShards && data.shardPrice > 0;
            shardIconRoot.SetActive(showShard);
        }

        // buy button opens preview (ShopManager handles the preview)
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
