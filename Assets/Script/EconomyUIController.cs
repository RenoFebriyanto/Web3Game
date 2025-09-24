using UnityEngine;
using UnityEngine.UI;

using TMPro;


public class EconomyUIController : MonoBehaviour
{
    [Header("Assign either TMP fields or regular Text fields")]

    public TMP_Text coinsTMP;
    public TMP_Text shardsTMP;
    public TMP_Text energyTMP;

    void OnEnable()
    {
        if (PlayerEconomy.Instance != null)
            PlayerEconomy.Instance.OnEconomyChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (PlayerEconomy.Instance != null)
            PlayerEconomy.Instance.OnEconomyChanged -= Refresh;
    }

    void Refresh()
    {
        if (PlayerEconomy.Instance == null) return;

        string coinsStr = PlayerEconomy.Instance.Coins.ToString("N0");
        string shardsStr = PlayerEconomy.Instance.Shards.ToString();
        string energyStr = $"{PlayerEconomy.Instance.Energy}/{PlayerEconomy.Instance.MaxEnergy}";

    if (coinsTMP != null) coinsTMP.text = coinsStr;
    if (shardsTMP != null) shardsTMP.text = shardsStr;
    if (energyTMP != null) energyTMP.text = energyStr;

    }

}
