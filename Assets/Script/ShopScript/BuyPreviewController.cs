// BuyPreviewController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuyPreviewController : MonoBehaviour
{
    [Header("UI refs (assign in Inspector)")]
    public GameObject rootPanel;        // root panel yang di-Enable/Disable
    public Image blurOverlay;           // full-screen blur image (Image)
    public RectTransform previewPanel;  // panel popup (RectTransform)

    public Image iconPreviewImage;
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public TMP_Text rewardAmountText;

    // groups in preview that include icon + price text (so they can be hidden)
    public GameObject coinPriceGroup;   // e.g. BuyConfirm/CoinPrice
    public GameObject shardPriceGroup;  // e.g. BuyConfirm/ShardPrice

    public Button buyWithCoinsBtn;
    public Button buyWithShardsBtn;
    public TMP_Text buyWithCoinsBtnText;
    public TMP_Text buyWithShardsBtnText;
    public Button closeBtn;             // optional: close X button on preview

    ShopManager manager;
    ShopItemData currentData;

    void Awake()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(false);

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(Close);
        }

        if (blurOverlay != null)
        {
            var b = blurOverlay.GetComponent<Button>();
            if (b == null)
            {
                b = blurOverlay.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
            }
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(Close);
        }
    }

    public void Initialize(ShopManager mgr)
    {
        manager = mgr;
    }

    public void Show(ShopItemData data)
    {
        currentData = data;
        if (data == null) return;

        if (rootPanel != null) rootPanel.SetActive(true);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(true);

        // icon: prefer iconPreview, fallback to iconGrid
        if (iconPreviewImage != null) iconPreviewImage.sprite = data.iconPreview != null ? data.iconPreview : data.iconGrid;
        if (titleText != null) titleText.text = data.displayName ?? "";
        if (descText != null) descText.text = data.description ?? "";
        if (rewardAmountText != null) rewardAmountText.text = data.rewardAmount.ToString();

        if (coinPriceText != null) coinPriceText.text = data.coinPrice > 0 ? data.coinPrice.ToString("N0") : "";
        if (shardPriceText != null) shardPriceText.text = data.shardPrice > 0 ? data.shardPrice.ToString("N0") : "";

        // show/hide the price groups (icon+text) in the preview
        if (coinPriceGroup != null) coinPriceGroup.SetActive(data.allowBuyWithCoins && data.coinPrice > 0);
        if (shardPriceGroup != null) shardPriceGroup.SetActive(data.allowBuyWithShards && data.shardPrice > 0);

        // BUTTONS: remove previous listeners then add fresh ones that capture 'data'
        if (buyWithCoinsBtn != null)
        {
            buyWithCoinsBtn.onClick.RemoveAllListeners();
            buyWithCoinsBtn.gameObject.SetActive(data.allowBuyWithCoins && data.coinPrice > 0);
            if (data.allowBuyWithCoins && data.coinPrice > 0)
            {
                buyWithCoinsBtn.onClick.AddListener(() =>
                {
                    if (manager != null)
                    {
                        manager.TryBuy(data, Currency.Coins);
                    }
                });
            }
        }

        if (buyWithShardsBtn != null)
        {
            buyWithShardsBtn.onClick.RemoveAllListeners();
            buyWithShardsBtn.gameObject.SetActive(data.allowBuyWithShards && data.shardPrice > 0);
            if (data.allowBuyWithShards && data.shardPrice > 0)
            {
                buyWithShardsBtn.onClick.AddListener(() =>
                {
                    if (manager != null)
                    {
                        manager.TryBuy(data, Currency.Shards);
                    }
                });
            }
        }
    }

    public void Close()
    {
        currentData = null;
        if (rootPanel != null) rootPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(false);

        if (buyWithCoinsBtn != null) buyWithCoinsBtn.onClick.RemoveAllListeners();
        if (buyWithShardsBtn != null) buyWithShardsBtn.onClick.RemoveAllListeners();
    }
}
