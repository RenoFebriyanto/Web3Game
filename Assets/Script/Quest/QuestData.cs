using UnityEngine;

public enum QuestRewardType { None, Coin, Shard, Energy, Booster }

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/Quest Data", order = 100)]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    public string questId;           // unik, contoh: daily_play_5min
    public string title;
    [TextArea] public string description;

    [Header("Icon (optional)")]
    public Sprite icon;              // sprite yang ditampilkan di quest item (optional)

    [Header("Progress")]
    public int requiredAmount = 1;   // jumlah yang harus dicapai

    [Header("Meta")]
    public bool isDaily = true;      // true = daily, false = weekly pool candidate

    [Header("Reward")]
    public QuestRewardType rewardType = QuestRewardType.Coin;
    public int rewardAmount = 0;     // coin/shard/energy amount or booster amount
    public string rewardBoosterId;   // jika rewardType == Booster, isi itemId booster
}
