using System;
using UnityEngine;

public class PlayerEconomy : MonoBehaviour
{
    public static PlayerEconomy Instance { get; private set; }

    const string KEY_COINS = "Kulino_Coins_v1";
    const string KEY_SHARDS = "Kulino_Shards_v1";
    const string KEY_ENERGY = "Kulino_Energy_v1";
    // (OPTIONAL) you can add KEY_MAXENERGY if you want to persist
    // const string KEY_MAXENERGY = "Kulino_MaxEnergy_v1";

    public long Coins { get; private set; }
    public int Shards { get; private set; }
    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; } = 100;

    public event Action OnEconomyChanged;

    [Header("Default starting values (set in Inspector)")]
    [SerializeField] int defaultEnergy = 100;       // <-- default 100
    [SerializeField] long defaultCoins = 5000;      // <-- default 5000
    [SerializeField] int defaultShards = 0;         // <-- default 0
    [SerializeField] int defaultMaxEnergy = 100;    // <-- inspector-editable max


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[PlayerEconomy] Duplicate instance found - destroying this.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[PlayerEconomy] Awake - loading values");
        Load();
    }

    void Load()
    {
        // load coins as string then parse to support big numbers
        string sCoins = PlayerPrefs.GetString(KEY_COINS, defaultCoins.ToString());
        if (!long.TryParse(sCoins, out var c)) c = defaultCoins;

        Coins = c;
        Shards = PlayerPrefs.GetInt(KEY_SHARDS, defaultShards);
        Energy = PlayerPrefs.GetInt(KEY_ENERGY, defaultEnergy);

        // set MaxEnergy from inspector default if not persisting
        MaxEnergy = defaultMaxEnergy;

        // clamp energy to max
        Energy = Mathf.Clamp(Energy, 0, MaxEnergy);

        Debug.Log($"[PlayerEconomy] Loaded -> Coins={Coins}, Shards={Shards}, Energy={Energy}/{MaxEnergy}");
        OnEconomyChanged?.Invoke();
    }

    void Save()
    {
        PlayerPrefs.SetString(KEY_COINS, Coins.ToString());
        PlayerPrefs.SetInt(KEY_SHARDS, Shards);
        PlayerPrefs.SetInt(KEY_ENERGY, Energy);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerEconomy] Saved -> Coins={Coins}, Shards={Shards}, Energy={Energy}/{MaxEnergy}");
    }

    public void AddCoins(long amount)
    {
        Debug.Log($"[PlayerEconomy] AddCoins({amount}) called");
        if (amount == 0) return;
        Coins = Math.Max(0, Coins + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendCoins(long amount)
    {
        if (amount <= 0) return false;
        if (Coins < amount) return false;
        Coins -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddShards(int amount)
    {
        Debug.Log($"[PlayerEconomy] AddShards({amount}) called");
        if (amount == 0) return;
        Shards = Math.Max(0, Shards + amount);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendShards(int amount)
    {
        if (amount <= 0) return false;
        if (Shards < amount) return false;
        Shards -= amount;
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddEnergy(int amount)
    {
        Debug.Log($"[PlayerEconomy] AddEnergy({amount}) called");
        if (amount == 0) return;
        Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool ConsumeEnergy(int amount)
    {
        if (amount <= 0) return false;
        if (Energy < amount) return false;
        Energy = Mathf.Max(0, Energy - amount);
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    [ContextMenu("Reset Economy")]
    public void ResetEconomy()
    {
        Debug.Log("[PlayerEconomy] ResetEconomy called");
        Coins = defaultCoins;
        Shards = defaultShards;
        MaxEnergy = defaultMaxEnergy;
        Energy = defaultEnergy;
        Save();
        OnEconomyChanged?.Invoke();
    }

    // helper to wipe persistent saved data (call from code if needed)
    public void ClearSavedData()
    {
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_SHARDS);
        PlayerPrefs.DeleteKey(KEY_ENERGY);
        PlayerPrefs.Save();
        Debug.Log("[PlayerEconomy] ClearSavedData called - PlayerPrefs keys removed");
        Load();
    }
}
