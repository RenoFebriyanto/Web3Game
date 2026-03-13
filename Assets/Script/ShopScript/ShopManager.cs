using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ✅ FIXED: Currency enum - KulinoCoin & Rupiah TETAP ADA agar BuyPreviewController tidak error,
// tapi tidak diproses di TryBuy
public enum Currency { Coins, Shards, KulinoCoin, Rupiah }

/// <summary>
/// ✅ FIXED v2.0: ShopManager - Semua KulinoCoinManager & KulinoCoinPriceAPI DIHAPUS
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("🎨 Prefabs")]
    public GameObject categoryContainerPrefab;
    public GameObject itemUIPrefab;
    public Transform itemsParent;
    public BuyPreviewController buyPreviewUI;

    [Header("📦 Data")]
    public ShopDatabase database;
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("🎯 Icons")]
    public Sprite iconCoin;
    public Sprite iconShard;
    public Sprite iconEnergy;

    [Header("🔄 Scroll Settings")]
    public ScrollRect scrollRect;
    [Range(0.1f, 1f)]
    public float scrollSpeed = 0.3f;

    public enum ShopFilter { All, Shard, Items, Bundle }

    private ShopItemData _pendingPurchaseData;
    private Dictionary<ShopRewardType, CategoryContainerUI> categoryContainers = new Dictionary<ShopRewardType, CategoryContainerUI>();
    private ShopFilter currentFilter = ShopFilter.All;
    private Coroutine scrollCoroutine;

    public bool isInitialized => _isInitialized;
    private bool _isInitialized = false;
    private bool isPopulating = false;

    private readonly ShopRewardType[] categoryOrder = new ShopRewardType[]
    {
        ShopRewardType.Shard,
        ShopRewardType.Coin,
        ShopRewardType.Energy,
        ShopRewardType.Booster,
        ShopRewardType.Bundle
    };

    // ─── Lifecycle ────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsurePlayerEconomy();

        if (buyPreviewUI == null)
            buyPreviewUI = FindFirstObjectByType<BuyPreviewController>();

        if (scrollRect == null && itemsParent != null)
            scrollRect = itemsParent.GetComponentInParent<ScrollRect>();
    }

    void Start()
    {
        if (buyPreviewUI != null)
            buyPreviewUI.Initialize(this);

        // ✅ REMOVED: UpdateShardPrices() — tidak ada KulinoCoinPriceAPI lagi

        Debug.Log("[ShopManager] ✓ Start complete - waiting for panel to open");
    }

    void OnEnable()
    {
        Debug.Log($"[ShopManager] OnEnable - active={gameObject.activeInHierarchy}, initialized={isInitialized}");

        // ✅ REMOVED: KulinoCoinPriceAPI check

        if (gameObject.activeInHierarchy && IsShopPanelActive())
            StartCoroutine(EnsureInitialization());
        else
            Debug.Log("[ShopManager] Shop panel not active - skipping initialization");
    }

    // ─── Shop Panel Check ─────────────────────────────────────────────────
    bool IsShopPanelActive()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "ContentShop" || current.name == "Shop" || current.name == "ShopP")
            {
                bool isActive = current.gameObject.activeInHierarchy;
                Debug.Log($"[ShopManager] Shop parent '{current.name}': active={isActive}");
                return isActive;
            }
            current = current.parent;
        }
        Debug.LogWarning("[ShopManager] Shop parent panel not found!");
        return false;
    }

    // ─── Initialization ───────────────────────────────────────────────────
    public IEnumerator EnsureInitialization()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        if (itemsParent == null) { Debug.LogError("[ShopManager] ❌ itemsParent is NULL!"); yield break; }

        Transform cur = itemsParent;
        while (cur != null)
        {
            if (!cur.gameObject.activeSelf) cur.gameObject.SetActive(true);
            cur = cur.parent;
        }

        yield return null;

        if (!itemsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError("[ShopManager] ❌ itemsParent masih tidak aktif! Check hierarchy.");
            yield break;
        }

        Debug.Log("[ShopManager] ✓ itemsParent confirmed active - starting initialization");

        if (!isInitialized)
            yield return StartCoroutine(InitializeShopSequence());
        else
            yield return StartCoroutine(RefreshLayoutSequence());
    }

    IEnumerator InitializeShopSequence()
    {
        if (isPopulating) { Debug.Log("[ShopManager] Already populating - skipping"); yield break; }

        isPopulating = true;
        Debug.Log("[ShopManager] === Starting initialization ===");

        if (itemsParent != null && !itemsParent.gameObject.activeInHierarchy)
        {
            itemsParent.gameObject.SetActive(true);
            yield return null;
        }

        Canvas.ForceUpdateCanvases();
        yield return null;

        PopulateShopInternal();
        yield return null;
        yield return null;

        int refreshedCount = 0;
        foreach (var kvp in categoryContainers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.gameObject.SetActive(true);
                kvp.Value.ForceRefreshNow();
                refreshedCount++;
            }
        }
        Debug.Log($"[ShopManager] Refreshed {refreshedCount} categories");

        Canvas.ForceUpdateCanvases();
        ForceRebuildAllLayouts();
        yield return null;
        ForceRebuildAllLayouts();
        yield return null;
        ForceRebuildAllLayouts();

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;

        _isInitialized = true;
        isPopulating = false;
        Debug.Log("[ShopManager] ✅ Initialization complete!");
    }

    // ─── Buy Logic ────────────────────────────────────────────────────────
    public void ShowBuyPreview(ShopItemData data, ShopItemUI fromUI = null)
    {
        if (buyPreviewUI != null) buyPreviewUI.Show(data);
    }

    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            EnsurePlayerEconomy();
            if (PlayerEconomy.Instance == null) { SoundManager.Instance?.PlayPurchaseFail(); return false; }
        }

        double price = 0;

        switch (currency)
        {
            case Currency.Coins:
                if (!data.allowBuyWithCoins) return false;
                price = data.coinPrice;
                if (PlayerEconomy.Instance.Coins < price)
                {
                    Debug.Log($"[ShopManager] Insufficient Coins! Need {price:N0}, have {PlayerEconomy.Instance.Coins:N0}");
                    ShowInsufficientFundsAlert(data, Currency.Coins);
                    return false;
                }
                break;

            case Currency.Shards:
                if (!data.allowBuyWithShards) return false;
                price = data.shardPrice;
                if (PlayerEconomy.Instance.Shards < price)
                {
                    Debug.Log($"[ShopManager] Insufficient Shards! Need {price:N0}, have {PlayerEconomy.Instance.Shards:N0}");
                    ShowInsufficientFundsAlert(data, Currency.Shards);
                    return false;
                }
                break;

            // ✅ KulinoCoin & Rupiah: tidak diproses, tampilkan pesan
            case Currency.KulinoCoin:
            case Currency.Rupiah:
                Debug.LogWarning("[ShopManager] KulinoCoin/Rupiah payment tidak lagi didukung.");
                SoundManager.Instance?.PlayPurchaseFail();
                return false;

            default:
                Debug.LogWarning($"[ShopManager] Currency {currency} tidak dikenal.");
                return false;
        }

        buyPreviewUI?.Close();
        ShowPurchasePopup(data, currency);
        return true;
    }

    void ShowInsufficientFundsAlert(ShopItemData data, Currency currency)
    {
        Debug.Log($"[ShopManager] ⚠️ Insufficient {currency}!");
        SoundManager.Instance?.PlayPurchaseFail();
        buyPreviewUI?.Close();

        if (OpenPhantomBuyCoinPopup.Instance == null) { Debug.LogError("[ShopManager] OpenPhantomBuyCoinPopup.Instance NULL!"); return; }

        switch (currency)
        {
            case Currency.Coins:
                OpenPhantomBuyCoinPopup.Instance.Show("Not Enough Coins", "You don't have enough Coins. Go to shop to buy more?", () => OpenShopFilterItems());
                break;
            case Currency.Shards:
                OpenPhantomBuyCoinPopup.Instance.Show("Not Enough Shards", "You don't have enough Shards. Go to shop to buy more?", () => OpenShopFilterShard());
                break;
        }
    }

    void ShowPurchasePopup(ShopItemData data, Currency currency)
    {
        // Deduct currency and grant reward directly
        bool deducted = false;

        switch (currency)
        {
            case Currency.Coins:
                PlayerEconomy.Instance.SpendCoins((long)data.coinPrice);
                deducted = true;
                break;
            case Currency.Shards:
                PlayerEconomy.Instance.SpendShards(data.shardPrice);
                deducted = true;
                break;
        }

        if (deducted)
        {
            GrantReward(data);
            SoundManager.Instance?.PlayPurchaseSuccess();
            Debug.Log($"[ShopManager] ✅ Purchase success: {data.displayName}");
        }
    }

    public void OnPaymentConfirmed()
    {
        if (_pendingPurchaseData != null)
        {
            GrantReward(_pendingPurchaseData);
            SoundManager.Instance?.PlayPurchaseSuccess();
            _pendingPurchaseData = null;
        }
    }

    void GrantReward(ShopItemData data)
    {
        if (data == null || PlayerEconomy.Instance == null) return;

        switch (data.rewardType)
        {
            case ShopRewardType.Energy: PlayerEconomy.Instance.AddEnergy(data.rewardAmount); break;
            case ShopRewardType.Coin:   PlayerEconomy.Instance.AddCoins(data.rewardAmount);  break;
            case ShopRewardType.Shard:  PlayerEconomy.Instance.AddShards(data.rewardAmount); break;
            case ShopRewardType.Booster:
                EnsureBoosterInventory();
                BoosterInventory.Instance?.AddBooster(data.itemId, data.rewardAmount);
                break;
            case ShopRewardType.Bundle: GrantBundleReward(data); break;
        }
    }

    void GrantBundleReward(ShopItemData data)
    {
        if (data?.bundleItems == null) return;
        foreach (var item in data.bundleItems)
        {
            if (item == null) continue;
            string id = item.itemId.ToLower().Trim();
            if (id == "coin" || id == "coins")          PlayerEconomy.Instance.AddCoins(item.amount);
            else if (id == "shard" || id == "shards")   PlayerEconomy.Instance.AddShards(item.amount);
            else if (id == "energy")                    PlayerEconomy.Instance.AddEnergy(item.amount);
            else { EnsureBoosterInventory(); BoosterInventory.Instance?.AddBooster(item.itemId, item.amount); }
        }
    }

    // ─── Populate & Filter ────────────────────────────────────────────────
    public void PopulateShop() { StartCoroutine(PopulateShopSequence()); }

    IEnumerator PopulateShopSequence()
    {
        FilterShop(ShopFilter.All);
        yield return null;
        yield return null;
        ForceRebuildAllLayouts();
        ScrollToTop();
    }

    public void ShowAll()   { StartCoroutine(FilterSequence(ShopFilter.All));    }
    public void ShowShard() { StartCoroutine(FilterSequence(ShopFilter.Shard));  }
    public void ShowItems() { StartCoroutine(FilterSequence(ShopFilter.Items));  }
    public void ShowBundle(){ StartCoroutine(FilterSequence(ShopFilter.Bundle)); }

    IEnumerator FilterSequence(ShopFilter filter)
    {
        FilterShop(filter);
        yield return null;
        yield return null;
        ForceRebuildAllLayouts();
        ScrollToTop();
    }

    public void FilterShop(ShopFilter filter)
    {
        currentFilter = filter;
        Debug.Log($"[ShopManager] Filter: {filter}");
        PopulateShopInternal();
    }

    void PopulateShopInternal()
    {
        ClearAllContainers();

        if (categoryContainerPrefab == null || itemUIPrefab == null || itemsParent == null)
        {
            Debug.LogError("[ShopManager] Missing prefabs!");
            return;
        }

        if (!itemsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError("[ShopManager] ❌ itemsParent INACTIVE! Force enabling...");
            itemsParent.gameObject.SetActive(true);
        }

        List<ShopItemData> source = database?.items ?? shopItems;
        if (source == null || source.Count == 0) { Debug.LogWarning("[ShopManager] No items!"); return; }

        switch (currentFilter)
        {
            case ShopFilter.All:    CreateAllCategories(source);                              break;
            case ShopFilter.Shard:  CreateSingleCategory(ShopRewardType.Shard,  source);     break;
            case ShopFilter.Items:  CreateItemsCategories(source);                            break;
            case ShopFilter.Bundle: CreateSingleCategory(ShopRewardType.Bundle, source);     break;
        }

        Debug.Log($"[ShopManager] ✓ Created {categoryContainers.Count} categories");
    }

    void CreateAllCategories(List<ShopItemData> source)
    {
        foreach (var t in categoryOrder) CreateCategoryContainer(t, source);
    }

    void CreateItemsCategories(List<ShopItemData> source)
    {
        CreateCategoryContainer(ShopRewardType.Coin,    source);
        CreateCategoryContainer(ShopRewardType.Energy,  source);
        CreateCategoryContainer(ShopRewardType.Booster, source);
    }

    void CreateSingleCategory(ShopRewardType t, List<ShopItemData> source) => CreateCategoryContainer(t, source);

    void CreateCategoryContainer(ShopRewardType rewardType, List<ShopItemData> source)
    {
        var filtered = FilterByRewardType(source, rewardType);
        if (filtered.Count == 0) { Debug.Log($"[ShopManager] No items for: {rewardType}"); return; }

        if (itemsParent == null || !itemsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[ShopManager] ❌ itemsParent INACTIVE! Cannot create {rewardType}");
            return;
        }

        GameObject containerObj = Instantiate(categoryContainerPrefab, itemsParent);
        containerObj.name = $"CategoryContainer_{rewardType}";
        containerObj.SetActive(true);
        Canvas.ForceUpdateCanvases();

        var container = containerObj.GetComponent<CategoryContainerUI>();
        if (container == null) { Debug.LogError($"[ShopManager] CategoryContainerUI missing!"); return; }

        container.SetHeaderText(GetCategoryHeader(rewardType));
        container.ClearDummyItems();

        foreach (var item in filtered)
        {
            if (item != null)
                container.AddItem(itemUIPrefab, item, this);
        }

        categoryContainers[rewardType] = container;
        Debug.Log($"[ShopManager] ✓ {rewardType}: {filtered.Count} items");
    }

    List<ShopItemData> FilterByRewardType(List<ShopItemData> source, ShopRewardType type)
    {
        var result = new List<ShopItemData>();
        foreach (var item in source)
            if (item != null && item.rewardType == type) result.Add(item);
        return result;
    }

    string GetCategoryHeader(ShopRewardType t) => t switch
    {
        ShopRewardType.Shard   => "Shards",
        ShopRewardType.Coin    => "Coins",
        ShopRewardType.Energy  => "Energy",
        ShopRewardType.Booster => "Boosters",
        ShopRewardType.Bundle  => "Bundles",
        _                      => t.ToString()
    };

    void ClearAllContainers()
    {
        foreach (var kvp in categoryContainers)
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        categoryContainers.Clear();
    }

    // ─── Layout ───────────────────────────────────────────────────────────
    public void ForceRebuildAllLayouts()
    {
        if (itemsParent != null)
        {
            var r = itemsParent.GetComponent<RectTransform>();
            if (r != null) LayoutRebuilder.ForceRebuildLayoutImmediate(r);
        }

        foreach (var kvp in categoryContainers)
            if (kvp.Value != null) kvp.Value.RefreshLayout();

        Canvas.ForceUpdateCanvases();
    }

    IEnumerator RefreshLayoutSequence()
    {
        yield return null;
        yield return null;
        ForceRebuildAllLayouts();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    // ─── Scroll ───────────────────────────────────────────────────────────
    public void ScrollToTop()
    {
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    // ─── Helper ───────────────────────────────────────────────────────────
    void OpenShopFilterItems()  { if (ButtonManager.Instance != null) ButtonManager.Instance.ShowShop(); StartCoroutine(ShowItemsAfterDelay()); }
    void OpenShopFilterShard()  { if (ButtonManager.Instance != null) ButtonManager.Instance.ShowShop(); StartCoroutine(ShowShardsAfterDelay()); }
    IEnumerator ShowItemsAfterDelay()  { yield return new WaitForSeconds(0.3f); ShowItems(); ScrollToTop(); }
    IEnumerator ShowShardsAfterDelay() { yield return new WaitForSeconds(0.3f); ShowShard(); ScrollToTop(); }

    Sprite GetItemIcon(ShopItemData data)
    {
        if (data == null) return null;
        if (data.iconPreview != null) return data.iconPreview;
        if (data.iconGrid != null)    return data.iconGrid;
        return data.rewardType switch
        {
            ShopRewardType.Coin    => iconCoin,
            ShopRewardType.Shard   => iconShard,
            ShopRewardType.Energy  => iconEnergy,
            _                      => null
        };
    }

    string GetItemAmountText(ShopItemData data)
    {
        if (data.rewardAmount <= 0) return "";
        return data.rewardType == ShopRewardType.Booster
            ? $"x{data.rewardAmount}"
            : data.rewardAmount.ToString("N0");
    }

    void EnsurePlayerEconomy()
    {
        if (PlayerEconomy.Instance != null) return;
        var existing = FindFirstObjectByType<PlayerEconomy>();
        if (existing != null) return;
        var prefab = Resources.Load<GameObject>("EconomyManager");
        if (prefab != null) { Instantiate(prefab).name = "EconomyManager"; return; }
        var go = new GameObject("PlayerEconomy");
        go.AddComponent<PlayerEconomy>();
        DontDestroyOnLoad(go);
    }

    void EnsureBoosterInventory()
    {
        if (BoosterInventory.Instance != null) return;
        var go = new GameObject("BoosterInventory");
        go.AddComponent<BoosterInventory>();
        DontDestroyOnLoad(go);
    }

    void LogError(string msg) { Debug.LogError($"[ShopManager] ❌ {msg}"); }

    [ContextMenu("🔄 Refresh Shop")]
    void Context_Refresh() { StartCoroutine(PopulateShopSequence()); }

    [ContextMenu("🔧 Force Layout Rebuild")]
    void Context_ForceLayout() { StartCoroutine(ForceLayoutSequence()); }

    IEnumerator ForceLayoutSequence()
    {
        yield return null;
        yield return null;
        ForceRebuildAllLayouts();
        yield return null;
        ForceRebuildAllLayouts();
    }

    // ─── Serializable ─────────────────────────────────────────────────────
    [Serializable]
    class PaymentPayload { public double amount; public string itemId, itemName, nonce; public long timestamp; }
}