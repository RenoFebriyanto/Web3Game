using UnityEngine;

public enum QuestRewardType { None, Coin, Shard, Energy, Booster }

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/Quest Data", order = 100)]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    public string questId;           // unik, contoh: daily_play_5min
    public string title;             // judul tanpa [Daily] prefix
    [TextArea] public string description;

    [Header("Progress")]
    public int requiredAmount = 1;   // jumlah yang harus dicapai

    [Header("Meta")]
    public bool isDaily = true;      // true = daily, false = weekly pool candidate
    public Sprite icon;             // gambar icon untuk preview / item display

    [Header("Reward")]
    public QuestRewardType rewardType = QuestRewardType.Coin;
    public int rewardAmount = 0;     // coin/shard/energy amount or booster amount
    public string rewardBoosterId;   // jika rewardType == Booster, isi itemId booster
}
