using UnityEngine;

public class QuestSpawner : MonoBehaviour
{
    [Header("Prefab & counts")]
    public GameObject questItemPrefab; // prefab QuestItemRow (UI)
    public int dailyCount = 6;
    public int weeklyCount = 2;

    [Header("Reward ranges")]
    public int minCoins = 100;
    public int maxCoins = 2000;
    public int minShards = 1;
    public int maxShards = 50;
    public int minEnergy = 1;
    public int maxEnergy = 10;

    [Header("Requirement ranges")]
    public int minLoginDays = 1;
    public int maxLoginDays = 14;
    public float minPlaySec = 60f;
    public float maxPlaySec = 1800f;
    public int minCoinsCollected = 100;
    public int maxCoinsCollected = 2000;
    public float minDistance = 1f;
    public float maxDistance = 100f;

    void Start()
    {
        if (questItemPrefab == null)
        {
            Debug.LogError("[QuestSpawner] questItemPrefab not assigned!");
            return;
        }

        // spawn daily content (assume this GameObject is Content for daily viewport)
        SpawnDaily();

        // If you want weekly spawner separate, create another GameObject with this script and call SpawnWeekly(true)
    }

    public void SpawnDaily()
    {
        // daily: use today's date to create unique ids
        var content = transform;
        string dayKey = System.DateTime.UtcNow.ToString("yyyyMMdd");

        for (int i = 0; i < dailyCount; i++)
        {
            var go = Instantiate(questItemPrefab, content);
            var qi = go.GetComponent<QuestItem>();
            if (qi == null) continue;

            qi.isWeekly = false;
            qi.cycleId = 0;
            qi.questId = $"daily_{dayKey}_{i}";

            SetupRandomQuest(qi, false);
        }
    }

    public void SpawnWeekly()
    {
        // ensure weekly cycle id updated (resets if older than 7 days)
        bool createdNew = QuestProgress.Instance != null && QuestProgress.Instance.EnsureWeeklyCycleUpToDate(7);
        int cycle = QuestProgress.Instance != null ? QuestProgress.Instance.GetWeeklyCycleId() : 0;

        var content = transform;
        for (int i = 0; i < weeklyCount; i++)
        {
            var go = Instantiate(questItemPrefab, content);
            var qi = go.GetComponent<QuestItem>();
            if (qi == null) continue;

            qi.isWeekly = true;
            qi.cycleId = cycle;
            qi.questId = $"weekly_{cycle}_{i}";

            SetupRandomQuest(qi, true);
        }
    }

    void SetupRandomQuest(QuestItem qi, bool isWeekly)
    {
        // random requirement type
        var reqIndex = Random.Range(0, System.Enum.GetValues(typeof(QuestItem.RequirementType)).Length);
        qi.requirementType = (QuestItem.RequirementType)reqIndex;

        // pick value based on type
        switch (qi.requirementType)
        {
            case QuestItem.RequirementType.LoginDays:
                qi.requiredValue = Random.Range(minLoginDays, maxLoginDays + 1);
                break;
            case QuestItem.RequirementType.PlayTimeSeconds:
                qi.requiredValue = Random.Range(minPlaySec, maxPlaySec);
                break;
            case QuestItem.RequirementType.CoinsCollected:
                qi.requiredValue = Random.Range(minCoinsCollected, maxCoinsCollected);
                break;
            case QuestItem.RequirementType.DistanceMeters:
                qi.requiredValue = Random.Range(minDistance, maxDistance);
                break;
            default:
                qi.requiredValue = 1;
                break;
        }

        // pick reward type and amount
        int rt = Random.Range(0, 3);
        qi.rewardType = (QuestItem.RewardType)rt;
        if (qi.rewardType == QuestItem.RewardType.Coins)
            qi.rewardCoins = Random.Range(minCoins, maxCoins);
        else if (qi.rewardType == QuestItem.RewardType.Shards)
            qi.rewardShards = Random.Range(minShards, maxShards);
        else
            qi.rewardEnergy = Random.Range(minEnergy, maxEnergy);

        // set default UI title if exists
        if (qi.titleText != null)
        {
            string r = qi.rewardType == QuestItem.RewardType.Coins ? $"{qi.rewardCoins:N0} Coins" :
                       qi.rewardType == QuestItem.RewardType.Shards ? $"{qi.rewardShards} Shards" :
                       $"{qi.rewardEnergy} Energy";
            qi.titleText.text = $"{qi.requirementType} • {r}";
        }

        // make sure visuals & evaluation recalc
        qi.EvaluateRequirement();    // *make this public if needed* (we used internal EvaluateRequirement previously - if you find errors, replace with calling OnProgressChanged simulation)
        qi.UpdateVisual();
    }
}
