// Assets/Script/ShopScript/ShopManager.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Currency enum (pastikan hanya satu definisi Currency ada di project).
/// </summary>
public enum Currency { Coins, Shards }

/// <summary>
/// Manages shop UI population. Uses ShopDatabase if assigned, otherwise falls back to manual shopItems list.
/// Prefab itemUIPrefab is instantiated as children of itemsParent and must contain ShopItemUI component.
/// </summary>
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
        // try to auto-find BuyPreviewController if missing
        if (buyPreviewUI == null)
        {
            buyPreviewUI = FindObjectOfType<BuyPreviewController>();
        }
    }

    void Start()
    {
        if (buyPreviewUI != null) buyPreviewUI.Initialize(this);
        PopulateShop();
    }

    /// <summary>
    /// Populate the itemsParent with UI entries for each ShopItemData found in database (preferred) or shopItems.
    /// </summary>
    public void PopulateShop()
    {
        // safety checks
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

        // clear existing children (safe reverse loop)
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
            go.name = $"ShopItem_{(string.IsNullOrEmpty(data.itemId) ? data.displayName : data.itemId)}";
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
            {
                ui.Setup(data, this);
                spawned.Add(ui);
            }
            else
            {
                Debug.LogWarning("[ShopManager] itemUIPrefab does not contain ShopItemUI component: " + itemUIPrefab.name);
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
        }

        Debug.Log($"[ShopManager] Granted reward {data.rewardType} x{data.rewardAmount}");
    }
}
