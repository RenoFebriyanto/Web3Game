// BuyPreviewUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BuyPreviewController : MonoBehaviour
{
    [Header("UI refs")]
    public GameObject rootPanel;      // panel utama yang di-enable/disable
    public Image blurOverlay;        // full-screen image used as blur (assign blur image or semi-transparent black)
    public Image iconPreviewImage;   // large icon in preview
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;

    public Button buyWithCoinsBtn;
    public Button buyWithShardsBtn;
    public TMP_Text buyWithCoinsBtnText;
    public TMP_Text buyWithShardsBtnText;

    ShopManager manager;
    ShopItemData currentData;

    void Awake()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(false);
    }

    public void Initialize(ShopManager mgr)
    {
        manager = mgr;
        if (buyWithCoinsBtn != null)
        {
            buyWithCoinsBtn.onClick.RemoveAllListeners();
            buyWithCoinsBtn.onClick.AddListener(() => manager.TryBuy(currentData, Currency.Coins));
        }
        if (buyWithShardsBtn != null)
        {
            buyWithShardsBtn.onClick.RemoveAllListeners();
            buyWithShardsBtn.onClick.AddListener(() => manager.TryBuy(currentData, Currency.Shards));
        }
    }

    public void Show(ShopItemData data)
    {
        currentData = data;
        if (data == null) return;

        if (rootPanel != null) rootPanel.SetActive(true);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(true);

        // icon: prefer preview, fallback to grid
        if (iconPreviewImage != null) iconPreviewImage.sprite = data.iconPreview != null ? data.iconPreview : data.iconGrid;
        if (titleText != null) titleText.text = data.displayName;
        if (descText != null) descText.text = data.description ?? "";

        if (coinPriceText != null) coinPriceText.text = data.coinPrice > 0 ? data.coinPrice.ToString("N0") : "";
        if (shardPriceText != null) shardPriceText.text = data.shardPrice > 0 ? data.shardPrice.ToString("N0") : "";

        // show/hide buy buttons depending on allowances
        if (buyWithCoinsBtn != null) buyWithCoinsBtn.gameObject.SetActive(data.allowBuyWithCoins && data.coinPrice > 0);
        if (buyWithShardsBtn != null) buyWithShardsBtn.gameObject.SetActive(data.allowBuyWithShards && data.shardPrice > 0);
    }

    public void Close()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(false);
        currentData = null;
    }
}
