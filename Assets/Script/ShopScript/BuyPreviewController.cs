// BuyPreviewController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuyPreviewController : MonoBehaviour
{
    [Header("UI refs (assign in Inspector)")]
    public GameObject rootPanel;
    public Image blurOverlay;
    public RectTransform previewPanel;

    [Header("Icon Display")]
    public Transform iconItemsPreview;      // Container untuk display items (single atau bundle)
    public GameObject singleItemTemplate;   // Template existing IconsItem pertama (untuk single item)

    [Header("Single Item Display (jika pakai terpisah)")]
    public Image iconPreviewImage;          // Optional: icon besar terpisah untuk single item
    public TMP_Text rewardAmountText;       // Optional: text terpisah untuk single item

    [Header("Bundle Item Prefab")]
    public GameObject bundleItemPrefab;     // Prefab IconsItem (untuk clone bundle)

    [Header("Info Display")]
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Price Display")]
    public TMP_Text coinPriceText;
    public TMP_Text shardPriceText;
    public GameObject coinPriceGroup;
    public GameObject shardPriceGroup;

    [Header("Buttons")]
    public Button buyWithCoinsBtn;
    public Button buyWithShardsBtn;
    public TMP_Text buyWithCoinsBtnText;
    public TMP_Text buyWithShardsBtnText;
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

        // Auto-find ScrollRect
        if (scrollRect == null && iconItemsPreview != null)
        {
            scrollRect = iconItemsPreview.GetComponentInParent<ScrollRect>();
        }

        // PERBAIKAN: Simpan direct children dari IconItemsPreview (IconsItems parent, bukan grandchildren)
        if (iconItemsPreview != null)
        {
            for (int i = 0; i < iconItemsPreview.childCount; i++)
            {
                var child = iconItemsPreview.GetChild(i).gameObject;
                existingChildren.Add(child);
                Debug.Log($"[BuyPreview] Saved existing child: {child.name}");
            }

            // Auto-assign singleItemTemplate dari existing children
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

        // Common: title, description, prices
        if (titleText != null) titleText.text = data.displayName ?? "";
        if (descText != null) descText.text = data.description ?? "";

        if (coinPriceText != null) coinPriceText.text = data.coinPrice > 0 ? data.coinPrice.ToString("N0") : "";
        if (shardPriceText != null) shardPriceText.text = data.shardPrice > 0 ? data.shardPrice.ToString("N0") : "";

        if (coinPriceGroup != null) coinPriceGroup.SetActive(data.allowBuyWithCoins && data.coinPrice > 0);
        if (shardPriceGroup != null) shardPriceGroup.SetActive(data.allowBuyWithShards && data.shardPrice > 0);

        // Check if bundle or single
        if (data.IsBundle)
        {
            ShowBundleIcons(data);
        }
        else
        {
            ShowSingleIcon(data);
        }

        // Setup buttons
        SetupButtons(data);
    }

    void ShowSingleIcon(ShopItemData data)
    {
        Debug.Log("[BuyPreview] ShowSingleIcon called");

        // Destroy spawned bundle items jika ada
        ClearSpawnedBundleItems();

        // Show IconItemsPreview container
        if (iconItemsPreview != null)
        {
            iconItemsPreview.gameObject.SetActive(true);
        }

        // Hide iconPreviewImage & rewardAmountText jika ada
        if (iconPreviewImage != null) iconPreviewImage.gameObject.SetActive(false);
        if (rewardAmountText != null) rewardAmountText.gameObject.SetActive(false);

        // Show HANYA singleItemTemplate (IconsItems parent)
        for (int i = 0; i < existingChildren.Count; i++)
        {
            if (existingChildren[i] != null)
            {
                existingChildren[i].SetActive(i == 0); // show hanya index 0 (IconsItems)
            }
        }

        // PENTING: Pastikan children dari singleItemTemplate aktif (Icons, Priceitems)
        if (singleItemTemplate != null)
        {
            singleItemTemplate.SetActive(true);

            // Aktifkan semua children dari singleItemTemplate
            for (int i = 0; i < singleItemTemplate.transform.childCount; i++)
            {
                var child = singleItemTemplate.transform.GetChild(i).gameObject;
                child.SetActive(true);
                Debug.Log($"[BuyPreview] Activated child: {child.name}");
            }

            // Update dengan data
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

        // Disable scroll untuk single item
        if (scrollRect != null)
        {
            scrollRect.enabled = false;
        }
    }

    void ShowBundleIcons(ShopItemData data)
    {
        Debug.Log("[BuyPreview] ShowBundleIcons called");

        // Destroy spawned items dari preview sebelumnya
        ClearSpawnedBundleItems();

        // Show IconItemsPreview container
        if (iconItemsPreview != null)
        {
            iconItemsPreview.gameObject.SetActive(true);
        }

        // Hide iconPreviewImage & rewardAmountText (tidak dipakai untuk bundle)
        if (iconPreviewImage != null) iconPreviewImage.gameObject.SetActive(false);
        if (rewardAmountText != null) rewardAmountText.gameObject.SetActive(false);

        // Hide SEMUA existing children (tidak dipakai untuk bundle)
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

        // Spawn bundle items (clones)
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

        // Enable scroll if items > maxVisibleItems
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
        // Destroy HANYA spawned bundle items (clones), bukan existing children
        foreach (var go in spawnedBundleItems)
        {
            if (go != null) Destroy(go);
        }
        spawnedBundleItems.Clear();

        Debug.Log("[BuyPreview] Cleared spawned bundle items");
    }

    void SetupButtons(ShopItemData data)
    {
        // Buy with Coins button
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

        // Buy with Shards button
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

        // Clear spawned bundle items
        ClearSpawnedBundleItems();

        // Restore existing children visibility
        foreach (var child in existingChildren)
        {
            if (child != null) child.SetActive(true);
        }

        // Show IconItemsPreview
        if (iconItemsPreview != null) iconItemsPreview.gameObject.SetActive(true);

        // Restore iconPreviewImage & rewardAmountText
        if (iconPreviewImage != null) iconPreviewImage.gameObject.SetActive(true);
        if (rewardAmountText != null) rewardAmountText.gameObject.SetActive(true);

        // Reset scroll
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            scrollRect.enabled = false;
        }

        if (rootPanel != null) rootPanel.SetActive(false);
        if (blurOverlay != null) blurOverlay.gameObject.SetActive(false);

        if (buyWithCoinsBtn != null) buyWithCoinsBtn.onClick.RemoveAllListeners();
        if (buyWithShardsBtn != null) buyWithShardsBtn.onClick.RemoveAllListeners();
    }
}