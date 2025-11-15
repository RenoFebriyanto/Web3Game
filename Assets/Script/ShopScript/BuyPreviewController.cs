using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// FULL COMPLETE BuyPreviewController dengan Kulino Coin support
/// </summary>
public class BuyPreviewController : MonoBehaviour
{
    [Header("UI refs (assign in Inspector)")]
    public GameObject rootPanel;
    public Image blurOverlay;
    public RectTransform previewPanel;

    [Header("Icon Display")]
    public Transform iconItemsPreview;
    public GameObject singleItemTemplate;

    [Header("Single Item Display")]
    public Image iconPreviewImage;
    public TMP_Text rewardAmountText;

    [Header("Bundle Item Prefab")]
    public GameObject bundleItemPrefab;

    [Header("Info Display")]
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Price Display")]
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public TMP_Text kulinoCoinPriceText;
    public GameObject coinPriceGroup;
    public GameObject shardPriceGroup;
    public GameObject kulinoCoinPriceGroup;

    [Header("Buttons")]
    public Button buyWithCoinsBtn;
    public Button buyWithShardsBtn;
    public Button buyWithKulinoCoinBtn;
    public TMP_Text buyWithCoinsBtnText;
    public TMP_Text buyWithShardsBtnText;
    public TMP_Text buyWithKulinoCoinBtnText;
    public Button closeBtn;

    [Header("Scroll Settings")]
    public ScrollRect scrollRect;
    public int maxVisibleItems = 5;

    ShopManager manager;
    ShopItemData currentData;
    List<GameObject> spawnedBundleItems = new List<GameObject>();
    List<GameObject> existingChildren = new List<GameObject>();

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

        if (scrollRect == null && iconItemsPreview != null)
        {
            scrollRect = iconItemsPreview.GetComponentInParent<ScrollRect>();
        }

        if (iconItemsPreview != null)
        {
            for (int i = 0; i < iconItemsPreview.childCount; i++)
            {
                var child = iconItemsPreview.GetChild(i).gameObject;
                existingChildren.Add(child);
                Debug.Log($"[BuyPreview] Saved existing child: {child.name}");
            }

            if (singleItemTemplate == null && existingChildren.Count > 0)
            {
                singleItemTemplate = existingChildren[0];
                Debug.Log($"[BuyPreview] Auto-assigned singleItemTemplate: {singleItemTemplate.name}");
            }
        }

        Debug.Log($"[BuyPreview] Found {existingChildren.Count} existing children in IconItemsPreview");
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

        if (titleText != null) titleText.text = data.displayName ?? "";
        if (descText != null) descText.text = data.description ?? "";

        if (coinPriceText != null) coinPriceText.text = data.coinPrice > 0 ? data.coinPrice.ToString("N0") : "";
        if (shardPriceText != null) shardPriceText.text = data.shardPrice > 0 ? data.shardPrice.ToString("N0") : "";
        if (kulinoCoinPriceText != null) kulinoCoinPriceText.text = data.kulinoCoinPrice > 0 ? data.kulinoCoinPrice.ToString("F6") + " KC" : "";

        if (coinPriceGroup != null) coinPriceGroup.SetActive(data.allowBuyWithCoins && data.coinPrice > 0);
        if (shardPriceGroup != null) shardPriceGroup.SetActive(data.allowBuyWithShards && data.shardPrice > 0);
        if (kulinoCoinPriceGroup != null) kulinoCoinPriceGroup.SetActive(data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0);

        if (data.IsBundle)
        {
            ShowBundleIcons(data);
        }
        else
        {
            ShowSingleIcon(data);
        }

        SetupButtons(data);
    }

    void ShowSingleIcon(ShopItemData data)
    {
        Debug.Log("[BuyPreview] ShowSingleIcon called");

        ClearSpawnedBundleItems();

        if (iconItemsPreview != null)
        {
            iconItemsPreview.gameObject.SetActive(true);
        }

        if (iconPreviewImage != null) iconPreviewImage.gameObject.SetActive(false);
        if (rewardAmountText != null) rewardAmountText.gameObject.SetActive(false);

        for (int i = 0; i < existingChildren.Count; i++)
        {
            if (existingChildren[i] != null)
            {
                existingChildren[i].SetActive(i == 0);
            }
        }

        if (singleItemTemplate != null)
        {
            singleItemTemplate.SetActive(true);

            for (int i = 0; i < singleItemTemplate.transform.childCount; i++)
            {
                var child = singleItemTemplate.transform.GetChild(i).gameObject;
                child.SetActive(true);
                Debug.Log($"[BuyPreview] Activated child: {child.name}");
            }

            var display = singleItemTemplate.GetComponent<BundleItemDisplay>();
            if (display != null)
            {
                Sprite iconToUse = data.iconPreview != null ? data.iconPreview : data.iconGrid;
                display.Setup(iconToUse, data.rewardAmount);
                Debug.Log($"[BuyPreview] Updated single item: icon={iconToUse?.name}, amount={data.rewardAmount}");
            }
            else
            {
                Debug.LogWarning("[BuyPreview] singleItemTemplate doesn't have BundleItemDisplay component!");
            }
        }
        else
        {
            Debug.LogWarning("[BuyPreview] singleItemTemplate is null!");
        }

        if (scrollRect != null)
        {
            scrollRect.enabled = false;
        }
    }

    void ShowBundleIcons(ShopItemData data)
    {
        Debug.Log("[BuyPreview] ShowBundleIcons called");

        ClearSpawnedBundleItems();

        if (iconItemsPreview != null)
        {
            iconItemsPreview.gameObject.SetActive(true);
        }

        if (iconPreviewImage != null) iconPreviewImage.gameObject.SetActive(false);
        if (rewardAmountText != null) rewardAmountText.gameObject.SetActive(false);

        foreach (var child in existingChildren)
        {
            if (child != null) child.SetActive(false);
        }

        if (iconItemsPreview == null)
        {
            Debug.LogWarning("[BuyPreview] iconItemsPreview not assigned!");
            return;
        }

        if (bundleItemPrefab == null)
        {
            Debug.LogWarning("[BuyPreview] bundleItemPrefab not assigned!");
            return;
        }

        if (data.bundleItems == null || data.bundleItems.Count == 0)
        {
            Debug.LogWarning("[BuyPreview] Bundle has no items!");
            return;
        }

        int spawnedCount = 0;
        foreach (var bundleItem in data.bundleItems)
        {
            if (bundleItem == null || bundleItem.icon == null)
            {
                Debug.LogWarning("[BuyPreview] Bundle item or icon is null, skipping");
                continue;
            }

            var go = Instantiate(bundleItemPrefab, iconItemsPreview);
            go.SetActive(true);

            var display = go.GetComponent<BundleItemDisplay>();
            if (display != null)
            {
                display.Setup(bundleItem.icon, bundleItem.amount);
                spawnedCount++;
                spawnedBundleItems.Add(go);
            }
            else
            {
                Debug.LogWarning("[BuyPreview] bundleItemPrefab doesn't have BundleItemDisplay component!");
                Destroy(go);
            }
        }

        if (scrollRect != null)
        {
            scrollRect.enabled = spawnedCount > maxVisibleItems;

            if (scrollRect.enabled)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        Debug.Log($"[BuyPreview] Spawned {spawnedCount} bundle icons. Scroll: {(scrollRect != null && scrollRect.enabled ? "Enabled" : "Disabled")}");
    }

    void ClearSpawnedBundleItems()
    {
        foreach (var go in spawnedBundleItems)
        {
            if (go != null) Destroy(go);
        }
        spawnedBundleItems.Clear();

        Debug.Log("[BuyPreview] Cleared spawned bundle items");
    }

    void SetupButtons(ShopItemData data)
    {
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

        if (buyWithKulinoCoinBtn != null)
        {
            buyWithKulinoCoinBtn.onClick.RemoveAllListeners();
            buyWithKulinoCoinBtn.gameObject.SetActive(data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0);

            if (data.allowBuyWithKulinoCoin && data.kulinoCoinPrice > 0)
            {
                buyWithKulinoCoinBtn.onClick.AddListener(() =>
                {
                    if (manager != null)
                    {
                        Debug.Log("[BuyPreview] Attempting to buy with Kulino Coin");
                        manager.TryBuy(data, Currency.KulinoCoin);
                    }
                });
            }
        }
    }

    public void Close()
    {
        currentData = null;

        ClearSpawnedBundleItems();

        foreach (var child in existingChildren)
        {
            if (child != null) child.SetActive(true);
        }

        if (iconItemsPreview != null) iconItemsPreview.gameObject.SetActive(true);

        if (iconPreviewImage != null) iconPreviewImage.gameObject.SetActive(true);
        if (rewardAmountText != null) rewardAmountText.gameObject.SetActive(true);

        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            scrollRect.enabled = false;
        }

        if (rootPanel != null) rootPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(false);

        if (buyWithCoinsBtn != null) buyWithCoinsBtn.onClick.RemoveAllListeners();
        if (buyWithShardsBtn != null) buyWithShardsBtn.onClick.RemoveAllListeners();
        if (buyWithKulinoCoinBtn != null) buyWithKulinoCoinBtn.onClick.RemoveAllListeners();
    }
}