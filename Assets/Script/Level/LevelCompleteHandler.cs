using UnityEngine;

public class LevelCompleteHandler : MonoBehaviour
{
    // example reward amounts
    public long rewardCoins = 100;
    public int rewardShards = 1;
    public int rewardEnergy = 1;

    public void OnLevelComplete()
    {
        if (PlayerEconomy.Instance == null) return;
        PlayerEconomy.Instance.AddCoins(rewardCoins);
        PlayerEconomy.Instance.AddShards(rewardShards);
        PlayerEconomy.Instance.AddEnergy(rewardEnergy);
    }
}
