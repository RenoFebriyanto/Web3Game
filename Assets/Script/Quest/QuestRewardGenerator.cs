using UnityEngine;

public static class QuestRewardGenerator
{
    [System.Serializable]
    public class RewardChance
    {
        public string rewardName;
        public float weight;        // probabilitas relatif
        public int minAmount;
        public int maxAmount;
        public bool isBooster;      // true jika item ini booster
    }

    // ============================================
    // REWARD POOL - SESUAIKAN PROBABILITAS DI SINI
    // ============================================
    // Total weight = 100 untuk mudah hitung persentase

    static RewardChance[] rewardPool = new RewardChance[]
    {
        // === ECONOMY ITEMS (Total: 70%) ===
        new RewardChance
        {
            rewardName = "Coins",
            weight = 50f,           // 50% chance
            minAmount = 1000,
            maxAmount = 5000,
            isBooster = false
        },
        new RewardChance
        {
            rewardName = "Energy",
            weight = 19f,           // 19% chance
            minAmount = 20,
            maxAmount = 100,
            isBooster = false
        },
        new RewardChance
        {
            rewardName = "Shards",
            weight = 1f,            // 1% chance (RARE!) ⭐
            minAmount = 20,
            maxAmount = 100,
            isBooster = false
        },
        
        // === BOOSTER ITEMS (Total: 30%) ===
        new RewardChance
        {
            rewardName = "magnet",      // itemId harus lowercase!
            weight = 10f,               // 10% chance
            minAmount = 1,
            maxAmount = 3,
            isBooster = true
        },
        new RewardChance
        {
            rewardName = "shield",
            weight = 8f,                // 8% chance
            minAmount = 1,
            maxAmount = 2,
            isBooster = true
        },
        new RewardChance
        {
            rewardName = "coin2x",
            weight = 5f,                // 5% chance
            minAmount = 1,
            maxAmount = 2,
            isBooster = true
        },
        new RewardChance
        {
            rewardName = "speedboost",
            weight = 4f,                // 4% chance
            minAmount = 1,
            maxAmount = 2,
            isBooster = true
        },
        new RewardChance
        {
            rewardName = "timefreeze",
            weight = 3f,                // 3% chance
            minAmount = 1,
            maxAmount = 1,
            isBooster = true
        }
    };

    /// <summary>
    /// Generate random reward berdasarkan weighted probability
    /// Dipanggil saat player claim crate
    /// </summary>
    public static void GenerateRandomReward()
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var r in rewardPool)
        {
            totalWeight += r.weight;
        }

        // Generate random value
        float randomValue = Random.Range(0f, totalWeight);

        // Find reward based on weighted random
        float cumulative = 0f;
        foreach (var r in rewardPool)
        {
            cumulative += r.weight;
            if (randomValue <= cumulative)
            {
                // Roll amount
                int amount = Random.Range(r.minAmount, r.maxAmount + 1);

                // Calculate actual probability percentage
                float probability = (r.weight / totalWeight) * 100f;

                Debug.Log($"[QuestRewardGenerator] 🎁 Rolled: {r.rewardName} x{amount} (chance: {probability:F1}%, roll: {randomValue:F2}/{totalWeight:F0})");

                // Grant reward to player
                GrantReward(r.rewardName, amount, r.isBooster);

                return;
            }
        }

        // Fallback (shouldn't happen)
        Debug.LogWarning("[QuestRewardGenerator] No reward selected! Giving fallback coins");
        GrantReward("Coins", 1000, false);
    }

    /// <summary>
    /// Actually grant the reward to player
    /// </summary>
    static void GrantReward(string rewardName, int amount, bool isBooster)
    {
        if (isBooster)
        {
            // Grant booster item
            EnsureBoosterInventory();

            if (BoosterInventory.Instance != null)
            {
                BoosterInventory.Instance.AddBooster(rewardName, amount);
                Debug.Log($"[QuestRewardGenerator] ✓ Added {amount}x {rewardName} to Booster Inventory");
            }
            else
            {
                Debug.LogError("[QuestRewardGenerator] BoosterInventory.Instance is null!");
            }
        }
        else
        {
            // Grant economy item (Coins/Shards/Energy)
            string lowerName = rewardName.ToLower();

            if (lowerName.Contains("coin"))
            {
                if (PlayerEconomy.Instance != null)
                {
                    PlayerEconomy.Instance.AddCoins(amount);
                    Debug.Log($"[QuestRewardGenerator] ✓ Added {amount} Coins to PlayerEconomy");
                }
                else
                {
                    Debug.LogError("[QuestRewardGenerator] PlayerEconomy.Instance is null!");
                }
            }
            else if (lowerName.Contains("shard"))
            {
                if (PlayerEconomy.Instance != null)
                {
                    PlayerEconomy.Instance.AddShards(amount);
                    Debug.Log($"[QuestRewardGenerator] ✓ Added {amount} Shards to PlayerEconomy (RARE!)");
                }
                else
                {
                    Debug.LogError("[QuestRewardGenerator] PlayerEconomy.Instance is null!");
                }
            }
            else if (lowerName.Contains("energy"))
            {
                if (PlayerEconomy.Instance != null)
                {
                    PlayerEconomy.Instance.AddEnergy(amount);
                    Debug.Log($"[QuestRewardGenerator] ✓ Added {amount} Energy to PlayerEconomy");
                }
                else
                {
                    Debug.LogError("[QuestRewardGenerator] PlayerEconomy.Instance is null!");
                }
            }
            else
            {
                Debug.LogWarning($"[QuestRewardGenerator] Unknown economy item: {rewardName}");
            }
        }
    }

    static void EnsureBoosterInventory()
    {
        if (BoosterInventory.Instance == null)
        {
            var go = new GameObject("BoosterInventory");
            go.AddComponent<BoosterInventory>();
            Object.DontDestroyOnLoad(go);
            Debug.Log("[QuestRewardGenerator] Created BoosterInventory instance");
        }
    }

    /// <summary>
    /// Get probability percentage for a specific reward (untuk debug/UI)
    /// </summary>
    public static float GetRewardProbability(string rewardName)
    {
        float totalWeight = 0f;
        foreach (var r in rewardPool)
        {
            totalWeight += r.weight;
        }

        foreach (var r in rewardPool)
        {
            if (r.rewardName.Equals(rewardName, System.StringComparison.OrdinalIgnoreCase))
            {
                return (r.weight / totalWeight) * 100f;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Debug: Print all reward probabilities ke Console
    /// </summary>
    public static void DebugPrintProbabilities()
    {
        float totalWeight = 0f;
        foreach (var r in rewardPool)
        {
            totalWeight += r.weight;
        }

        Debug.Log("========================================");
        Debug.Log("  QUEST CRATE REWARD PROBABILITIES");
        Debug.Log("========================================");

        Debug.Log("\n--- ECONOMY ITEMS ---");
        foreach (var r in rewardPool)
        {
            if (!r.isBooster)
            {
                float percentage = (r.weight / totalWeight) * 100f;
                Debug.Log($"  {r.rewardName,-15} : {percentage,6:F2}%  (amount: {r.minAmount}-{r.maxAmount})");
            }
        }

        Debug.Log("\n--- BOOSTER ITEMS ---");
        foreach (var r in rewardPool)
        {
            if (r.isBooster)
            {
                float percentage = (r.weight / totalWeight) * 100f;
                Debug.Log($"  {r.rewardName,-15} : {percentage,6:F2}%  (amount: {r.minAmount}-{r.maxAmount})");
            }
        }

        Debug.Log($"\nTotal Weight: {totalWeight}");
        Debug.Log("========================================\n");
    }
}