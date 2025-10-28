using UnityEngine;

public class Coin2x : MonoBehaviour
{
    [Header("Coin2x")]
    public GameObject coin2x;
    public int cointime = 10;
    public PlayerEconomy playerEconomy;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if ( collision.gameObject.CompareTag("Player") )
        {
            Destroy(coin2x);
            AddCoin2x();
            // StartCoroutine(WaitAndDisable(cointime));
        }
    }

    public void AddCoin2x()
    {
        playerEconomy.AddCoins(1+1);
    }
}
