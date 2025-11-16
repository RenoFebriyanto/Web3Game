// ShopItemData.cs - SUDAH BAGUS, HANYA TAMBAHKAN COMMENT
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

    [Header("💎 Kulino Coin Pricing")]
    [Tooltip("Harga dalam Kulino Coin (0 = tidak bisa beli dengan Kulino Coin)")]
    public double kulinoCoinPrice = 0;

    [Tooltip("Boleh dibeli dengan Kulino Coin dari wallet?")]
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
}