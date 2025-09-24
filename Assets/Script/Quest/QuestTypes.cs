using System;
using UnityEngine;

public enum QuestConditionType { LoginDays, PlayTimeSeconds, CoinsCollected, DistanceMeters }
public enum QuestRewardType { Coins, Shards, Energy }

[Serializable]
public class QuestDefinition
{
    public string id;                // unique id, e.g. "daily_login_3"
    public string title;
    [TextArea] public string description;
    public QuestConditionType conditionType;
    public long targetValue;         // numeric target (seconds, coins, distance, days)
    public QuestRewardType rewardType;
    public long rewardAmount;        // reward value (coins=long, shards=int fits in long, energy=int fits long)
    public bool isDaily = true;      // apakah daily atau weekly (untuk reset)
}

[Serializable]
public class QuestState
{
    public string id;
    public long progress;       // current progress (use long to store seconds/coins/distance)
    public bool completed;
    public bool claimed;
    public long lastUpdatedEpoch; // optional
}
