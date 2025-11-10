using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ✅ GLOBAL REWARD DATA CLASS
/// Dipakai oleh:
/// - LevelPreview (preview rewards sebelum level)
/// - LevelCompleteUI (display rewards setelah level complete)
/// - QuestRewardGenerator (generate random rewards dari quest crates)
/// </summary>
[System.Serializable]
public class RewardData
{
    // Common properties (dipakai semua)
    public RewardType type;
    public int amount;
    
    // Booster-specific (untuk type = Booster)
    public string boosterId; // itemId booster (coin2x, magnet, shield, dll)
    
    // UI Display (untuk popup)
    public Sprite icon;
    public string displayName;
    
    // Quest-specific (untuk QuestRewardGenerator)
    public string rewardName;     // nama item (Coins, Energy, magnet, dll)
    public bool isBooster;        // true jika booster item
    public float probability;     // chance percentage (untuk debug)
    
    // Constructors untuk convenience
    public RewardData()
    {
        // Default constructor
    }
    
    /// <summary>
    /// Constructor untuk economy rewards (Coin, Energy, Shard)
    /// </summary>
    public RewardData(RewardType type, int amount, Sprite icon = null, string displayName = null)
    {
        this.type = type;
        this.amount = amount;
        this.icon = icon;
        this.displayName = displayName ?? type.ToString();
        this.isBooster = false;
        this.rewardName = type.ToString();
    }
    
    /// <summary>
    /// Constructor untuk booster rewards
    /// </summary>
    public RewardData(string boosterId, int amount, Sprite icon = null, string displayName = null)
    {
        this.type = RewardType.Booster;
        this.boosterId = boosterId;
        this.amount = amount;
        this.icon = icon;
        this.displayName = displayName ?? boosterId;
        this.isBooster = true;
        this.rewardName = boosterId;
    }
}

/// <summary>
/// ✅ Wrapper untuk list of rewards (untuk JSON serialization)
/// </summary>
[System.Serializable]
public class RewardDataList
{
    public List<RewardData> rewards;
    
    public RewardDataList()
    {
        rewards = new List<RewardData>();
    }
}

/// <summary>
/// ✅ GLOBAL REWARD TYPE ENUM
/// PENTING: Jangan ubah order enum karena sudah tersimpan di PlayerPrefs!
/// </summary>
public enum RewardType
{
    Coin = 0,
    Energy = 1,
    Booster = 2
}