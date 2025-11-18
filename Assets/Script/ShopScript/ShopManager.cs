using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Currency { Coins, Shards, KulinoCoin }

/// <summary>
/// UPDATED ShopManager dengan Category Filters (Coin, Shard, Energy, Booster, Bundle)
/// Version: 3.0 - Category Header Support
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Prefabs & UI")]
    [Tooltip("Prefab for one shop entry (BackgroundItem). Must have ShopItemUI component.")]
    public GameObject itemUIPrefab;
    [Tooltip("Parent transform (ItemsShop) where items will be instantiated.")]
    public Transform itemsParent;
    [Tooltip("Popup controller for buy preview (optional but recommended).")]
    public BuyPreviewController buyPreviewUI;

    [Header("✨ NEW: Category Header")]
    [Tooltip("GameObject untuk header kategori (contoh: panel dengan Text 'Shard')")]
    public GameObject categoryHeader;
    [Tooltip("Text component untuk menampilkan nama kategori")]
    public TMP_Text categoryHeaderText;

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

    // ✅ UPDATED: Added new filter types
    public enum ShopFilter { All, Coin, Shard, Energy, Booster, Bundle }

    // Private variables
    private ShopItemData _pendingPurchaseData;
    private List<ShopItemUI> spawned = new List<ShopItemUI>();
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

        // ✅ Hide category header initially
        if (categoryHeader != null)
        {
            categoryHeader.SetActive(false);
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
        Debug.Log($"[ShopManager] PopulateShop called. itemUIPrefab={(itemUIPrefab ? itemUIPrefab.name : "NULL")}, itemsParent={(itemsParent ? itemsParent.name : "NULL")}");

        if (itemUIPrefab == null)
        {
            Debug.LogError("[ShopManager] itemUIPrefab is not assigned!");
            return;
        }
        if (itemsParent == null)
        {
            Debug.LogError("[ShopManager] itemsParent is not assigned!");
            return;
        }

        spawned.Clear();
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            var child = itemsParent.GetChild(i);
            Destroy(child.gameObject);
        }

        List<ShopItemData> source = null;
        if (database != null && database.items != null && database.items.Count > 0)
        {
            source = database.items;
        }
        else if (shopItems != null && shopItems.Count > 0)
        {
            source = shopItems;
        }

        if (source == null || source.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No items assigned in database or manual list.");
            return;
        }

        foreach (var data in source)
        {
            if (data == null) continue;

            var go = Instantiate(itemUIPrefab, itemsParent);
            go.name = $"ShopItem_{(string.IsNullOrEmpty(data.itemId) ? data.displayName : data.itemId)}";

            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
            {
                try
                {
                    ui.Setup(data, this);
                    spawned.Add(ui);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ShopManager] Exception while calling Setup on ShopItemUI ({go.name}): {ex.Message}");
                }
            }
            else
            {
                ui = go.GetComponentInChildren<ShopItemUI>(true);
                if (ui != null)
                {
                    try
                    {
                        ui.Setup(data, this);
                        spawned.Add(ui);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ShopManager] Exception while calling Setup on child ShopItemUI: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[ShopManager] Could not find ShopItemUI component");
                }
            }
        }

        Debug.Log($"[ShopManager] Populated {spawned.Count} shop items");

        // ✅ Hide header untuk "All" filter
        UpdateCategoryHeader(ShopFilter.All);
    }

    // ==================== ✅ NEW: SHOP FILTERING WITH CATEGORIES ====================

    public void ShowAll() { FilterShop(ShopFilter.All); }
    public void ShowCoin() { FilterShop(ShopFilter.Coin); }
    public void ShowShard() { FilterShop(ShopFilter.Shard); }
    public void ShowEnergy() { FilterShop(ShopFilter.Energy); }
    public void ShowBooster() { FilterShop(ShopFilter.Booster); }
    public void ShowBundle() { FilterShop(ShopFilter.Bundle); }

    public void FilterShop(ShopFilter filter)
    {
        currentFilter = filter;
        Debug.Log($"[ShopManager] FilterShop: {filter}");
        RepopulateWithFilter();
        UpdateCategoryHeader(filter);
    }

    void RepopulateWithFilter()
    {
        spawned.Clear();
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsParent.GetChild(i).gameObject);
        }

        List<ShopItemData> source = database?.items ?? shopItems;
        if (source == null || source.Count == 0) return;

        List<ShopItemData> filteredItems = FilterItems(source);

        foreach (var data in filteredItems)
        {
            if (data == null) continue;

            var go = Instantiate(itemUIPrefab, itemsParent);
            var ui = go.GetComponent<ShopItemUI>() ?? go.GetComponentInChildren<ShopItemUI>(true);
            
            if (ui != null)
            {
                ui.Setup(data, this);
                spawned.Add(ui);
            }
        }

        Debug.Log($"[ShopManager] Filtered {spawned.Count} items (filter: {currentFilter})");
    }

    List<ShopItemData> FilterItems(List<ShopItemData> source)
    {
        List<ShopItemData> filtered = new List<ShopItemData>();

        foreach (var data in source)
        {
            if (data == null) continue;

            switch (currentFilter)
            {
                case ShopFilter.All:
                    filtered.Add(data);
                    break;

                case ShopFilter.Coin:
                    if (data.rewardType == ShopRewardType.Coin)
                        filtered.Add(data);
                    break;

                case ShopFilter.Shard:
                    if (data.rewardType == ShopRewardType.Shard)
                        filtered.Add(data);
                    break;

                case ShopFilter.Energy:
                    if (data.rewardType == ShopRewardType.Energy)
                        filtered.Add(data);
                    break;

                case ShopFilter.Booster:
                    if (data.rewardType == ShopRewardType.Booster)
                        filtered.Add(data);
                    break;

                case ShopFilter.Bundle:
                    if (data.rewardType == ShopRewardType.Bundle)
                        filtered.Add(data);
                    break;
            }
        }

        return filtered;
    }

    // ✅ NEW: Update Category Header
    void UpdateCategoryHeader(ShopFilter filter)
    {
        if (categoryHeader == null || categoryHeaderText == null)
        {
            return;
        }

        // Hide header untuk "All" filter
        if (filter == ShopFilter.All)
        {
            categoryHeader.SetActive(false);
            return;
        }

        // Show header dan set text
        categoryHeader.SetActive(true);

        string headerText = filter switch
        {
            ShopFilter.Coin => "COIN",
            ShopFilter.Shard => "SHARD",
            ShopFilter.Energy => "ENERGY",
            ShopFilter.Booster => "BOOSTER",
            ShopFilter.Bundle => "BUNDLE",
            _ => ""
        };

        categoryHeaderText.text = headerText;
        Debug.Log($"[ShopManager] Category header updated: {headerText}");
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

    [ContextMenu("Repopulate Shop")]
    void Context_Repopulate()
    {
        PopulateShop();
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