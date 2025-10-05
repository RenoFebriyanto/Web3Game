// Assets/Script/ShopScript/ShopManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Currency { Coins, Shards }

public class ShopManager : MonoBehaviour
{
    [Header("Prefabs & UI")]
    [Tooltip("Prefab for one shop entry (BackgroundItem). Must have ShopItemUI component.")]
    public GameObject itemUIPrefab;     // BackgroundItem prefab (must contain ShopItemUI)
    [Tooltip("Parent transform (ItemsShop) where items will be instantiated.")]
    public Transform itemsParent;       // ItemsShop (container)
    [Tooltip("Popup controller for buy preview (optional but recommended).")]
    public BuyPreviewController buyPreviewUI;   // assign in inspector (preview popup controller)

    [Header("Data")]
    [Tooltip("Optional: ShopDatabase ScriptableObject with items array. If null, uses shopItems list.")]
    public ShopDatabase database;
    [Tooltip("Fallback manual list of ShopItemData if no database assigned.")]
    public List<ShopItemData> shopItems = new List<ShopItemData>(); // fallback/manual list

    List<ShopItemUI> spawned = new List<ShopItemUI>();

    void Awake()
    {
        // auto-find BuyPreviewController if not assigned in inspector
        if (buyPreviewUI == null)
        {
            try
            {
                buyPreviewUI = UnityEngine.Object.FindFirstObjectByType<BuyPreviewController>();
            }
            catch { /* older Unitysafety: ignore if API unavailable */ }
        }
    }

    void Start()
    {
        // initialize preview controller if found
        if (buyPreviewUI != null)
        {
            try { buyPreviewUI.Initialize(this); }
            catch (Exception ex) { Debug.LogWarning("[ShopManager] buyPreviewUI.Initialize threw: " + ex.Message); }
        }

        PopulateShop();
    }

    /// <summary>
    /// Populate the itemsParent with UI entries for each ShopItemData found in database (preferred) or shopItems.
    /// </summary>
    public void PopulateShop()
    {
        Debug.Log($"[ShopManager] PopulateShop called. itemUIPrefab={(itemUIPrefab ? itemUIPrefab.name : "NULL")}, itemsParent={(itemsParent ? itemsParent.name : "NULL")}, database={(database ? "present" : "null")}, shopItemsCount={(shopItems != null ? shopItems.Count : 0)}");

        // safety checks
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

        // clear existing children
        spawned.Clear();
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            var child = itemsParent.GetChild(i);
            Destroy(child.gameObject);
        }

        // choose data source: database preferred
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

        // instantiate UI entries
        foreach (var data in source)
        {
            if (data == null) continue;

            var go = Instantiate(itemUIPrefab, itemsParent);
            // give distinct name so it's easy to find in Hierarchy at runtime
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

    // TryBuy called by BuyPreview buttons
    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null. Ensure PlayerEconomy exists and its Awake ran before buying.");
            return false;
        }

        // Coins
        if (currency == Currency.Coins)
        {
            if (!data.allowBuyWithCoins) return false;
            bool ok = PlayerEconomy.Instance.SpendCoins(data.coinPrice);
            if (!ok) { Debug.Log("[ShopManager] Not enough coins."); return false; }
            GrantReward(data);
            buyPreviewUI?.Close();
            return true;
        }
        // Shards
        if (currency == Currency.Shards)
        {
            if (!data.allowBuyWithShards) return false;
            bool ok = PlayerEconomy.Instance.SpendShards(data.shardPrice);
            if (!ok) { Debug.Log("[ShopManager] Not enough shards."); return false; }
            GrantReward(data);
            buyPreviewUI?.Close();
            return true;
        }

        return false;
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
                    if (BoosterInventory.Instance == null)
                    {
                        var go = new GameObject("BoosterInventory");
                        go.AddComponent<BoosterInventory>();
                    }
                    // safety check
                    if (BoosterInventory.Instance != null)
                    {
                        BoosterInventory.Instance.AddBooster(data.itemId, data.rewardAmount);
                    }
                    else
                    {
                        Debug.LogError("[ShopManager] Could not create BoosterInventory instance.");
                    }
                }
                break;
            default:
                Debug.LogWarning("[ShopManager] Unknown reward type: " + data.rewardType);
                break;
        }

        Debug.Log($"[ShopManager] Granted reward {data.rewardType} x{data.rewardAmount} for {data.itemId ?? data.displayName}");
    }

    // helper for editor/testing
    [ContextMenu("Repopulate Shop")]
    void Context_Repopulate()
    {
        PopulateShop();
    }
}
