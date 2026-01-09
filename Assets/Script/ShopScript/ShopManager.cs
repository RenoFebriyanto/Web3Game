using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Currency { Coins, Shards, KulinoCoin, Rupiah }

/// <summary>
/// ✅ FIXED: ShopManager - Initialization Issue
/// - Fixed: itemsParent not active issue on first load
/// - Categories now populate correctly on initial open
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

    [Header("🏢 Company Wallet")]
    public string companyWalletAddress = "9QM8aSCHFp76RacWXgXFQQxUKXt5Vf2zLzSkdMdQuByk";

    [Header("🔄 Scroll Settings")]
    public ScrollRect scrollRect;
    [Range(0.1f, 1f)]
    public float scrollSpeed = 0.3f;

    public enum ShopFilter { All, Shard, Items, Bundle }

    private ShopItemData _pendingPurchaseData;
    private Dictionary<ShopRewardType, CategoryContainerUI> categoryContainers = new Dictionary<ShopRewardType, CategoryContainerUI>();
    private ShopFilter currentFilter = ShopFilter.All;
    private Coroutine scrollCoroutine;
    private bool isInitialized = false;
    private bool isPopulating = false;

    private readonly ShopRewardType[] categoryOrder = new ShopRewardType[]
    {
        ShopRewardType.Shard,
        ShopRewardType.Coin,
        ShopRewardType.Energy,
        ShopRewardType.Booster,
        ShopRewardType.Bundle
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsurePlayerEconomy();

        if (buyPreviewUI == null)
        {
            buyPreviewUI = FindFirstObjectByType<BuyPreviewController>();
        }

        if (scrollRect == null && itemsParent != null)
        {
            scrollRect = itemsParent.GetComponentInParent<ScrollRect>();
        }
    }

    void Start()
    {
        if (buyPreviewUI != null)
        {
            buyPreviewUI.Initialize(this);
        }

        UpdateShardPrices();

        Debug.Log("[ShopManager] ✓ Start complete - waiting for panel to open");
    }

    // ✅ FIXED: OnEnable - Better initialization check
    void OnEnable()
    {
        Debug.Log($"[ShopManager] OnEnable - active={gameObject.activeInHierarchy}, initialized={isInitialized}");

        // Update prices
        if (KulinoCoinPriceAPI.Instance != null)
        {
            UpdateShardPrices();
        }

        // ✅ FIX: Force enable itemsParent jika belum active
        if (itemsParent != null && !itemsParent.gameObject.activeInHierarchy)
        {
            Debug.Log("[ShopManager] ⚠️ itemsParent not active - force enabling");
            itemsParent.gameObject.SetActive(true);
        }

        // ✅ FIX: Wait lebih lama untuk ensure hierarchy ready
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(InitializeWhenPanelActive());
        }
    }

    // ✅ FIXED: Better waiting mechanism
    IEnumerator InitializeWhenPanelActive()
    {
        // ✅ FIX: Wait 3 frames instead of 2
        yield return null;
        yield return null;
        yield return null;

        // ✅ FIX: Force enable itemsParent if still inactive
        if (itemsParent != null && !itemsParent.gameObject.activeInHierarchy)
        {
            Debug.Log("[ShopManager] 🔧 Force enabling itemsParent...");
            itemsParent.gameObject.SetActive(true);
            yield return null; // Wait 1 more frame after enabling
        }

        // Check jika panel masih active
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log("[ShopManager] Panel became inactive - skipping init");
            yield break;
        }

        // ✅ FIX: Better validation check
        if (itemsParent == null)
        {
            Debug.LogError("[ShopManager] ❌ itemsParent is NULL!");
            yield break;
        }

        if (!itemsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError("[ShopManager] ❌ itemsParent still not active after force enable!");
            yield break;
        }

        Debug.Log("[ShopManager] ✓ Panel confirmed active - starting initialization");

        // Initialize or refresh
        if (!isInitialized)
        {
            yield return StartCoroutine(InitializeShopSequence());
        }
        else
        {
            yield return StartCoroutine(RefreshLayoutSequence());
        }
    }

    IEnumerator InitializeShopSequence()
    {
        if (isPopulating)
        {
            Debug.Log("[ShopManager] Already populating - skipping");
            yield break;
        }

        isPopulating = true;
        Debug.Log("[ShopManager] === Starting initialization ===");

        // ✅ FIX: Ensure itemsParent active
        if (itemsParent != null)
        {
            if (!itemsParent.gameObject.activeInHierarchy)
            {
                itemsParent.gameObject.SetActive(true);
                yield return null;
            }
        }

        Canvas.ForceUpdateCanvases();
        yield return null;

        // Populate
        PopulateShopInternal();
        yield return null;
        yield return null;

        // Refresh layouts
        Debug.Log("[ShopManager] Refreshing layouts...");
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

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        isInitialized = true;
        isPopulating = false;

        Debug.Log("[ShopManager] ✅ Initialization complete!");
    }

    IEnumerator RefreshLayoutSequence()
    {
        yield return null;
        yield return null;

        ForceRebuildAllLayouts();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ForceRebuildAllLayouts()
    {
        if (itemsParent != null)
        {
            var parentRect = itemsParent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        foreach (var kvp in categoryContainers)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                kvp.Value.RefreshLayout();
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    void UpdateShardPrices()
    {
        if (KulinoCoinPriceAPI.Instance == null) return;

        double kcPriceIDR = KulinoCoinPriceAPI.Instance.GetCurrentPrice();
        Debug.Log($"[ShopManager] 💰 KC price: Rp {kcPriceIDR:N2}");

        if (database == null || database.items == null) return;

        int updatedCount = 0;
        foreach (var item in database.items)
        {
            if (item == null) continue;

            if (item.rewardType == ShopRewardType.Shard && item.allowBuyWithKulinoCoin)
            {
                double idrPrice = GetShardIDRPrice(item.rewardAmount);
                double kcPrice = KulinoCoinPriceAPI.Instance.ConvertIDRToKulinoCoin(idrPrice);
                item.kulinoCoinPrice = kcPrice;
                updatedCount++;
            }
        }

        Debug.Log($"[ShopManager] ✅ Updated {updatedCount} shard prices");
    }

    double GetShardIDRPrice(int shardAmount)
    {
        return shardAmount switch
        {
            100 => 15000,
            350 => 50000,
            500 => 75000,
            1000 => 150000,
            1500 => 220000,
            3500 => 500000,
            5000 => 750000,
            8000 => 1200000,
            10000 => 1500000,
            _ => shardAmount * 150
        };
    }

    void EnsurePlayerEconomy()
    {
        if (PlayerEconomy.Instance != null) return;

        var existing = FindFirstObjectByType<PlayerEconomy>();
        if (existing != null) return;

        var prefab = Resources.Load<GameObject>("EconomyManager");
        if (prefab != null)
        {
            Instantiate(prefab).name = "EconomyManager";
            return;
        }

        var go = new GameObject("PlayerEconomy");
        go.AddComponent<PlayerEconomy>();
        DontDestroyOnLoad(go);
    }

    void EnsureBoosterInventory()
    {
        if (BoosterInventory.Instance == null)
        {
            var go = new GameObject("BoosterInventory");
            go.AddComponent<BoosterInventory>();
            DontDestroyOnLoad(go);
        }
    }

    public void PopulateShop()
    {
        StartCoroutine(PopulateShopSequence());
    }

    IEnumerator PopulateShopSequence()
    {
        FilterShop(ShopFilter.All);
        yield return null;
        yield return null;
        ForceRebuildAllLayouts();
        ScrollToTop();
    }

    public void ShowAll() { StartCoroutine(FilterSequence(ShopFilter.All)); }
    public void ShowShard() { StartCoroutine(FilterSequence(ShopFilter.Shard)); }
    public void ShowItems() { StartCoroutine(FilterSequence(ShopFilter.Items)); }
    public void ShowBundle() { StartCoroutine(FilterSequence(ShopFilter.Bundle)); }

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

        // ✅ FIX: Better validation
        if (!itemsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError("[ShopManager] ❌ itemsParent INACTIVE! Force enabling...");
            itemsParent.gameObject.SetActive(true);
        }

        List<ShopItemData> source = database?.items ?? shopItems;
        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No items!");
            return;
        }

        switch (currentFilter)
        {
            case ShopFilter.All:
                CreateAllCategories(source);
                break;
            case ShopFilter.Shard:
                CreateSingleCategory(ShopRewardType.Shard, source);
                break;
            case ShopFilter.Items:
                CreateItemsCategories(source);
                break;
            case ShopFilter.Bundle:
                CreateSingleCategory(ShopRewardType.Bundle, source);
                break;
        }

        Debug.Log($"[ShopManager] ✓ Created {categoryContainers.Count} categories");
    }

    void CreateAllCategories(List<ShopItemData> source)
    {
        foreach (var rewardType in categoryOrder)
        {
            CreateCategoryContainer(rewardType, source);
        }
    }

    void CreateItemsCategories(List<ShopItemData> source)
    {
        CreateCategoryContainer(ShopRewardType.Coin, source);
        CreateCategoryContainer(ShopRewardType.Energy, source);
        CreateCategoryContainer(ShopRewardType.Booster, source);
    }

    void CreateSingleCategory(ShopRewardType rewardType, List<ShopItemData> source)
    {
        CreateCategoryContainer(rewardType, source);
    }

    void CreateCategoryContainer(ShopRewardType rewardType, List<ShopItemData> source)
    {
        List<ShopItemData> filtered = FilterByRewardType(source, rewardType);

        if (filtered.Count == 0)
        {
            Debug.Log($"[ShopManager] No items for: {rewardType}");
            return;
        }

        Debug.Log($"[ShopManager] Creating {rewardType}: {filtered.Count} items");

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
        if (container == null)
        {
            Debug.LogError($"[ShopManager] CategoryContainerUI missing!");
            Destroy(containerObj);
            return;
        }

        container.SetHeaderText(GetCategoryDisplayName(rewardType));
        container.ClearDummyItems();
        Canvas.ForceUpdateCanvases();

        foreach (var data in filtered)
        {
            container.AddItem(itemUIPrefab, data, this);
        }

        container.ForceRefreshNow();
        container.OnAllItemsAdded();

        categoryContainers[rewardType] = container;

        Debug.Log($"[ShopManager] ✓ Created {rewardType}");
    }

    List<ShopItemData> FilterByRewardType(List<ShopItemData> source, ShopRewardType rewardType)
    {
        List<ShopItemData> filtered = new List<ShopItemData>();
        foreach (var data in source)
        {
            if (data != null && data.rewardType == rewardType)
            {
                filtered.Add(data);
            }
        }
        return filtered;
    }

    string GetCategoryDisplayName(ShopRewardType type)
    {
        return type switch
        {
            ShopRewardType.Shard => "SHARD",
            ShopRewardType.Coin => "COIN",
            ShopRewardType.Energy => "ENERGY",
            ShopRewardType.Booster => "BOOSTER",
            ShopRewardType.Bundle => "BUNDLE",
            _ => type.ToString().ToUpper()
        };
    }

    void ClearAllContainers()
    {
        foreach (var kvp in categoryContainers)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        categoryContainers.Clear();
    }

    public void ScrollToTop()
    {
        if (scrollRect == null) return;

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }

        scrollCoroutine = StartCoroutine(SmoothScrollToTop());
    }

    IEnumerator SmoothScrollToTop()
    {
        float currentPos = scrollRect.verticalNormalizedPosition;
        float targetPos = 1f;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime / scrollSpeed;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(currentPos, targetPos, elapsed);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetPos;
        scrollCoroutine = null;
    }

    public void ShowBuyPreview(ShopItemData data, ShopItemUI fromUI = null)
    {
        if (buyPreviewUI != null)
            buyPreviewUI.Show(data);
    }

    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            EnsurePlayerEconomy();
            if (PlayerEconomy.Instance == null)
            {
                SoundManager.Instance?.PlayPurchaseFail();
                return false;
            }
        }

        bool canAfford = false;
        double price = 0;

        switch (currency)
        {
            case Currency.Coins:
                if (!data.allowBuyWithCoins) return false;
                price = data.coinPrice;
                canAfford = PlayerEconomy.Instance.Coins >= price;

                if (!canAfford)
                {
                    Debug.Log($"[ShopManager] Insufficient Coins! Need {price:N0}, have {PlayerEconomy.Instance.Coins:N0}");
                    ShowInsufficientFundsAlert(data, Currency.Coins);
                    return false;
                }
                break;

            case Currency.Shards:
                if (!data.allowBuyWithShards) return false;
                price = data.shardPrice;
                canAfford = PlayerEconomy.Instance.Shards >= price;

                if (!canAfford)
                {
                    Debug.Log($"[ShopManager] Insufficient Shards! Need {price:N0}, have {PlayerEconomy.Instance.Shards:N0}");
                    ShowInsufficientFundsAlert(data, Currency.Shards);
                    return false;
                }
                break;

            case Currency.Rupiah:
                if (!data.UseRupiahPricing)
                {
                    Debug.LogError("[ShopManager] Item doesn't support Rupiah pricing!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                if (KulinoCoinPriceAPI.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinPriceAPI not found!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                double kcPriceIDR = KulinoCoinPriceAPI.Instance.GetCurrentPrice();
                if (kcPriceIDR <= 0)
                {
                    Debug.LogError("[ShopManager] Invalid KC price!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                double rupiahAmount = data.rupiahPrice;
                double requiredKC = rupiahAmount / kcPriceIDR;

                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager not found!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                double playerBalance = KulinoCoinManager.Instance.GetBalance();

                if (playerBalance < requiredKC)
                {
                    Debug.LogWarning($"[ShopManager] Insufficient KC! Need {requiredKC:F6}, have {playerBalance:F6}");
                    ShowInsufficientFundsAlert(data, Currency.Rupiah);
                    return false;
                }

                return HandleRupiahPayment(data);

            case Currency.KulinoCoin:
                if (!data.allowBuyWithKulinoCoin)
                {
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager not found!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                price = data.kulinoCoinPrice;

                KulinoCoinManager.Instance.RefreshBalance();
                System.Threading.Thread.Sleep(500);

                double currentBalance = KulinoCoinManager.Instance.GetBalance();
                canAfford = currentBalance >= price;

                if (!canAfford)
                {
                    Debug.LogWarning($"[ShopManager] Insufficient KC! Need {price:F6}, have {currentBalance:F6}");
                    ShowInsufficientFundsAlert(data, Currency.KulinoCoin);
                    return false;
                }
                break;
        }

        buyPreviewUI?.Close();
        ShowPurchasePopup(data, currency);
        return true;
    }

    void ShowInsufficientFundsAlert(ShopItemData data, Currency currency)
    {
        Debug.Log($"[ShopManager] ⚠️ Insufficient {currency}!");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseFail();
        }

        if (buyPreviewUI != null)
        {
            buyPreviewUI.Close();
        }

        if (OpenPhantomBuyCoinPopup.Instance == null)
        {
            Debug.LogError("[ShopManager] OpenPhantomBuyCoinPopup.Instance is NULL!");
            return;
        }

        switch (currency)
        {
            case Currency.Coins:
                OpenPhantomBuyCoinPopup.Instance.Show(
                    "Not Enough Coins",
                    "You don't have enough Coins. Go to shop to buy more?",
                    () => OpenShopFilterItems()
                );
                break;

            case Currency.Shards:
                OpenPhantomBuyCoinPopup.Instance.Show(
                    "Not Enough Shards",
                    "You don't have enough Shards. Go to shop to buy more?",
                    () => OpenShopFilterShard()
                );
                break;

            case Currency.KulinoCoin:
            case Currency.Rupiah:
                OpenPhantomBuyCoinPopup.Instance.Show(
                    "Not Enough Kulino Coin",
                    "You don't have enough Kulino Coin. Buy some in Phantom Wallet?",
                    null
                );
                break;
        }
    }

    void OpenShopFilterItems()
    {
        if (ButtonManager.Instance != null)
        {
            ButtonManager.Instance.ShowShop();
        }
        StartCoroutine(ShowItemsAfterDelay());
    }

    void OpenShopFilterShard()
    {
        if (ButtonManager.Instance != null)
        {
            ButtonManager.Instance.ShowShop();
        }
        StartCoroutine(ShowShardsAfterDelay());
    }

    IEnumerator ShowItemsAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);
        ShowItems();
        ScrollToTop();
    }

    IEnumerator ShowShardsAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);
        ShowShard();
        ScrollToTop();
    }

    bool HandleRupiahPayment(ShopItemData data)
    {
        if (!data.UseRupiahPricing)
        {
            Debug.LogError("[ShopManager] Item doesn't support Rupiah pricing!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        if (KulinoCoinPriceAPI.Instance == null)
        {
            Debug.LogError("[ShopManager] KulinoCoinPriceAPI not found!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        double kcPriceIDR = KulinoCoinPriceAPI.Instance.GetCurrentPrice();

        if (kcPriceIDR <= 0)
        {
            Debug.LogError("[ShopManager] Invalid KC price!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        double rupiahAmount = data.rupiahPrice;
        double requiredKC = rupiahAmount / kcPriceIDR;

        if (KulinoCoinManager.Instance == null)
        {
            Debug.LogError("[ShopManager] KulinoCoinManager not found!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        double playerBalance = KulinoCoinManager.Instance.GetBalance();

        if (playerBalance < requiredKC)
        {
            Debug.LogWarning($"[ShopManager] Insufficient KC! Need {requiredKC:F6}, have {playerBalance:F6}");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        buyPreviewUI?.Close();

        _pendingPurchaseData = data;
        StartCoroutine(InitiateRupiahPayment(data, requiredKC, rupiahAmount));

        return true;
    }

    IEnumerator InitiateRupiahPayment(ShopItemData data, double kcAmount, double rupiahAmount)
    {
        var payload = new RupiahPaymentPayload
        {
            destinationWallet = companyWalletAddress,
            kcAmount = kcAmount,
            rupiahAmount = rupiahAmount,
            itemId = data.itemId,
            itemName = data.displayName,
            rewardAmount = data.rewardAmount,
            nonce = System.Guid.NewGuid().ToString(),
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string json = JsonUtility.ToJson(payload);

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string jsCode = $"if(typeof requestRupiahPayment === 'function') {{ requestRupiahPayment('{json}'); }}";
            Application.ExternalEval(jsCode);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] Failed to call JavaScript: {ex.Message}");
            SoundManager.Instance?.PlayPurchaseFail();
            _pendingPurchaseData = null;
        }
#else
        yield return new WaitForSeconds(2f);

        string mockResponse = $"{{\"success\":true,\"txHash\":\"MOCK_RUPIAH_TX_{System.DateTime.Now.Ticks}\",\"kcAmount\":{kcAmount},\"rupiahAmount\":{rupiahAmount}}}";

        OnRupiahPaymentResult(mockResponse);
#endif

        yield return null;
    }

    public void OnRupiahPaymentResult(string resultJson)
    {
        try
        {
            var result = JsonUtility.FromJson<PaymentResult>(resultJson);

            if (result.success)
            {
                if (_pendingPurchaseData != null)
                {
                    GrantReward(_pendingPurchaseData);
                    _pendingPurchaseData = null;
                    SoundManager.Instance?.PlayPurchaseSuccess();
                }

                StartCoroutine(RefreshKCBalanceDelayed(2f));
            }
            else
            {
                Debug.LogError($"[ShopManager] ❌ RUPIAH PAYMENT FAILED: {result.error}");
                SoundManager.Instance?.PlayPurchaseFail();
                _pendingPurchaseData = null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] ❌ Parse error: {ex.Message}");
            SoundManager.Instance?.PlayPurchaseFail();
            _pendingPurchaseData = null;
        }
    }

    IEnumerator RefreshKCBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
        }
    }

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10) return addr;
        return $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
    }

    // ======================================== NEED TO TEST THIS PART

    void ShowPurchasePopup(ShopItemData data, Currency currency)
    {
        if (PopupClaimQuest.Instance == null)
        {
            CompletePurchase(data, currency);
            return;
        }

        if (data.IsBundle)
            ShowBundlePurchasePopup(data, currency);
        else
            ShowSingleItemPurchasePopup(data, currency);
    }

    void ShowSingleItemPurchasePopup(ShopItemData data, Currency currency)
    {
        Sprite icon = GetItemIcon(data);
        string amountText = GetItemAmountText(data);
        string title = $"Purchase {data.displayName}";

        PopupClaimQuest.Instance.Open(
            icon,
            amountText,
            title,
            () => CompletePurchase(data, currency)
        );
    }

    void ShowBundlePurchasePopup(ShopItemData data, Currency currency)
    {
        List<BundleItemData> bundleItems = new List<BundleItemData>();

        foreach (var item in data.bundleItems)
        {
            if (item == null || item.icon == null) continue;

            bundleItems.Add(new BundleItemData(
                item.icon,
                item.amount,
                item.displayName
            ));
        }

        string title = $"Purchase {data.displayName}";
        string description = data.description ?? $"Bundle with {bundleItems.Count} items";

        PopupClaimQuest.Instance.OpenBundle(
            bundleItems,
            title,
            description,
            () => CompletePurchase(data, currency)
        );
    }

    void CompletePurchase(ShopItemData data, Currency currency)
    {
        if (PlayerEconomy.Instance == null) return;

        bool deductSuccess = false;

        switch (currency)
        {
            case Currency.Coins:
                if (!PlayerEconomy.Instance.SpendCoins(data.coinPrice))
                {
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }
                deductSuccess = true;
                break;

            case Currency.Shards:
                if (!PlayerEconomy.Instance.SpendShards(data.shardPrice))
                {
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }
                deductSuccess = true;
                break;

            case Currency.KulinoCoin:
                if (KulinoCoinManager.Instance == null)
                {
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }

                if (!KulinoCoinManager.Instance.HasEnoughBalance(data.kulinoCoinPrice))
                {
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }

                _pendingPurchaseData = data;
                StartCoroutine(InitiatePhantomPayment(data, data.kulinoCoinPrice));
                return;
        }

        if (deductSuccess)
        {
            GrantReward(data);
        }
    }

    IEnumerator InitiatePhantomPayment(ShopItemData data, double kcAmount)
    {
        var payload = new PaymentPayload
        {
            amount = kcAmount,
            itemId = data.itemId,
            itemName = data.displayName,
            nonce = System.Guid.NewGuid().ToString(),
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        string json = JsonUtility.ToJson(payload);
        
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string jsCode = $"if(typeof requestKulinoCoinPayment === 'function') {{ requestKulinoCoinPayment('{json}'); }}";
            Application.ExternalEval(jsCode);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] Failed to call JavaScript: {ex.Message}");
            SoundManager.Instance?.PlayPurchaseFail();
            _pendingPurchaseData = null;
        }
#else
        yield return new WaitForSeconds(2f);
        
        string mockResponse = $"{{\"success\":true,\"txHash\":\"MOCK_TX_{System.DateTime.Now.Ticks}\"}}";
        
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.OnPhantomPaymentResult(mockResponse);
            
            if (_pendingPurchaseData != null)
            {
                GrantReward(_pendingPurchaseData);
                _pendingPurchaseData = null;
                SoundManager.Instance?.PlayPurchaseSuccess();
            }
        }
#endif
        
        yield return null;
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
            case ShopRewardType.Energy:
                PlayerEconomy.Instance.AddEnergy(data.rewardAmount);
                break;

            case ShopRewardType.Coin:
                PlayerEconomy.Instance.AddCoins(data.rewardAmount);
                break;

            case ShopRewardType.Shard:
                PlayerEconomy.Instance.AddShards(data.rewardAmount);
                break;

            case ShopRewardType.Booster:
                EnsureBoosterInventory();
                BoosterInventory.Instance?.AddBooster(data.itemId, data.rewardAmount);
                break;

            case ShopRewardType.Bundle:
                GrantBundleReward(data);
                break;
        }
    }

    void GrantBundleReward(ShopItemData data)
    {
        if (data?.bundleItems == null) return;

        foreach (var item in data.bundleItems)
        {
            if (item == null) continue;

            string id = item.itemId.ToLower().Trim();

            if (id == "coin" || id == "coins")
                PlayerEconomy.Instance.AddCoins(item.amount);
            else if (id == "shard" || id == "shards")
                PlayerEconomy.Instance.AddShards(item.amount);
            else if (id == "energy")
                PlayerEconomy.Instance.AddEnergy(item.amount);
            else
            {
                EnsureBoosterInventory();
                BoosterInventory.Instance?.AddBooster(item.itemId, item.amount);
            }
        }
    }

    Sprite GetItemIcon(ShopItemData data)
    {
        if (data == null) return null;
        if (data.iconPreview != null) return data.iconPreview;
        if (data.iconGrid != null) return data.iconGrid;

        return data.rewardType switch
        {
            ShopRewardType.Coin => iconCoin,
            ShopRewardType.Shard => iconShard,
            ShopRewardType.Energy => iconEnergy,
            _ => null
        };
    }

    string GetItemAmountText(ShopItemData data)
    {
        if (data.rewardAmount <= 0) return "";
        return data.rewardType == ShopRewardType.Booster 
            ? $"x{data.rewardAmount}" 
            : data.rewardAmount.ToString("N0");
    }

    

void LogError(string message)
{
    Debug.LogError($"[ShopManager] ❌ {message}");
}

    [ContextMenu("🔄 Refresh Shop")]
    void Context_Refresh() 
    { 
        StartCoroutine(PopulateShopSequence());
    }

    [ContextMenu("🔧 Force Layout Rebuild")]
    void Context_ForceLayout()
    {
        StartCoroutine(ForceLayoutSequence());
    }

    IEnumerator ForceLayoutSequence()
    {
        yield return null;
        yield return null;
        ForceRebuildAllLayouts();
        yield return null;
        ForceRebuildAllLayouts();
    }

    // ========================================
// ✅ TAMBAHKAN: Context menu untuk testing
// ========================================

[ContextMenu("💰 Test: Update Shard Prices")]
void Context_TestUpdatePrices()
{
    UpdateShardPrices();
}

[ContextMenu("💰 Test: Print Shard Prices")]
void Context_PrintShardPrices()
{
    Debug.Log("=== SHARD PRICES ===");
    
    if (database == null || database.items == null)
    {
        Debug.Log("No database");
        return;
    }
    
    foreach (var item in database.items)
    {
        if (item != null && item.rewardType == ShopRewardType.Shard && item.allowBuyWithKulinoCoin)
        {
            double idrPrice = GetShardIDRPrice(item.rewardAmount);
            Debug.Log($"{item.displayName}: {item.rewardAmount} Shard = Rp {idrPrice:N0} = {item.kulinoCoinPrice:F6} KC");
        }
    }
    
    Debug.Log("===================");
}

    [System.Serializable]
    class PaymentPayload
    {
        public double amount;
        public string itemId;
        public string itemName;
        public string nonce;
        public long timestamp;
    }

    // ✅ NEW: Payment payload untuk Rupiah transaction
    [System.Serializable]
    class RupiahPaymentPayload
    {
        public string destinationWallet;  // Wallet perusahaan
        public double kcAmount;           // Jumlah KC yang harus ditransfer
        public double rupiahAmount;       // Nilai Rupiah equivalent
        public string itemId;
        public string itemName;
        public int rewardAmount;
        public string nonce;
        public long timestamp;
    }

    [System.Serializable]
    class PaymentResult
    {
        public bool success;
        public string error;
        public string txHash;
        public double kcAmount;
        public double rupiahAmount;
    }
}