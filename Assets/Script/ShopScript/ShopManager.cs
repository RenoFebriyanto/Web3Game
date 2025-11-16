using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Currency { Coins, Shards, KulinoCoin }

/// <summary>
/// FULL COMPLETE ShopManager dengan Kulino Coin support
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

    List<ShopItemUI> spawned = new List<ShopItemUI>();

    public enum ShopFilter { All, Items, Bundle }
    private ShopFilter currentFilter = ShopFilter.All;

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

    public void PopulateShop()
    {
        Debug.Log($"[ShopManager] PopulateShop called. itemUIPrefab={(itemUIPrefab ? itemUIPrefab.name : "NULL")}, itemsParent={(itemsParent ? itemsParent.name : "NULL")}, database={(database ? "present" : "null")}, shopItemsCount={(shopItems != null ? shopItems.Count : 0)}");

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
                    Debug.LogError($"[ShopManager] Exception while calling Setup on ShopItemUI ({go.name}): {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogWarning("[ShopManager] itemUIPrefab does not contain ShopItemUI component (root). Searching children...");
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
                        Debug.LogError($"[ShopManager] Exception while calling Setup on child ShopItemUI ({go.name}): {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogWarning("[ShopManager] Still could not find ShopItemUI on prefab or its children: " + itemUIPrefab.name);
                }
            }
        }

        Debug.Log($"[ShopManager] Populated {spawned.Count} shop items (source: {(database != null ? "database" : "manual list")}).");
    }

    public void ShowBuyPreview(ShopItemData data, ShopItemUI fromUI = null)
    {
        if (buyPreviewUI != null) buyPreviewUI.Show(data);
        else Debug.LogWarning("[ShopManager] buyPreviewUI not assigned. Cannot show preview.");
    }

    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null. Attempting to create...");
            EnsurePlayerEconomy();

            if (PlayerEconomy.Instance == null)
            {
                Debug.LogError("[ShopManager] Failed to create PlayerEconomy! Cannot proceed with purchase.");

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPurchaseFail();
                }

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
                Debug.Log($"[ShopManager] TryBuy with Coins: has={PlayerEconomy.Instance.Coins}, need={price}, canAfford={canAfford}");
                break;

            case Currency.Shards:
                if (!data.allowBuyWithShards) return false;
                price = data.shardPrice;
                canAfford = PlayerEconomy.Instance.Shards >= price;
                Debug.Log($"[ShopManager] TryBuy with Shards: has={PlayerEconomy.Instance.Shards}, need={price}, canAfford={canAfford}");
                break;

            case Currency.KulinoCoin:
                if (!data.allowBuyWithKulinoCoin)
                {
                    Debug.LogWarning("[ShopManager] This item cannot be purchased with Kulino Coin!");

                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseFail();
                    }

                    return false;
                }

                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager.Instance is null! Cannot check Kulino Coin balance.");

                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseFail();
                    }

                    return false;
                }

                price = data.kulinoCoinPrice;
                canAfford = KulinoCoinManager.Instance.HasEnoughBalance(price);
                Debug.Log($"[ShopManager] TryBuy with Kulino Coin: has={KulinoCoinManager.Instance.GetBalance():F6}, need={price:F6}, canAfford={canAfford}");
                break;

            default:
                Debug.LogError($"[ShopManager] Unknown currency type: {currency}");
                return false;
        }

        if (!canAfford)
        {
            Debug.Log($"[ShopManager] Not enough {currency}. Need {price}");

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPurchaseFail();
            }

            return false;
        }

        buyPreviewUI?.Close();

        ShowPurchasePopup(data, currency);

        return true;
    }

    // ShopManager.cs - ADD THIS METHOD
IEnumerator InitiatePhantomPayment(ShopItemData data, double kcAmount)
{
    Debug.Log($"[ShopManager] Initiating Phantom payment: {kcAmount:F6} KC");

    // Prepare payload untuk Phantom
    var payload = new
    {
        type = "payment",
        amount = kcAmount,
        itemId = data.itemId,
        itemName = data.displayName,
        recipient = "KULINO_TREASURY_WALLET_ADDRESS", // TODO: Ganti dengan wallet Kulino
        nonce = System.Guid.NewGuid().ToString(),
        timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    string json = JsonUtility.ToJson(payload);

    // Call JavaScript function (dari web integration)
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalEval($"requestKulinoCoinPayment('{json}')");
#else
    Debug.Log($"[ShopManager] [EDITOR] Would call JS: requestKulinoCoinPayment({json})");
    
    // Simulate success untuk testing
    yield return new WaitForSeconds(1f);
    OnPhantomPaymentComplete(true, data);
#endif

    yield return null;
}

/// <summary>
/// Callback dari JavaScript setelah payment complete
/// </summary>
public void OnPhantomPaymentComplete(bool success, ShopItemData data)
{
    if (success)
    {
        Debug.Log("[ShopManager] ✓ Payment successful!");
        
        // Grant reward
        GrantReward(data);
        
        // Refresh balance
        if (KulinoCoinManager.Instance != null)
        {
            StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
        }

        // Success notification
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }
    }
    else
    {
        Debug.LogError("[ShopManager] ✗ Payment failed!");
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseFail();
        }
    }
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
        {
            ShowBundlePurchasePopup(data, currency);
        }
        else
        {
            ShowSingleItemPurchasePopup(data, currency);
        }
    }

    void ShowSingleItemPurchasePopup(ShopItemData data, Currency currency)
    {
        Sprite icon = GetItemIcon(data);
        string amountText = GetItemAmountText(data);
        string title = $"Purchase {data.displayName}";

        if (icon == null)
        {
            Debug.LogError($"[ShopManager] ✗✗✗ ICON IS NULL for {data.itemId}! ✗✗✗");
            Debug.LogError($"[ShopManager] iconPreview: {(data.iconPreview != null ? data.iconPreview.name : "NULL")}");
            Debug.LogError($"[ShopManager] iconGrid: {(data.iconGrid != null ? data.iconGrid.name : "NULL")}");
        }
        else
        {
            Debug.Log($"[ShopManager] ✓ Sending icon to popup: {icon.name}");
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

        Debug.Log($"[ShopManager] ShowBundlePurchasePopup: {data.bundleItems.Count} items in bundle");

        foreach (var item in data.bundleItems)
        {
            if (item == null)
            {
                Debug.LogWarning("[ShopManager] Bundle item is null, skipping");
                continue;
            }

            Sprite itemIcon = item.icon;

            if (itemIcon == null)
            {
                Debug.LogWarning($"[ShopManager] Bundle item {item.itemId} has no icon assigned!");
                continue;
            }

            bundleItems.Add(new BundleItemData(
                itemIcon,
                item.amount,
                item.displayName
            ));

            Debug.Log($"[ShopManager] Added bundle item: {item.displayName}, icon={itemIcon.name}, amount={item.amount}");
        }

        string title = $"Purchase {data.displayName}";
        string description = data.description ?? $"You will receive {bundleItems.Count} items";

        Debug.Log($"[ShopManager] Opening bundle popup with {bundleItems.Count} items");

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
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null in CompletePurchase!");
            EnsurePlayerEconomy();

            if (PlayerEconomy.Instance == null)
            {
                Debug.LogError("[ShopManager] Failed to create PlayerEconomy! Cannot complete purchase.");
                return;
            }
        }

        bool deductSuccess = false;

        switch (currency)
        {
            case Currency.Coins:
                bool okCoins = PlayerEconomy.Instance.SpendCoins(data.coinPrice);
                if (!okCoins)
                {
                    Debug.LogError("[ShopManager] Failed to spend coins!");
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseFail();
                    }
                    return;
                }
                deductSuccess = true;
                Debug.Log($"[ShopManager] ✓ Spent {data.coinPrice} Coins");
                break;

            case Currency.Shards:
                bool okShards = PlayerEconomy.Instance.SpendShards(data.shardPrice);
                if (!okShards)
                {
                    Debug.LogError("[ShopManager] Failed to spend shards!");
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseFail();
                    }
                    return;
                }
                deductSuccess = true;
                Debug.Log($"[ShopManager] ✓ Spent {data.shardPrice} Shards");
                break;

            case Currency.KulinoCoin:
                if (KulinoCoinManager.Instance == null)
                {
                    Debug.LogError("[ShopManager] KulinoCoinManager.Instance is null!");
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseFail();
                    }
                    return;
                }

                if (!KulinoCoinManager.Instance.HasEnoughBalance(data.kulinoCoinPrice))
                {
                    Debug.LogError("[ShopManager] Insufficient Kulino Coin balance!");
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayPurchaseFail();
                    }
                    return;
                }

                Debug.LogWarning("[ShopManager] ⚠️ DEVELOPMENT MODE: Kulino Coin purchase approved (LOCAL CHECK ONLY)");
                Debug.LogWarning("[ShopManager] ⚠️ TODO: Implement actual blockchain transaction for production!");

                deductSuccess = true;
                Debug.Log($"[ShopManager] ✓ Kulino Coin purchase approved: {data.kulinoCoinPrice:F6} KC");
                break;

            default:
                Debug.LogError($"[ShopManager] Unknown currency type: {currency}");
                return;
        }

        if (!deductSuccess)
        {
            Debug.LogError("[ShopManager] Failed to deduct currency!");
            return;
        }

        GrantReward(data);

        if (currency == Currency.KulinoCoin && KulinoCoinManager.Instance != null)
        {
            Debug.Log("[ShopManager] Refreshing Kulino Coin balance from blockchain...");
            StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
        }

        Debug.Log($"[ShopManager] ✓ Purchase completed: {data.displayName}");
    }

    System.Collections.IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
            Debug.Log("[ShopManager] Kulino Coin balance refreshed");
        }
    }

    Sprite GetItemIcon(ShopItemData data)
    {
        if (data == null) return null;

        if (data.iconPreview != null)
        {
            Debug.Log($"[ShopManager] Using iconPreview for {data.itemId}: {data.iconPreview.name}");
            return data.iconPreview;
        }

        if (data.iconGrid != null)
        {
            Debug.Log($"[ShopManager] Using iconGrid for {data.itemId}: {data.iconGrid.name}");
            return data.iconGrid;
        }

        switch (data.rewardType)
        {
            case ShopRewardType.Coin:
                return iconCoin;
            case ShopRewardType.Shard:
                return iconShard;
            case ShopRewardType.Energy:
                return iconEnergy;
            default:
                Debug.LogWarning($"[ShopManager] No icon found for {data.itemId}");
                return null;
        }
    }

    string GetItemAmountText(ShopItemData data)
    {
        if (data.rewardAmount <= 0) return "";

        if (data.rewardType == ShopRewardType.Booster)
        {
            return $"x{data.rewardAmount}";
        }

        return data.rewardAmount.ToString("N0");
    }

    void GrantReward(ShopItemData data)
    {
        if (data == null) return;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null when granting reward. Aborting grant.");
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
                if (string.IsNullOrEmpty(data.itemId))
                {
                    Debug.LogWarning("[ShopManager] Booster item has no itemId set in ShopItemData.");
                }
                else
                {
                    EnsureBoosterInventory();
                    if (BoosterInventory.Instance != null)
                    {
                        BoosterInventory.Instance.AddBooster(data.itemId, data.rewardAmount);
                    }
                }
                break;

            case ShopRewardType.Bundle:
                GrantBundleReward(data);
                break;

            default:
                Debug.LogWarning("[ShopManager] Unknown reward type: " + data.rewardType);
                break;
        }

        Debug.Log($"[ShopManager] Granted reward {data.rewardType} for {data.itemId ?? data.displayName}");
    }

    void GrantBundleReward(ShopItemData data)
    {
        if (data == null || data.bundleItems == null || data.bundleItems.Count == 0)
        {
            Debug.LogWarning("[ShopManager] Bundle has no items!");
            return;
        }

        Debug.Log($"[ShopManager] Granting bundle '{data.displayName}' with {data.bundleItems.Count} items:");

        foreach (var bundleItem in data.bundleItems)
        {
            if (bundleItem == null || string.IsNullOrEmpty(bundleItem.itemId))
            {
                Debug.LogWarning("[ShopManager] Bundle item has no itemId, skipping.");
                continue;
            }

            string id = bundleItem.itemId.ToLower().Trim();

            if (id == "coin" || id == "coins")
            {
                PlayerEconomy.Instance.AddCoins(bundleItem.amount);
                Debug.Log($"  ✓ Added {bundleItem.amount} Coins to PlayerEconomy");
            }
            else if (id == "shard" || id == "shards")
            {
                PlayerEconomy.Instance.AddShards(bundleItem.amount);
                Debug.Log($"  ✓ Added {bundleItem.amount} Shards to PlayerEconomy");
            }
            else if (id == "energy")
            {
                PlayerEconomy.Instance.AddEnergy(bundleItem.amount);
                Debug.Log($"  ✓ Added {bundleItem.amount} Energy to PlayerEconomy");
            }
            else
            {
                EnsureBoosterInventory();

                if (BoosterInventory.Instance != null)
                {
                    BoosterInventory.Instance.AddBooster(bundleItem.itemId, bundleItem.amount);
                    Debug.Log($"  ✓ Added {bundleItem.amount}x {bundleItem.itemId} to BoosterInventory");
                }
                else
                {
                    Debug.LogError("[ShopManager] BoosterInventory.Instance is null after ensure!");
                }
            }
        }

        Debug.Log($"[ShopManager] Bundle '{data.displayName}' granted successfully!");
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

    [ContextMenu("Repopulate Shop")]
    void Context_Repopulate()
    {
        PopulateShop();
    }

    public void FilterShop(ShopFilter filter)
    {
        currentFilter = filter;
        Debug.Log($"[ShopManager] FilterShop called: {filter}");
        RepopulateWithFilter();
    }

    void RepopulateWithFilter()
    {
        Debug.Log($"[ShopManager] RepopulateWithFilter: {currentFilter}");

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
            Debug.LogWarning("[ShopManager] No items in database or manual list.");
            return;
        }

        List<ShopItemData> filteredItems = FilterItems(source);

        Debug.Log($"[ShopManager] Filtered {filteredItems.Count} items from {source.Count} total (filter: {currentFilter})");

        foreach (var data in filteredItems)
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
                        Debug.LogError($"[ShopManager] Exception while calling Setup on child ShopItemUI ({go.name}): {ex.Message}");
                    }
                }
            }
        }

        Debug.Log($"[ShopManager] Populated {spawned.Count} shop items (filter: {currentFilter})");
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

                case ShopFilter.Items:
                    if (data.rewardType != ShopRewardType.Bundle)
                    {
                        filtered.Add(data);
                    }
                    break;

                case ShopFilter.Bundle:
                    if (data.rewardType == ShopRewardType.Bundle)
                    {
                        filtered.Add(data);
                    }
                    break;
            }
        }

        return filtered;
    }

    public void ShowAll()
    {
        FilterShop(ShopFilter.All);
    }

    public void ShowItems()
    {
        FilterShop(ShopFilter.Items);
    }

    public void ShowBundle()
    {
        FilterShop(ShopFilter.Bundle);
    }
}