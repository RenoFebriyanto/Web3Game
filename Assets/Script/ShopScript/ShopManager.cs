// Assets/Script/ShopScript/ShopManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Currency { Coins, Shards }

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

    List<ShopItemUI> spawned = new List<ShopItemUI>();

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

    public bool TryBuy(ShopItemData data, Currency currency)
    {
        if (data == null) return false;

        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("[ShopManager] PlayerEconomy.Instance is null. Ensure PlayerEconomy exists and its Awake ran before buying.");
            return false;
        }

        if (currency == Currency.Coins)
        {
            if (!data.allowBuyWithCoins) return false;
            bool ok = PlayerEconomy.Instance.SpendCoins(data.coinPrice);
            if (!ok) { Debug.Log("[ShopManager] Not enough coins."); return false; }
            GrantReward(data);
            buyPreviewUI?.Close();
            return true;
        }

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

        EnsureBoosterInventory();

        if (BoosterInventory.Instance == null)
        {
            Debug.LogError("[ShopManager] BoosterInventory.Instance is null. Cannot grant bundle items.");
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

            BoosterInventory.Instance.AddBooster(bundleItem.itemId, bundleItem.amount);
            Debug.Log($"  + {bundleItem.amount}x {bundleItem.itemId} ({bundleItem.displayName})");
        }
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
}