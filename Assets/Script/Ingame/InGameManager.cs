using UnityEngine;

public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance;
    public int coins = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddCoins(int amt)
    {
        coins += amt;
        // update UI
        PlayerEconomy.Instance?.AddCoins(coins);
    }

}
