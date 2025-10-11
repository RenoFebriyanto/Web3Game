// QuestData.cs
using UnityEngine;

public enum QuestRewardType { Coin, Energy, Shard, Booster }

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/Quest Data", order = 100)]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    public string questId;         // unique id, e.g. "daily_login_1" or "weekly_play_3"
    public string title;
    [TextArea(2, 4)] public string description;

    [Header("Progress")]
    public int requiredCount = 1;  // e.g. play 5 times -> 5

    [Header("Reward")]
    public QuestRewardType rewardType = QuestRewardType.Coin;
    public int rewardAmount = 100;
    public string boosterItemId;   // only if rewardType == Booster (itemId matches ShopItemData.itemId)

    [Header("Meta")]
    public bool isDaily = true;    // true -> put into daily list, false -> candidate for weekly
    public Sprite icon;
}
