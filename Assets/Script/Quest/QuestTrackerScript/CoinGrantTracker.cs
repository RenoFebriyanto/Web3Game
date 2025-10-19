using UnityEngine;

public static class CoinGrantTracker
{
    // Call this instead of directly calling PlayerEconomy.AddCoins when possible
    public static void GiveCoins(long amount)
    {
        if (amount == 0) return;
        PlayerEconomy.Instance?.AddCoins(amount);

        // Add progress for coin quest (cast to int; ensure quest threshold fits int)
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AddProgress("daily_getcoin_2000", (int)amount);
            Debug.Log($"[CoinGrantTracker] Added {amount} coins and progressed daily_getcoin_2000 by {(int)amount}");
        }
    }
}
