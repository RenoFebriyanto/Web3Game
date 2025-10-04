// ShopItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ShopItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Image iconImage;        // child image in backgroundItem
    public TMP_Text nameText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public Button buyButton;

    ShopItemData currentData;
    ShopManager manager;

    public void Setup(ShopItemData data, ShopManager shopManager)
    {
        currentData = data;
        manager = shopManager;

        if (data == null) return;

        if (iconImage != null) iconImage.sprite = data.iconGrid;
        if (nameText != null) nameText.text = data.displayName;

        if (coinPriceText != null)
            coinPriceText.text = data.coinPrice > 0 ? data.coinPrice.ToString("N0") : "";

        if (shardPriceText != null)
            shardPriceText.text = data.shardPrice > 0 ? data.shardPrice.ToString("N0") : "";

        // buy button opens preview (ShopManager handles the preview)
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => {
                manager.ShowBuyPreview(currentData, this);
            });
        }
    }
}
