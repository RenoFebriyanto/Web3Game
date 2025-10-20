// Assets/Script/Quest/QuestRewardGenerator.cs
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.Rendering.FilterWindow;

public static class QuestRewardGenerator
{
    [System.Serializable]
    public class RewardChance
    {
        public string rewardName;
        public float weight; // higher = more common
        public int minAmount;
        public int maxAmount;
    }

    // Reward pool dengan weighted chance
    static RewardChance[] rewardPool = new RewardChance[]
    {
        // Coins - common (40%)
        new RewardChance { rewardName = "Coins", weight = 40f, minAmount = 1000, maxAmount = 5000 },
        
        // Energy - common (30%)
        new RewardChance { rewardName = "Energy", weight = 30f, minAmount = 20, maxAmount = 100 },
        
        // Boosters - uncommon (25%)
        new RewardChance { rewardName = "Magnet", weight = 10f, minAmount = 1, maxAmount = 3 },
        new RewardChance { rewardName = "Shield", weight = 8f, minAmount = 1, maxAmount = 2 },
        new RewardChance { rewardName = "SpeedBoost", weight = 7f, minAmount = 1, maxAmount = 2 },
        
        // Shards - rare (5%)
        new RewardChance { rewardName = "Shards", weight = 5f, minAmount = 5, maxAmount = 20 }
    };

    public static void GenerateRandomReward()
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var r in rewardPool) totalWeight += r.weight;

        // Random pick
        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var reward in rewardPool)
        {
            cumulative += reward.weight;
            if (random <= cumulative)
            {
                // This reward won!
                int amount = Random.Range(reward.minAmount, reward.maxAmount + 1);
                GrantReward(reward.rewardName, amount);
                return;
            }
        }

        // Fallback (should never happen)
        GrantReward("Coins", 1000);
    }

    static void GrantReward(string rewardType, int amount)
    {
        Debug.Log($"[QuestRewardGenerator] Generated: {amount}x {rewardType}");

        switch (rewardType)
        {
            case "Coins":
                PlayerEconomy.Instance?.AddCoins(amount);
                Debug.Log($"  ✓ Granted {amount} coins");
                break;

            case "Shards":
                PlayerEconomy.Instance?.AddShards(amount);
                Debug.Log($"  ✓ Granted {amount} shards (RARE!)");
                break;

            case "Energy":
                PlayerEconomy.Instance?.AddEnergy(amount);
                Debug.Log($"  ✓ Granted {amount} energy");
                break;

            case "Magnet":
            case "Shield":
            case "SpeedBoost":
            case "TimeFreeze":
            case "Coin2x":
                string boosterId = rewardType.ToLower();
                if (boosterId == "speedboost") boosterId = "speedboost";
                if (boosterId == "coin2x") boosterId = "coin2x";

                if (BoosterInventory.Instance == null)
                {
                    var go = new GameObject("BoosterInventory");
                    go.AddComponent<BoosterInventory>();
                    Object.DontDestroyOnLoad(go);
                }

                BoosterInventory.Instance?.AddBooster(boosterId, amount);
                Debug.Log($"  ✓ Granted {amount}x {rewardType} booster");
                break;

            default:
                Debug.LogWarning($"[QuestRewardGenerator] Unknown reward type: {rewardType}");
                break;
        }

        // Optional: Show popup notification to player
        ShowRewardPopup(rewardType, amount);
    }

    static void ShowRewardPopup(string rewardType, int amount)
    {
        // TODO: Implement popup UI
        Debug.Log($"💰 Reward: {amount}x {rewardType}!");
    }
}
