using UnityEngine;

public static class QuestRewardGenerator
{
    [System.Serializable]
    public class RewardChance
    {
        public string rewardName;
        public float weight;
        public int minAmount;
        public int maxAmount;
    }

    static RewardChance[] rewardPool = new RewardChance[]
    {
        new RewardChance { rewardName = "Coins", weight = 40f, minAmount = 1000, maxAmount = 5000 },
        new RewardChance { rewardName = "Energy", weight = 30f, minAmount = 20, maxAmount = 100 },
        new RewardChance { rewardName = "Magnet", weight = 10f, minAmount = 1, maxAmount = 3 },
        new RewardChance { rewardName = "Shield", weight = 8f, minAmount = 1, maxAmount = 2 },
        new RewardChance { rewardName = "SpeedBoost", weight = 7f, minAmount = 1, maxAmount = 2 },
        new RewardChance { rewardName = "Shards", weight = 5f, minAmount = 5, maxAmount = 20 }
    };

    public static void GenerateRandomReward()
    {
        float total = 0f; foreach (var r in rewardPool) total += r.weight;
        float rnd = UnityEngine.Random.Range(0f, total);
        float cum = 0f;
        foreach (var r in rewardPool)
        {
            cum += r.weight;
            if (rnd <= cum)
            {
                int amount = UnityEngine.Random.Range(r.minAmount, r.maxAmount + 1);
                Grant(r.rewardName, amount);
                return;
            }
        }
        Grant("Coins", 1000);
    }

    static void Grant(string name, int amount)
    {
        switch (name)
        {
            case "Coins": PlayerEconomy.Instance?.AddCoins(amount); break;
            case "Shards": PlayerEconomy.Instance?.AddShards(amount); break;
            case "Energy": PlayerEconomy.Instance?.AddEnergy(amount); break;
            default:
                // boosters
                if (BoosterInventory.Instance == null)
                {
                    var go = new GameObject("BoosterInventory");
                    go.AddComponent<BoosterInventory>();
                    Object.DontDestroyOnLoad(go);
                }
                BoosterInventory.Instance?.AddBooster(name.ToLower(), amount);
                break;
        }
        Debug.Log($"[QuestRewardGenerator] Granted {amount}x {name}");
    }
}
