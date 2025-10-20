using UnityEngine;

public class CoinGrantTracker : MonoBehaviour
{
    [Tooltip("Quest id to progress when player gains coins (quest expects coin amount).")]
    public string questId;

    long lastCoins = -1;

    void OnEnable()
    {
        if (PlayerEconomy.Instance != null)
        {
            lastCoins = PlayerEconomy.Instance.Coins;
            PlayerEconomy.Instance.OnEconomyChanged += OnEconomyChanged;
        }
    }

    void OnDisable()
    {
        if (PlayerEconomy.Instance != null) PlayerEconomy.Instance.OnEconomyChanged -= OnEconomyChanged;
    }

    void OnEconomyChanged()
    {
        if (PlayerEconomy.Instance == null) return;
        long cur = PlayerEconomy.Instance.Coins;
        if (lastCoins < 0) lastCoins = cur;
        long delta = cur - lastCoins;
        if (delta > 0 && !string.IsNullOrEmpty(questId))
        {
            QuestManager.Instance?.AddProgress(questId, (int)delta);
        }
        lastCoins = cur;
    }
}
