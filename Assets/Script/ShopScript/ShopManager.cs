using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Currency { Coins, Shards, KulinoCoin, Rupiah }

/// <summary>
/// ShopManager - CLEANED v9.0
/// ✅ Removed unused layout settings
/// ✅ Improved scroll functionality
/// ✅ Better initialization sequence
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

    [Header("🏢 Company Wallet (for Rupiah transactions)")]
    [Tooltip("Wallet perusahaan Kulino untuk menerima pembayaran Shard")]
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
        EnsurePlayerEconomy();

        if (buyPreviewUI == null)
        {
            buyPreviewUI = FindFirstObjectByType<BuyPreviewController>();
        }

        if (scrollRect == null && itemsParent != null)
        {
            scrollRect = itemsParent.GetComponentInParent<ScrollRect>();
            
            if (scrollRect != null)
            {
                Debug.Log("[ShopManager] ✓ ScrollRect auto-found");
            }
        }

        
    }

    void Start()
{
    if (buyPreviewUI != null)
    {
        buyPreviewUI.Initialize(this);
    }
    
    // ✅ CRITICAL: Update prices on start
    UpdateShardPrices();
}

    void OnEnable()
{
    // ✅ Update prices setiap kali shop dibuka
    if (KulinoCoinPriceAPI.Instance != null)
    {
        UpdateShardPrices();
    }
    
    if (!isInitialized)
    {
        StartCoroutine(InitializeShopSequence());
    }
    else
    {
        // ✅ CRITICAL: Force refresh saat panel reopened
        StartCoroutine(ForceRefreshOnEnable());
    }
}

/// <summary>
/// ✅ NEW: Aggressive refresh saat panel dibuka kembali
/// </summary>
IEnumerator ForceRefreshOnEnable()
{
    Debug.Log("[ShopManager] === Force refresh on enable ===");
    
    // Wait 2 frames
    yield return null;
    yield return null;
    
    // Force rebuild ALL
    ForceRebuildAllLayouts();
    
    yield return null;
    ForceRebuildAllLayouts();
    
    // Reset scroll
    if (scrollRect != null)
    {
        scrollRect.verticalNormalizedPosition = 1f;
    }
    
    Debug.Log("[ShopManager] ✅ Refresh complete");
}

    /// <summary>
/// ✅ IMPROVED: Extended initialization dengan more aggressive refresh
/// </summary>
IEnumerator InitializeShopSequence()
{
    Debug.Log("[ShopManager] === Starting initialization sequence ===");
    
    // Frame 1: Wait untuk Canvas ready
    yield return null;
    
    Debug.Log("[ShopManager] Frame 2: Populating shop...");
    PopulateShopInternal();
    
    // ✅ CRITICAL: Wait 2 frames untuk items spawned
    yield return null;
    yield return null;
    
    Debug.Log("[ShopManager] Frame 4: First aggressive pass...");
    
    // ✅ Force semua categories refresh
    foreach (var kvp in categoryContainers)
    {
        if (kvp.Value != null && kvp.Value.gameObject.activeSelf)
        {
            kvp.Value.ForceRefreshNow();
        }
    }
    
    Canvas.ForceUpdateCanvases();
    ForceRebuildAllLayouts();
    
    // ✅ Wait 3 frames untuk layout settle
    yield return null;
    yield return null;
    yield return null;
    
    Debug.Log("[ShopManager] Frame 7: Second pass...");
    ForceRebuildAllLayouts();
    
    yield return null;
    
    Debug.Log("[ShopManager] Frame 8: Final pass...");
    ForceRebuildAllLayouts();
    
    // ✅ Reset scroll position
    if (scrollRect != null)
    {
        scrollRect.verticalNormalizedPosition = 1f;
    }
    
    isInitialized = true;
    Debug.Log("[ShopManager] ✅ Initialization complete!");
}

    /// <summary>
    /// ✅ Refresh layout saat panel reopened
    /// </summary>
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

    /// <summary>
    /// ✅ Force rebuild SEMUA layouts
    /// </summary>
    void ForceRebuildAllLayouts()
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
        
        Debug.Log("[ShopManager] ✓ All layouts rebuilt");
    }

    /// <summary>
/// ✅ FIXED: Update shard prices based on real-time Kulino Coin price
/// Call this on Start() dan sebelum populate shop
/// </summary>
void UpdateShardPrices()
{
    if (KulinoCoinPriceAPI.Instance == null)
    {
        Debug.LogWarning("[ShopManager] KulinoCoinPriceAPI not found - using default prices");
        return;
    }
    
    double kcPriceIDR = KulinoCoinPriceAPI.Instance.GetCurrentPrice();
    Debug.Log($"[ShopManager] 💰 Current KC price: Rp {kcPriceIDR:N2} per KC");
    
    if (database == null || database.items == null)
    {
        Debug.LogWarning("[ShopManager] No database items to update");
        return;
    }
    
    int updatedCount = 0;
    
    foreach (var item in database.items)
    {
        if (item == null) continue;
        
        // ✅ CRITICAL: Update prices untuk Shard items yang bisa dibeli dengan KC
        if (item.rewardType == ShopRewardType.Shard && item.allowBuyWithKulinoCoin)
        {
            // Get IDR price based on shard amount
            double idrPrice = GetShardIDRPrice(item.rewardAmount);
            
            // Convert IDR to KC
            double kcPrice = KulinoCoinPriceAPI.Instance.ConvertIDRToKulinoCoin(idrPrice);
            
            // ✅ Update item price
            item.kulinoCoinPrice = kcPrice;
            
            Debug.Log($"[ShopManager] Updated {item.displayName} ({item.rewardAmount} Shard):");
            Debug.Log($"  - IDR: Rp {idrPrice:N0}");
            Debug.Log($"  - KC: {kcPrice:F6} KC");
            
            updatedCount++;
        }
    }
    
    Debug.Log($"[ShopManager] ✅ Updated {updatedCount} shard items with dynamic pricing");
}

/// <summary>
/// ✅ FIXED: Get IDR price for specific shard amount
/// Mapping shard amount → Rupiah price
/// </summary>
double GetShardIDRPrice(int shardAmount)
{
    // ✅ CRITICAL: Price mapping berdasarkan jumlah shard
    switch(shardAmount)
    {
        case 100:   return 15000;    // Rp 15,000
        case 350:   return 50000;    // Rp 50,000
        case 500:   return 75000;    // Rp 75,000
        case 1000:  return 150000;   // Rp 150,000
        case 1500:  return 220000;   // Rp 220,000
        case 3500:  return 500000;   // Rp 500,000
        case 5000:  return 750000;   // Rp 750,000
        case 8000:  return 1200000;  // Rp 1,200,000
        case 10000: return 1500000;  // Rp 1,500,000
        default:
            // Fallback: Rp 150 per shard
            return shardAmount * 150;
    }
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

    public void ShowAll() 
    { 
        StartCoroutine(FilterSequence(ShopFilter.All));
    }
    
    public void ShowShard() 
    { 
        StartCoroutine(FilterSequence(ShopFilter.Shard));
    }
    
    public void ShowItems() 
    { 
        StartCoroutine(FilterSequence(ShopFilter.Items));
    }
    
    public void ShowBundle() 
    { 
        StartCoroutine(FilterSequence(ShopFilter.Bundle));
    }

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
        Debug.Log($"[ShopManager] FilterShop: {filter}");
        PopulateShopInternal();
    }

        void PopulateShopInternal()
    {
        ClearAllContainers();

        if (categoryContainerPrefab == null || itemUIPrefab == null || itemsParent == null)
        {
            Debug.LogError("[ShopManager] Missing prefab references!");
            return;
        }

        // ✅ CRITICAL: Force categories to initialize BEFORE spawning items
        EnsureCategoriesInitialized();

        List<ShopItemData> source = database?.items ?? shopItems;
        
        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No items in ShopDatabase!");
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

    /// <summary>
/// ✅ NEW: Force initialize all category containers BEFORE spawning items
/// </summary>
void EnsureCategoriesInitialized()
{
    Debug.Log("[ShopManager] 🔧 Force initializing categories...");
    
    if (itemsParent == null) return;
    
    // Get all CategoryContainerUI in children (termasuk yang inactive)
    var containers = itemsParent.GetComponentsInChildren<CategoryContainerUI>(true);
    
    foreach (var container in containers)
    {
        // Force call Awake equivalent
        if (container != null && !container.gameObject.activeSelf)
        {
            container.gameObject.SetActive(true);
        }
    }
    
    Canvas.ForceUpdateCanvases();
    
    Debug.Log($"[ShopManager] ✓ Initialized {containers.Length} containers");
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

    GameObject containerObj = Instantiate(categoryContainerPrefab, itemsParent);
    containerObj.name = $"CategoryContainer_{rewardType}";
    
    // ✅ CRITICAL: Ensure active immediately
    containerObj.SetActive(true);

    var container = containerObj.GetComponent<CategoryContainerUI>();
    
    if (container == null)
    {
        Debug.LogError($"[ShopManager] CategoryContainerUI not found!");
        Destroy(containerObj);
        return;
    }

    // ✅ Set header first
    container.SetHeaderText(GetCategoryDisplayName(rewardType));
    
    // ✅ Clear dummy items
    container.ClearDummyItems();

    // ✅ CRITICAL: Force Canvas update AFTER header set, BEFORE adding items
    Canvas.ForceUpdateCanvases();

    // ✅ Add all items
    foreach (var data in filtered)
    {
        container.AddItem(itemUIPrefab, data, this);
    }

    // ✅ CRITICAL: Call refresh AFTER all items added
    container.OnAllItemsAdded();

    categoryContainers[rewardType] = container;

    Debug.Log($"[ShopManager] ✓ '{rewardType}' with {filtered.Count} items");
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
        if (scrollRect == null)
        {
            Debug.LogWarning("[ShopManager] ScrollRect not assigned!");
            return;
        }

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
                // ✅ FIXED: Check balance first before handling payment
                if (!data.UseRupiahPricing)
                {
                    Debug.LogError("[ShopManager] Item doesn't support Rupiah pricing!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                // Check KC price API
                if (KulinoCoinPriceAPI.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinPriceAPI not found!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                // Get current KC price
                double kcPriceIDR = KulinoCoinPriceAPI.Instance.GetCurrentPrice();
                if (kcPriceIDR <= 0)
                {
                    Debug.LogError("[ShopManager] Invalid KC price!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                // Calculate required KC
                double rupiahAmount = data.rupiahPrice;
                double requiredKC = rupiahAmount / kcPriceIDR;

                // ✅ CRITICAL: Check balance
                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager not found!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                double playerBalance = KulinoCoinManager.Instance.GetBalance();

                if (playerBalance < requiredKC)
                {
                    // ✅ INSUFFICIENT KC - Show popup
                    Debug.LogWarning($"[ShopManager] Insufficient KC! Need {requiredKC:F6}, have {playerBalance:F6}");
                    ShowInsufficientFundsAlert(data, Currency.Rupiah);
                    return false;
                }

                // ✅ Enough balance - proceed with payment
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

                // Refresh balance
                KulinoCoinManager.Instance.RefreshBalance();
                System.Threading.Thread.Sleep(500);

                double currentBalance = KulinoCoinManager.Instance.GetBalance();
                canAfford = currentBalance >= price;

                if (!canAfford)
                {
                    // ✅ INSUFFICIENT KC - Show popup
                    Debug.LogWarning($"[ShopManager] Insufficient KC! Need {price:F6}, have {currentBalance:F6}");
                    ShowInsufficientFundsAlert(data, Currency.KulinoCoin);
                    return false;
                }
                break;
        }

        // ✅ Can afford - close preview and proceed
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
                Debug.Log("[ShopManager] Showing popup for insufficient Coins");

                OpenPhantomBuyCoinPopup.Instance.Show(
                    "Not Enough Coins",
                    "You don't have enough Coins. Go to shop to buy more?",
                    () => {
                        Debug.Log("[ShopManager] 🔄 Continue clicked - Opening Shop filter Items");
                        OpenShopFilterItems();
                    }
                );
                break;

            case Currency.Shards:
                Debug.Log("[ShopManager] Showing popup for insufficient Shards");

                OpenPhantomBuyCoinPopup.Instance.Show(
                    "Not Enough Shards",
                    "You don't have enough Shards. Go to shop to buy more?",
                    () => {
                        Debug.Log("[ShopManager] 🔄 Continue clicked - Opening Shop filter Shard");
                        OpenShopFilterShard();
                    }
                );
                break;

            case Currency.KulinoCoin:
            case Currency.Rupiah:
                Debug.Log("[ShopManager] Showing popup for insufficient KC");

                OpenPhantomBuyCoinPopup.Instance.Show(
                    "Not Enough Kulino Coin",
                    "You don't have enough Kulino Coin. Buy some in Phantom Wallet?",
                    null  // ✅ FIXED: Pass null to use default Jupiter swap URL
                );
                break;
        }
    }
    void OpenShopFilterItems()
    {
        Debug.Log("[ShopManager] Opening Shop → Filter: Items");

        // Open shop panel
        if (ButtonManager.Instance != null)
        {
            ButtonManager.Instance.ShowShop();
        }

        // Wait for panel to open, then set filter
        StartCoroutine(ShowItemsAfterDelay());
    }

    /// <summary>
    /// ✅ Helper: Open shop with Shard filter
    /// </summary>
    void OpenShopFilterShard()
    {
        Debug.Log("[ShopManager] Opening Shop → Filter: Shard");

        // Open shop panel
        if (ButtonManager.Instance != null)
        {
            ButtonManager.Instance.ShowShop();
        }

        // Wait for panel to open, then set filter
        StartCoroutine(ShowShardsAfterDelay());
    }

    /// <summary>
    /// ✅ Helper: Open Phantom Wallet
    /// </summary>
    void OpenPhantomWallet()
    {
        Debug.Log("[ShopManager] Opening Phantom Wallet");

#if UNITY_WEBGL && !UNITY_EDITOR
    try
    {
        string phantomURL = "https://phantom.app/";
        string jsCode = $"window.open('{phantomURL}', '_blank');";
        Application.ExternalEval(jsCode);
        Debug.Log("[ShopManager] ✓ Phantom Wallet opened");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[ShopManager] Failed to open Phantom: {ex.Message}");
        Application.OpenURL("https://phantom.app/");
    }
#else
        Application.OpenURL("https://phantom.app/");
        Debug.Log("[ShopManager] 🧪 EDITOR: Phantom URL opened");
#endif
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

    // ✅ NEW: Handle pembayaran Rupiah
    bool HandleRupiahPayment(ShopItemData data)
    {
        if (!data.UseRupiahPricing)
        {
            Debug.LogError("[ShopManager] Item doesn't support Rupiah pricing!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        // Check KC price API
        if (KulinoCoinPriceAPI.Instance == null)
        {
            Debug.LogError("[ShopManager] KulinoCoinPriceAPI not found!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        // Get current KC price in Rupiah
        double kcPriceIDR = KulinoCoinPriceAPI.Instance.GetCurrentPrice();
        
        if (kcPriceIDR <= 0)
        {
            Debug.LogError("[ShopManager] Invalid KC price!");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        // Calculate required KC amount
        double rupiahAmount = data.rupiahPrice;
        double requiredKC = rupiahAmount / kcPriceIDR;

        Debug.Log("═══════════════════════════════════");
        Debug.Log("[ShopManager] 💰 RUPIAH PAYMENT");
        Debug.Log($"  Item: {data.displayName}");
        Debug.Log($"  Rupiah Price: Rp {rupiahAmount:N0}");
        Debug.Log($"  KC Price: Rp {kcPriceIDR:N2} per KC");
        Debug.Log($"  Required KC: {requiredKC:F6} KC");
        Debug.Log($"  Destination: {ShortenAddress(companyWalletAddress)}");
        Debug.Log("═══════════════════════════════════");

        // Check player balance
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

        // Close preview and initiate payment
        buyPreviewUI?.Close();
        
        _pendingPurchaseData = data;
        StartCoroutine(InitiateRupiahPayment(data, requiredKC, rupiahAmount));
        
        return true;
    }

    // ✅ NEW: Initiate Rupiah payment via Phantom
    IEnumerator InitiateRupiahPayment(ShopItemData data, double kcAmount, double rupiahAmount)
    {
        Debug.Log($"[ShopManager] 🔄 Initiating Rupiah payment...");
        
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
        
        Debug.Log($"[ShopManager] 📤 Payment payload:");
        Debug.Log($"  Destination: {ShortenAddress(companyWalletAddress)}");
        Debug.Log($"  Amount: {kcAmount:F6} KC (= Rp {rupiahAmount:N0})");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // ✅ Call JavaScript function untuk payment
            string jsCode = $"if(typeof requestRupiahPayment === 'function') {{ requestRupiahPayment('{json}'); }}";
            Application.ExternalEval(jsCode);
            
            Debug.Log("[ShopManager] ✓ Payment request sent to JavaScript");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] Failed to call JavaScript: {ex.Message}");
            SoundManager.Instance?.PlayPurchaseFail();
            _pendingPurchaseData = null;
        }
#else
        // ✅ EDITOR MODE: Simulate success
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[ShopManager] 🧪 EDITOR MODE: Simulating payment success");
        
        string mockResponse = $"{{\"success\":true,\"txHash\":\"MOCK_RUPIAH_TX_{System.DateTime.Now.Ticks}\",\"kcAmount\":{kcAmount},\"rupiahAmount\":{rupiahAmount}}}";
        
        OnRupiahPaymentResult(mockResponse);
#endif
        
        yield return null;
    }

    // ✅ NEW: Callback dari JavaScript saat payment selesai
    public void OnRupiahPaymentResult(string resultJson)
    {
        Debug.Log($"[ShopManager] 📥 Rupiah payment result: {resultJson}");

        try
        {
            var result = JsonUtility.FromJson<PaymentResult>(resultJson);

            if (result.success)
            {
                Debug.Log($"[ShopManager] ✅ RUPIAH PAYMENT SUCCESS!");
                Debug.Log($"  TX Hash: {result.txHash}");
                
                if (_pendingPurchaseData != null)
                {
                    GrantReward(_pendingPurchaseData);
                    _pendingPurchaseData = null;
                    SoundManager.Instance?.PlayPurchaseSuccess();
                }

                // Refresh KC balance
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
            Debug.Log("[ShopManager] 🔄 Refreshing KC balance after payment...");
            KulinoCoinManager.Instance.RefreshBalance();
        }
    }

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10) return addr;
        return $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
    }

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