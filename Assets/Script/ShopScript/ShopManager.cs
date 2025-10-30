// Assets/Script/ShopScript/ShopManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Currency { Coins, Shards }

/// <summary>
/// UPDATED: Integration dengan PopupClaimQuest untuk purchase flow.
/// Flow: ShopItem → BuyPreview → PopupClaimQuest → Grant Reward
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

    [Header("Icons for Economy Items (untuk bundle display)")]
    [Tooltip("Icon untuk Coins (dipakai di PopupClaimQuest bundle)")]
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

    public void PopulateShop()
    {
        Debug.Log($"[ShopManager] PopulateShop called. itemUIPrefab={(itemUIPrefab ? itemUIPrefab.name : "NULL")}, itemsParent={(itemsParent ? itemsParent.name : "NULL")}, database={(database ? "present" : "null")}, shopItemsCount={(shopItems != null ? shopItems.Count : 0)}");

        if (itemUIPrefab == null)
        {
            Debug.LogError("[ShopManager] itemUIPrefab is not assigned! Drag the BackgroundItem prefab into inspector.");
            return;
        }
        if (itemsParent == null)
        {
            Debug.LogError("[ShopManager] itemsParent is not assigned! Drag the ItemsShop (content transform) into inspector.");
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

    // ========================================
    // UPDATED: Purchase Flow dengan PopupClaimQuest
    // ========================================

    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null.");
            return false;
        }

        // Check if player has enough currency
        bool canAfford = false;
        long price = 0;

        if (currency == Currency.Coins)
        {
            if (!data.allowBuyWithCoins) return false;
            price = data.coinPrice;
            canAfford = PlayerEconomy.Instance.Coins >= price;
        }
        else if (currency == Currency.Shards)
        {
            if (!data.allowBuyWithShards) return false;
            price = data.shardPrice;
            canAfford = PlayerEconomy.Instance.Shards >= price;
        }

        if (!canAfford)
        {
            Debug.Log($"[ShopManager] Not enough {currency}. Need {price}");

            // Play fail sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPurchaseFail();
            }

            return false;
        }

        // PURCHASE FLOW: BuyPreview → PopupClaimQuest → Grant Reward
        // Close BuyPreview first
        buyPreviewUI?.Close();

        // Show PopupClaimQuest dengan items yang akan didapat
        ShowPurchasePopup(data, currency);

        return true;
    }







    // REPLACE bagian ShowPurchasePopup() sampai GetBundleItemIcon() di ShopManager.cs
    // dengan kode ini:

    /// <summary>
    /// Show PopupClaimQuest dengan items yang dibeli
    /// </summary>
    void ShowPurchasePopup(ShopItemData data, Currency currency)
    {
        if (PopupClaimQuest.Instance == null)
        {
            Debug.LogError("[ShopManager] PopupClaimQuest.Instance is null! Granting reward directly.");
            CompletePurchase(data, currency);
            return;
        }

        // Check if bundle or single
        if (data.IsBundle)
        {
            // BUNDLE: Show multiple items
            ShowBundlePurchasePopup(data, currency);
        }
        else
        {
            // SINGLE ITEM: Show single icon + amount
            ShowSingleItemPurchasePopup(data, currency);
        }
    }

    /// <summary>
    /// Show popup untuk single item purchase
    /// </summary>
    void ShowSingleItemPurchasePopup(ShopItemData data, Currency currency)
    {
        Sprite icon = GetItemIcon(data);
        string amountText = GetItemAmountText(data);
        string title = $"Purchase {data.displayName}";

        // DEBUG: Check icon
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

    /// <summary>
    /// Show popup untuk bundle purchase dengan multiple items
    /// </summary>
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

            Sprite itemIcon = GetBundleItemIcon(item);

            if (itemIcon != null)
            {
                bundleItems.Add(new BundleItemData(
                    itemIcon,
                    item.amount,
                    item.displayName
                ));
                Debug.Log($"[ShopManager] Added bundle item: {item.displayName}, icon={itemIcon.name}, amount={item.amount}");
            }
            else
            {
                Debug.LogWarning($"[ShopManager] No icon found for bundle item: {item.itemId}");
            }
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

    /// <summary>
    /// Get icon untuk item (single item atau booster)
    /// </summary>
    Sprite GetItemIcon(ShopItemData data)
    {
        // Priority: iconPreview → iconGrid
        if (data.iconPreview != null) return data.iconPreview;
        if (data.iconGrid != null) return data.iconGrid;

        // Fallback untuk economy items
        switch (data.rewardType)
        {
            case ShopRewardType.Coin:
                return iconCoin;
            case ShopRewardType.Shard:
                return iconShard;
            case ShopRewardType.Energy:
                return iconEnergy;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get icon untuk bundle item (bisa economy atau booster)
    /// </summary>
    Sprite GetBundleItemIcon(BundleItem item)
    {
        // Jika item punya icon sendiri, pakai itu
        if (item.icon != null)
        {
            Debug.Log($"[ShopManager] Using assigned icon for {item.itemId}: {item.icon.name}");
            return item.icon;
        }

        // Fallback: detect dari itemId
        string id = item.itemId.ToLower().Trim();

        // Economy items
        if (id == "coin" || id == "coins") return iconCoin;
        if (id == "shard" || id == "shards") return iconShard;
        if (id == "energy") return iconEnergy;

        // Booster items - cari dari database
        if (database != null)
        {
            foreach (var shopItem in database.items)
            {
                if (shopItem == null) continue;
                if (shopItem.itemId.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    Sprite foundIcon = shopItem.iconPreview != null ? shopItem.iconPreview : shopItem.iconGrid;
                    Debug.Log($"[ShopManager] Found icon from database for {item.itemId}: {(foundIcon != null ? foundIcon.name : "NULL")}");
                    return foundIcon;
                }
            }
        }

        Debug.LogWarning($"[ShopManager] No icon found for bundle item: {item.itemId}");
        return null;
    }

    /// <summary>
    /// Get amount text untuk single item
    /// </summary>
    string GetItemAmountText(ShopItemData data)
    {
        if (data.rewardAmount <= 0) return "";

        // Untuk booster, tampilkan "x5" style
        if (data.rewardType == ShopRewardType.Booster)
        {
            return $"x{data.rewardAmount}";
        }

        // Untuk currency, tampilkan angka dengan format
        return data.rewardAmount.ToString("N0");
    }

    /// <summary>
    /// Complete purchase: deduct currency & grant reward
    /// Called from PopupClaimQuest confirm button
    /// </summary>
    void CompletePurchase(ShopItemData data, Currency currency)
    {
        // Deduct currency
        if (currency == Currency.Coins)
        {
            bool ok = PlayerEconomy.Instance.SpendCoins(data.coinPrice);
            if (!ok)
            {
                Debug.LogError("[ShopManager] Failed to spend coins!");
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPurchaseFail();
                }
                return;
            }
        }
        else if (currency == Currency.Shards)
        {
            bool ok = PlayerEconomy.Instance.SpendShards(data.shardPrice);
            if (!ok)
            {
                Debug.LogError("[ShopManager] Failed to spend shards!");
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPurchaseFail();
                }
                return;
            }
        }

        // Grant reward
        GrantReward(data);

        Debug.Log($"[ShopManager] Purchase completed: {data.displayName}");
    }

    // ========================================
    // Reward Granting (unchanged)
    // ========================================

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

            // Handle special currency items
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
                // Regular booster - masuk ke BoosterInventory
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

    // ========================================
    // Filter Methods (unchanged)
    // ========================================

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