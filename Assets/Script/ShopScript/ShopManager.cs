using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Currency { Coins, Shards, KulinoCoin }

/// <summary>
/// ShopManager - CLEANED v9.0
/// ✅ Removed unused layout settings
/// ✅ Improved scroll functionality
/// ✅ Better initialization sequence
/// </summary>
public class ShopManager : MonoBehaviour
{
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
    }

    void OnEnable()
    {
        if (!isInitialized)
        {
            StartCoroutine(InitializeShopSequence());
        }
        else
        {
            StartCoroutine(RefreshLayoutSequence());
        }
    }

    /// <summary>
    /// ✅ Multi-frame initialization sequence
    /// </summary>
    IEnumerator InitializeShopSequence()
    {
        Debug.Log("[ShopManager] === Starting initialization sequence ===");
        
        yield return null;
        
        Debug.Log("[ShopManager] Frame 2: Populating shop...");
        PopulateShopInternal();
        
        yield return null;
        
        Debug.Log("[ShopManager] Frame 4: First layout pass...");
        Canvas.ForceUpdateCanvases();
        
        yield return null;
        Debug.Log("[ShopManager] Frame 5: Second layout pass...");
        ForceRebuildAllLayouts();
        
        yield return null;
        Debug.Log("[ShopManager] Frame 6: Final refresh...");
        ForceRebuildAllLayouts();
        
        yield return null;
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

        var container = containerObj.GetComponent<CategoryContainerUI>();
        
        if (container == null)
        {
            Debug.LogError($"[ShopManager] CategoryContainerUI not found!");
            Destroy(containerObj);
            return;
        }

        container.SetHeaderText(GetCategoryDisplayName(rewardType));
        container.ClearDummyItems();

        foreach (var data in filtered)
        {
            container.AddItem(itemUIPrefab, data, this);
        }

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
                break;

            case Currency.Shards:
                if (!data.allowBuyWithShards) return false;
                price = data.shardPrice;
                canAfford = PlayerEconomy.Instance.Shards >= price;
                break;

            case Currency.KulinoCoin:
                if (!data.allowBuyWithKulinoCoin)
                {
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                if (KulinoCoinManager.Instance == null)
                {
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
                }
                break;
        }

        if (!canAfford)
        {
            Debug.Log($"[ShopManager] Not enough {currency}");
            SoundManager.Instance?.PlayPurchaseFail();
            return false;
        }

        buyPreviewUI?.Close();
        ShowPurchasePopup(data, currency);
        return true;
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

    [System.Serializable]
    class PaymentPayload
    {
        public double amount;
        public string itemId;
        public string itemName;
        public string nonce;
        public long timestamp;
    }
}