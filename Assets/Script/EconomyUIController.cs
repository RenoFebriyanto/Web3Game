using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class EconomyUIController : MonoBehaviour
{
    [Header("Assign either TMP fields or regular Text fields")]
#if TMP_PRESENT
    public TMP_Text coinsTMP;
    public TMP_Text shardsTMP;
    public TMP_Text energyTMP;
#endif
    public Text coinsText;
    public Text shardsText;
    public Text energyText;

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
        string energyStr = PlayerEconomy.Instance.Energy.ToString();

#if TMP_PRESENT
        if (coinsTMP != null) coinsTMP.text = coinsStr;
        if (shardsTMP != null) shardsTMP.text = shardsStr;
        if (energyTMP != null) energyTMP.text = energyStr;
#endif
        if (coinsText != null) coinsText.text = coinsStr;
        if (shardsText != null) shardsText.text = shardsStr;
        if (energyText != null) energyText.text = energyStr;
    }
}
