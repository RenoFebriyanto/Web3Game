using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Currency { Coins, Shards, KulinoCoin }

/// <summary>
/// ShopManager - FULLY AUTOMATED dengan Dynamic Category Containers
/// Version: 5.0 - Container System
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("🎨 Prefabs")]
    [Tooltip("Prefab CategoryContainer (dengan CategoryContainerUI)")]
    public GameObject categoryContainerPrefab;
    
    [Tooltip("Prefab ShopItem (BackgroundItem dengan ShopItemUI)")]
    public GameObject itemUIPrefab;
    
    [Tooltip("Parent transform (ItemsShop dengan Vertical Layout)")]
    public Transform itemsParent;
    
    [Tooltip("Popup controller untuk buy preview")]
    public BuyPreviewController buyPreviewUI;

    [Header("📦 Data")]
    [Tooltip("ShopDatabase ScriptableObject")]
    public ShopDatabase database;
    
    [Tooltip("Fallback manual list (jika database null)")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("🎯 Icons")]
    public Sprite iconCoin;
    public Sprite iconShard;
    public Sprite iconEnergy;

    [Header("⚙️ Layout Settings")]
    [Tooltip("Grid columns untuk items (default: 3)")]
    public int gridColumns = 3;
    
    [Tooltip("Cell size untuk items")]
    public Vector2 cellSize = new Vector2(150f, 200f);
    
    [Tooltip("Spacing antara items")]
    public Vector2 spacing = new Vector2(10f, 10f);

    // 4 Filters
    public enum ShopFilter { All, Shard, Items, Bundle }

    // Private
    private ShopItemData _pendingPurchaseData;
    private Dictionary<ShopRewardType, CategoryContainerUI> categoryContainers = new Dictionary<ShopRewardType, CategoryContainerUI>();
    private ShopFilter currentFilter = ShopFilter.All;

    // Category ordering
    private readonly ShopRewardType[] categoryOrder = new ShopRewardType[]
    {
        ShopRewardType.Shard,
        ShopRewardType.Coin,
        ShopRewardType.Energy,
        ShopRewardType.Booster,
        ShopRewardType.Bundle
    };

    // ==================== UNITY LIFECYCLE ====================

    void Awake()
    {
        EnsurePlayerEconomy();

        if (buyPreviewUI == null)
        {
            buyPreviewUI = FindFirstObjectByType<BuyPreviewController>();
        }
    }

    void Start()
    {
        if (buyPreviewUI != null)
        {
            buyPreviewUI.Initialize(this);
        }

        // ✅ Setup ItemsParent Vertical Layout
        SetupItemsParentLayout();

        // ✅ AUTO-POPULATE from database
        PopulateShop();
    }

    // ==================== SETUP ====================

    void SetupItemsParentLayout()
    {
        if (itemsParent == null)
        {
            Debug.LogError("[ShopManager] itemsParent not assigned!");
            return;
        }

        var verticalLayout = itemsParent.GetComponent<VerticalLayoutGroup>();
        
        if (verticalLayout == null)
        {
            verticalLayout = itemsParent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        // Setup vertical layout properties
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.spacing = -25f; // Space between categories
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.childAlignment = TextAnchor.UpperCenter;

        Debug.Log("[ShopManager] ✓ ItemsParent VerticalLayout setup done");
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

    // ==================== 🚀 AUTO-POPULATE SYSTEM ====================

    public void PopulateShop()
    {
        Debug.Log($"[ShopManager] 🚀 PopulateShop - Filter: {currentFilter}");
        
        // Default: Show ALL
        FilterShop(ShopFilter.All);
    }

    // ==================== FILTERING ====================

    public void ShowAll() { FilterShop(ShopFilter.All); }
    public void ShowShard() { FilterShop(ShopFilter.Shard); }
    public void ShowItems() { FilterShop(ShopFilter.Items); }
    public void ShowBundle() { FilterShop(ShopFilter.Bundle); }

    public void FilterShop(ShopFilter filter)
    {
        currentFilter = filter;
        Debug.Log($"[ShopManager] FilterShop: {filter}");
        
        RepopulateShop();
    }

    /// <summary>
    /// ✅ CORE: Repopulate shop dengan dynamic containers
    /// </summary>
    void RepopulateShop()
    {
        // Clear existing containers
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

        // ✅ Create containers based on filter
        switch (currentFilter)
        {
            case ShopFilter.All:
                CreateAllCategories(source);
                break;
                
            case ShopFilter.Shard:
                CreateSingleCategory(ShopRewardType.Shard, source);
                break;
                
            case ShopFilter.Items:
                CreateItemsCategories(source); // Coin + Energy + Booster
                break;
                
            case ShopFilter.Bundle:
                CreateSingleCategory(ShopRewardType.Bundle, source);
                break;
        }

        Debug.Log($"[ShopManager] ✓ Created {categoryContainers.Count} category containers");
    }

    /// <summary>
    /// ✅ Create ALL categories
    /// </summary>
    void CreateAllCategories(List<ShopItemData> source)
    {
        foreach (var rewardType in categoryOrder)
        {
            CreateCategoryContainer(rewardType, source);
        }
    }

    /// <summary>
    /// ✅ Create ITEMS categories (Coin + Energy + Booster)
    /// </summary>
    void CreateItemsCategories(List<ShopItemData> source)
    {
        CreateCategoryContainer(ShopRewardType.Coin, source);
        CreateCategoryContainer(ShopRewardType.Energy, source);
        CreateCategoryContainer(ShopRewardType.Booster, source);
    }

    /// <summary>
    /// ✅ Create single category
    /// </summary>
    void CreateSingleCategory(ShopRewardType rewardType, List<ShopItemData> source)
    {
        CreateCategoryContainer(rewardType, source);
    }

    /// <summary>
    /// ✅ CORE: Create category container dengan items
    /// </summary>
    void CreateCategoryContainer(ShopRewardType rewardType, List<ShopItemData> source)
    {
        // Filter items by reward type
        List<ShopItemData> filtered = FilterByRewardType(source, rewardType);
        
        if (filtered.Count == 0)
        {
            Debug.Log($"[ShopManager] No items for category: {rewardType}");
            return;
        }

        // Spawn container
        GameObject containerObj = Instantiate(categoryContainerPrefab, itemsParent);
        containerObj.name = $"CategoryContainer_{rewardType}";

        var container = containerObj.GetComponent<CategoryContainerUI>();
        
        if (container == null)
        {
            Debug.LogError($"[ShopManager] CategoryContainerUI not found on prefab!");
            Destroy(containerObj);
            return;
        }

        // Setup container
        container.gridColumns = gridColumns;
        container.cellSize = cellSize;
        container.spacing = spacing;
        container.SetHeaderText(GetCategoryDisplayName(rewardType));

        // Add items to container
        foreach (var data in filtered)
        {
            container.AddItem(itemUIPrefab, data, this);
        }

        // Store reference
        categoryContainers[rewardType] = container;

        Debug.Log($"[ShopManager] ✓ Created '{rewardType}' category with {filtered.Count} items");
    }

    /// <summary>
    /// Filter items by reward type
    /// </summary>
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

    /// <summary>
    /// Get display name untuk category
    /// </summary>
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

    /// <summary>
    /// Clear all containers
    /// </summary>
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
        
        Debug.Log("[ShopManager] Cleared all category containers");
    }

    // ==================== PURCHASE FLOW ====================
    // (Keep existing purchase code - no changes needed)

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

    IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        KulinoCoinManager.Instance?.RefreshBalance();
    }

    // ==================== CONTEXT MENU ====================

    [ContextMenu("🔄 Refresh Shop")]
    void Context_Refresh() { RepopulateShop(); }

    [ContextMenu("🧪 Test: ALL")]
    void Context_All() { FilterShop(ShopFilter.All); }

    [ContextMenu("🧪 Test: SHARD")]
    void Context_Shard() { FilterShop(ShopFilter.Shard); }

    [ContextMenu("🧪 Test: ITEMS")]
    void Context_Items() { FilterShop(ShopFilter.Items); }

    [ContextMenu("🧪 Test: BUNDLE")]
    void Context_Bundle() { FilterShop(ShopFilter.Bundle); }

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