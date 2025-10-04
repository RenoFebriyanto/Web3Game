// ShopItemData.cs
using UnityEngine;

public enum ShopRewardType { Energy, Coin, Shard, None }

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Shop Item", order = 100)]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;

    [Header("Icons (Grid vs Preview)")]
    public Sprite iconGrid;      // dipakai di ItemsShop grid (kecil)
    public Sprite iconPreview;   // dipakai di BuyPreview (besar). kalau kosong, fallback ke iconGrid

    [Header("Pricing")]
    public long coinPrice = 0;
    public int shardPrice = 0;
    public bool allowBuyWithCoins = true;
    public bool allowBuyWithShards = false;

    [Header("Reward (what player gets when bought)")]
    public ShopRewardType rewardType = ShopRewardType.Energy;
    public int rewardAmount = 0; // e.g. 100 energy

    [TextArea(2, 4)]
    public string description;
}
