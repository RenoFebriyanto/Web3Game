using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Currency { Coins, Shards, KulinoCoin }

/// <summary>
/// UPDATED ShopManager dengan Dynamic Category Headers
/// Version: 4.0 - Scrollable Category Headers
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Prefabs & UI")]
    [Tooltip("Prefab for one shop entry (BackgroundItem). Must have ShopItemUI component.")]
    public GameObject itemUIPrefab;
    
    [Tooltip("Prefab for category header (with CategoryHeaderUI component)")]
    public GameObject categoryHeaderPrefab;
    
    [Tooltip("Parent transform (ItemsShop) where items will be instantiated.")]
    public Transform itemsParent;
    
    [Tooltip("Popup controller for buy preview (optional but recommended).")]
    public BuyPreviewController buyPreviewUI;

    [Header("Data")]
    [Tooltip("Optional: ShopDatabase ScriptableObject with items array. If null, uses shopItems list.")]
    public ShopDatabase database;
    [Tooltip("Fallback manual list of ShopItemData if no database assigned.")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("Icons for Economy Items")]
    [Tooltip("Icon untuk Coins")]
    public Sprite iconCoin;
    [Tooltip("Icon untuk Shards")]
    public Sprite iconShard;
    [Tooltip("Icon untuk Energy")]
    public Sprite iconEnergy;

    // ✅ FINAL: 4 Filters Only (All, Shard, Items, Bundle)
    public enum ShopFilter { All, Shard, Items, Bundle }

    // Private variables
    private ShopItemData _pendingPurchaseData;
    private List<GameObject> spawnedObjects = new List<GameObject>(); // Headers + Items
    private ShopFilter currentFilter = ShopFilter.All;

    // ==================== UNITY LIFECYCLE ====================

    void Awake()
    {
        EnsurePlayerEconomy();

        if (buyPreviewUI == null)
        {
            try
            {
                buyPreviewUI = UnityEngine.Object.FindFirstObjectByType<BuyPreviewController>();
            }
            catch { }
        }
    }

    void Start()
    {
        if (buyPreviewUI != null)
        {
            try { buyPreviewUI.Initialize(this); }
            catch (Exception ex) { Debug.LogWarning("[ShopManager] buyPreviewUI.Initialize threw: " + ex.Message); }
        }

        PopulateShop();
    }

    // ==================== ECONOMY SETUP ====================

    void EnsurePlayerEconomy()
    {
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log("[ShopManager] PlayerEconomy already exists");
            return;
        }

        Debug.LogWarning("[ShopManager] PlayerEconomy.Instance is null! Attempting to find or create...");

        var existing = FindFirstObjectByType<PlayerEconomy>();
        if (existing != null)
        {
            Debug.Log("[ShopManager] Found existing PlayerEconomy in scene");
            return;
        }

        var prefab = Resources.Load<GameObject>("EconomyManager");
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            instance.name = "EconomyManager";
            DontDestroyOnLoad(instance);
            Debug.Log("[ShopManager] Created PlayerEconomy from Resources/EconomyManager prefab");
            return;
        }

        var go = new GameObject("PlayerEconomy");
        go.AddComponent<PlayerEconomy>();
        DontDestroyOnLoad(go);
        Debug.Log("[ShopManager] Created fallback PlayerEconomy GameObject");
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

    // ==================== SHOP POPULATION ====================

    public void PopulateShop()
    {
        Debug.Log($"[ShopManager] PopulateShop called with filter: {currentFilter}");
        
        // Show ALL by default
        FilterShop(ShopFilter.All);
    }

    // ==================== ✅ SHOP FILTERING - 4 BUTTONS ====================

    public void ShowAll() { FilterShop(ShopFilter.All); }
    public void ShowShard() { FilterShop(ShopFilter.Shard); }
    public void ShowItems() { FilterShop(ShopFilter.Items); }
    public void ShowBundle() { FilterShop(ShopFilter.Bundle); }

    public void FilterShop(ShopFilter filter)
    {
        currentFilter = filter;
        Debug.Log($"[ShopManager] FilterShop: {filter}");
        RepopulateWithCategories();
    }

    void RepopulateWithCategories()
    {
        // Clear all spawned objects (headers + items)
        ClearAllSpawned();

        if (itemUIPrefab == null || itemsParent == null)
        {
            Debug.LogError("[ShopManager] itemUIPrefab or itemsParent not assigned!");
            return;
        }

        List<ShopItemData> source = database?.items ?? shopItems;
        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No shop items found!");
            return;
        }

        // ✅ 4 Filter System
        switch (currentFilter)
        {
            case ShopFilter.All:
                SpawnAllCategories(source);
                break;
                
            case ShopFilter.Shard:
                SpawnSingleCategory("SHARD", ShopRewardType.Shard, source);
                break;
                
            case ShopFilter.Items:
                SpawnItemsCategories(source); // Coin + Energy + Booster
                break;
                
            case ShopFilter.Bundle:
                SpawnSingleCategory("BUNDLE", ShopRewardType.Bundle, source);
                break;
        }

        Debug.Log($"[ShopManager] ✓ Spawned {spawnedObjects.Count} objects");
    }

    /// <summary>
    /// ✅ Spawn ALL categories (Shard, Coin, Energy, Booster, Bundle)
    /// Order: Shard → Coin → Energy → Booster → Bundle
    /// </summary>
    void SpawnAllCategories(List<ShopItemData> source)
    {
        SpawnCategoryWithItems("SHARD", ShopRewardType.Shard, source);
        SpawnCategoryWithItems("COIN", ShopRewardType.Coin, source);
        SpawnCategoryWithItems("ENERGY", ShopRewardType.Energy, source);
        SpawnCategoryWithItems("BOOSTER", ShopRewardType.Booster, source);
        SpawnCategoryWithItems("BUNDLE", ShopRewardType.Bundle, source);
    }

    /// <summary>
    /// ✅ Spawn ITEMS categories (Coin + Energy + Booster)
    /// Order: Coin → Energy → Booster
    /// </summary>
    void SpawnItemsCategories(List<ShopItemData> source)
    {
        SpawnCategoryWithItems("COIN", ShopRewardType.Coin, source);
        SpawnCategoryWithItems("ENERGY", ShopRewardType.Energy, source);
        SpawnCategoryWithItems("BOOSTER", ShopRewardType.Booster, source);
    }

    /// <summary>
    /// ✅ Spawn category header + items untuk satu category
    /// </summary>
    void SpawnSingleCategory(string categoryName, ShopRewardType rewardType, List<ShopItemData> source)
    {
        SpawnCategoryWithItems(categoryName, rewardType, source);
    }

    /// <summary>
    /// ✅ Spawn category dengan items (hanya jika ada items)
    /// </summary>
    void SpawnCategoryWithItems(string categoryName, ShopRewardType rewardType, List<ShopItemData> source)
    {
        List<ShopItemData> filtered = FilterByRewardType(source, rewardType);
        
        if (filtered.Count == 0)
        {
            Debug.LogWarning($"[ShopManager] No items found for category: {categoryName}");
            return;
        }

        // ✅ Get GridLayoutGroup
        var gridLayout = itemsParent.GetComponent<GridLayoutGroup>();
        int originalConstraintCount = gridLayout != null ? gridLayout.constraintCount : 3;

        // ✅ TRICK: Set constraint ke 1 untuk header (full width)
        if (gridLayout != null)
        {
            gridLayout.constraintCount = 1;
        }

        // Spawn category header
        SpawnCategoryHeader(categoryName);

        // ✅ Reset constraint ke original untuk items (3 kolom)
        if (gridLayout != null)
        {
            gridLayout.constraintCount = originalConstraintCount;
        }

        // Spawn items
        foreach (var data in filtered)
        {
            SpawnShopItem(data);
        }
        
        Debug.Log($"[ShopManager] ✓ Spawned category '{categoryName}' with {filtered.Count} items");
    }

    /// <summary>
    /// ✅ Spawn category header - Simple version tanpa tricks
    /// </summary>
    void SpawnCategoryHeader(string categoryName)
    {
        if (categoryHeaderPrefab == null)
        {
            Debug.LogWarning("[ShopManager] categoryHeaderPrefab not assigned!");
            return;
        }

        GameObject headerObj = Instantiate(categoryHeaderPrefab, itemsParent);
        headerObj.name = $"CategoryHeader_{categoryName}";

        var headerUI = headerObj.GetComponent<CategoryHeaderUI>();
        if (headerUI != null)
        {
            headerUI.SetText(categoryName);
        }

        spawnedObjects.Add(headerObj);
        
        Debug.Log($"[ShopManager] ✓ Spawned header: {categoryName}");
    }

    /// <summary>
    /// ✅ Spawn shop item
    /// </summary>
    void SpawnShopItem(ShopItemData data)
    {
        if (data == null) return;

        GameObject itemObj = Instantiate(itemUIPrefab, itemsParent);
        itemObj.name = $"ShopItem_{data.itemId}";

        var ui = itemObj.GetComponent<ShopItemUI>() ?? itemObj.GetComponentInChildren<ShopItemUI>(true);
        
        if (ui != null)
        {
            try
            {
                ui.Setup(data, this);
                spawnedObjects.Add(itemObj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopManager] Error setting up shop item: {ex.Message}");
                Destroy(itemObj);
            }
        }
        else
        {
            Debug.LogWarning($"[ShopManager] ShopItemUI not found in prefab!");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// ✅ Filter items by reward type
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
    /// ✅ Clear all spawned objects
    /// </summary>
    void ClearAllSpawned()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        spawnedObjects.Clear();
        
        Debug.Log("[ShopManager] Cleared all spawned objects");
    }

    // ==================== PURCHASE FLOW ====================

    public void ShowBuyPreview(ShopItemData data, ShopItemUI fromUI = null)
    {
        if (buyPreviewUI != null) 
            buyPreviewUI.Show(data);
        else 
            Debug.LogWarning("[ShopManager] buyPreviewUI not assigned");
    }

    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null!");
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
                    Debug.LogWarning("[ShopManager] Item cannot be purchased with Kulino Coin");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager.Instance is null!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return false;
                }

                price = data.kulinoCoinPrice;
                
                KulinoCoinManager.Instance.RefreshBalance();
                System.Threading.Thread.Sleep(500);
                
                double currentBalance = KulinoCoinManager.Instance.GetBalance();
                canAfford = currentBalance >= price;
                
                Debug.Log($"[ShopManager] Kulino Coin Check:");
                Debug.Log($"  - Required: {price:F6} KC");
                Debug.Log($"  - Current Balance: {currentBalance:F6} KC");
                Debug.Log($"  - Can Afford: {canAfford}");
                
                if (!canAfford)
                {
                    Debug.LogWarning($"[ShopManager] Insufficient Kulino Coin! Need {price:F6} KC, have {currentBalance:F6} KC");
                }
                
                break;
        }

        if (!canAfford)
        {
            Debug.Log($"[ShopManager] Not enough {currency}. Need {price}");
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
            Debug.LogError("[ShopManager] PopupClaimQuest.Instance is null! Granting reward directly.");
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

        if (icon == null)
        {
            Debug.LogError($"[ShopManager] ICON IS NULL for {data.itemId}!");
        }

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
        string description = data.description ?? $"You will receive {bundleItems.Count} items";

        PopupClaimQuest.Instance.OpenBundle(
            bundleItems,
            title,
            description,
            () => CompletePurchase(data, currency)
        );
    }

    void CompletePurchase(ShopItemData data, Currency currency)
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null!");
            return;
        }

        bool deductSuccess = false;

        switch (currency)
        {
            case Currency.Coins:
                if (!PlayerEconomy.Instance.SpendCoins(data.coinPrice))
                {
                    Debug.LogError("[ShopManager] Failed to spend coins!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }
                deductSuccess = true;
                Debug.Log($"[ShopManager] ✓ Spent {data.coinPrice} Coins");
                break;

            case Currency.Shards:
                if (!PlayerEconomy.Instance.SpendShards(data.shardPrice))
                {
                    Debug.LogError("[ShopManager] Failed to spend shards!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }
                deductSuccess = true;
                Debug.Log($"[ShopManager] ✓ Spent {data.shardPrice} Shards");
                break;

            case Currency.KulinoCoin:
                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager null!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }

                if (!KulinoCoinManager.Instance.HasEnoughBalance(data.kulinoCoinPrice))
                {
                    Debug.LogError("[ShopManager] Insufficient Kulino Coin balance!");
                    SoundManager.Instance?.PlayPurchaseFail();
                    return;
                }

                Debug.Log($"[ShopManager] 💰 Initiating Phantom payment: {data.kulinoCoinPrice:F6} KC");
                
                _pendingPurchaseData = data;
                StartCoroutine(InitiatePhantomPayment(data, data.kulinoCoinPrice));
                
                return;
        }

        if (deductSuccess)
        {
            GrantReward(data);
            Debug.Log($"[ShopManager] ✓ Purchase completed: {data.displayName}");
        }
    }

    // ==================== PHANTOM PAYMENT ====================

    IEnumerator InitiatePhantomPayment(ShopItemData data, double kcAmount)
    {
        Debug.Log($"[ShopManager] 🚀 Starting Phantom payment: {kcAmount:F6} KC for {data.displayName}");
        
        var payload = new PaymentPayload
        {
            amount = kcAmount,
            itemId = data.itemId,
            itemName = data.displayName,
            nonce = System.Guid.NewGuid().ToString(),
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        string json = JsonUtility.ToJson(payload);
        Debug.Log($"[ShopManager] 📤 Payload: {json}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string jsCode = $"if(typeof requestKulinoCoinPayment === 'function') {{ requestKulinoCoinPayment('{json}'); }} else {{ console.error('requestKulinoCoinPayment not found'); }}";
            Application.ExternalEval(jsCode);
            Debug.Log("[ShopManager] ✓ Payment request sent to Phantom");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] ❌ Failed to call JavaScript: {ex.Message}");
            SoundManager.Instance?.PlayPurchaseFail();
            _pendingPurchaseData = null;
        }
#else
        Debug.Log("[ShopManager] 🧪 EDITOR MODE: Simulating Phantom payment...");
        yield return new WaitForSeconds(2f);
        
        string mockResponse = $"{{\"success\":true,\"txHash\":\"EDITOR_MOCK_TX_{System.DateTime.Now.Ticks}\",\"error\":null}}";
        Debug.Log($"[ShopManager] 🧪 Mock response: {mockResponse}");
        
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.OnPhantomPaymentResult(mockResponse);
            
            if (_pendingPurchaseData != null)
            {
                Debug.Log($"[ShopManager] 🧪 EDITOR: Granting reward");
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
        Debug.Log("[ShopManager] 💰 Payment confirmed! Granting reward...");
        
        if (_pendingPurchaseData != null)
        {
            GrantReward(_pendingPurchaseData);
            SoundManager.Instance?.PlayPurchaseSuccess();
            
            Debug.Log($"[ShopManager] ✓ Reward granted for {_pendingPurchaseData.displayName}");
            _pendingPurchaseData = null;
        }
        else
        {
            Debug.LogWarning("[ShopManager] ⚠️ No pending purchase data");
        }
    }

    // ==================== REWARD GRANTING ====================

    void GrantReward(ShopItemData data)
    {
        if (data == null) return;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null when granting reward!");
            return;
        }

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
                if (!string.IsNullOrEmpty(data.itemId))
                {
                    EnsureBoosterInventory();
                    BoosterInventory.Instance?.AddBooster(data.itemId, data.rewardAmount);
                }
                break;

            case ShopRewardType.Bundle:
                GrantBundleReward(data);
                break;

            default:
                Debug.LogWarning($"[ShopManager] Unknown reward type: {data.rewardType}");
                break;
        }

        Debug.Log($"[ShopManager] Granted reward {data.rewardType} for {data.displayName}");
    }

    void GrantBundleReward(ShopItemData data)
    {
        if (data?.bundleItems == null || data.bundleItems.Count == 0) return;

        foreach (var bundleItem in data.bundleItems)
        {
            if (bundleItem == null || string.IsNullOrEmpty(bundleItem.itemId)) continue;

            string id = bundleItem.itemId.ToLower().Trim();

            if (id == "coin" || id == "coins")
                PlayerEconomy.Instance.AddCoins(bundleItem.amount);
            else if (id == "shard" || id == "shards")
                PlayerEconomy.Instance.AddShards(bundleItem.amount);
            else if (id == "energy")
                PlayerEconomy.Instance.AddEnergy(bundleItem.amount);
            else
            {
                EnsureBoosterInventory();
                BoosterInventory.Instance?.AddBooster(bundleItem.itemId, bundleItem.amount);
            }
        }

        Debug.Log($"[ShopManager] Bundle '{data.displayName}' granted!");
    }

    // ==================== HELPER METHODS ====================

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
        Debug.Log("[ShopManager] Kulino Coin balance refreshed");
    }

    // ==================== CONTEXT MENU ====================

    [ContextMenu("🔄 Repopulate Shop")]
    void Context_Repopulate()
    {
        RepopulateWithCategories();
    }

    [ContextMenu("🧪 Test: ALL Filter")]
    void Context_TestAll()
    {
        FilterShop(ShopFilter.All);
    }

    [ContextMenu("🧪 Test: SHARD Filter")]
    void Context_TestShard()
    {
        FilterShop(ShopFilter.Shard);
    }
    
    [ContextMenu("🧪 Test: ITEMS Filter")]
    void Context_TestItems()
    {
        FilterShop(ShopFilter.Items);
    }
    
    [ContextMenu("🧪 Test: BUNDLE Filter")]
    void Context_TestBundle()
    {
        FilterShop(ShopFilter.Bundle);
    }

    // ==================== NESTED CLASSES ====================

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