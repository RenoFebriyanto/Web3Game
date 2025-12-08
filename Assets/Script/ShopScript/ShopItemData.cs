// ShopItemData.cs - UPDATED dengan Rupiah support
using UnityEngine;
using System.Collections.Generic;

public enum ShopRewardType { Energy, Coin, Shard, Booster, Bundle, None }

[System.Serializable]
public class BundleItem
{
    public string itemId;
    public Sprite icon;
    public int amount;
    public string displayName;
}

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Shop Item", order = 100)]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;

    [Header("Icons (Grid vs Preview)")]
    public Sprite iconGrid;
    public Sprite iconPreview;

    [Header("Pricing")]
    public long coinPrice = 0;
    public int shardPrice = 0;
    public bool allowBuyWithCoins = true;
    public bool allowBuyWithShards = false;

    [Header("💰 Rupiah Pricing (FOR SHARD ONLY)")]
    [Tooltip("Harga dalam Rupiah - HANYA untuk Shard items")]
    public double rupiahPrice = 0;
    
    [Tooltip("Boleh dibeli dengan Rupiah? (auto-convert ke KC)")]
    public bool allowBuyWithRupiah = false;

    [Header("💎 Kulino Coin Pricing (FOR OTHER ITEMS)")]
    [Tooltip("Harga dalam Kulino Coin - untuk items selain Shard")]
    public double kulinoCoinPrice = 0;
    
    [Tooltip("Boleh dibeli dengan Kulino Coin?")]
    public bool allowBuyWithKulinoCoin = false;

    [Header("Reward (what player gets when bought)")]
    public ShopRewardType rewardType = ShopRewardType.Energy;
    public int rewardAmount = 0;

    [Header("Bundle Settings (only if rewardType = Bundle)")]
    public List<BundleItem> bundleItems = new List<BundleItem>();

    [TextArea(2, 4)]
    public string description;

    // Helper: check if this is a bundle
    public bool IsBundle => rewardType == ShopRewardType.Bundle && bundleItems != null && bundleItems.Count > 0;
    
    // ✅ NEW: Helper untuk check apakah item ini pakai Rupiah
    public bool UseRupiahPricing => rewardType == ShopRewardType.Shard && allowBuyWithRupiah && rupiahPrice > 0;
}