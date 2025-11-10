using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Global RewardData class - dipakai oleh LevelPreview dan LevelComplete
/// </summary>
[System.Serializable]
public class RewardData
{
    public RewardType type;
    public int amount;
    public string boosterId; // untuk boosters
    public Sprite icon;
    public string displayName;
}

[System.Serializable]
public class RewardDataList
{
    public List<RewardData> rewards;
}

public enum RewardType
{
    Coin,
    Energy,
    Booster
}