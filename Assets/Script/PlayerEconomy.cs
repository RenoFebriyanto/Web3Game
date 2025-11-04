using System;
using UnityEngine;

/// <summary>
/// FIXED: Proper singleton pattern dengan cleanup
/// </summary>
public class PlayerEconomy : MonoBehaviour
{
    public static PlayerEconomy Instance { get; private set; }

    const string KEY_COINS = "Kulino_Coins_v1";
    const string KEY_SHARDS = "Kulino_Shards_v1";
    const string KEY_ENERGY = "Kulino_Energy_v1";

    public long Coins { get; private set; }
    public int Shards { get; private set; }
    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; } = 100;

    public event Action OnEconomyChanged;

    [Header("Default starting values")]
    [SerializeField] int defaultEnergy = 100;
    [SerializeField] long defaultCoins = 5000;
    [SerializeField] int defaultShards = 0;
    [SerializeField] int defaultMaxEnergy = 100;

    void Awake()
    {
        // ✅ FIX 1: Check if instance exists AND is not destroyed
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning($"[PlayerEconomy] Duplicate found on '{gameObject.name}' - destroying");
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[PlayerEconomy] ✓ Initialized successfully");
        Load();
    }

    void OnDestroy()
    {
        // ✅ FIX 2: Clear instance reference when destroyed
        if (Instance == this)
        {
            Debug.Log("[PlayerEconomy] Instance destroyed, clearing reference");
            Instance = null;
        }
    }

    void Load()
    {
        string sCoins = PlayerPrefs.GetString(KEY_COINS, defaultCoins.ToString());
        if (!long.TryParse(sCoins, out var c)) c = defaultCoins;

        Coins = c;
        Shards = PlayerPrefs.GetInt(KEY_SHARDS, defaultShards);
        Energy = PlayerPrefs.GetInt(KEY_ENERGY, defaultEnergy);
        MaxEnergy = defaultMaxEnergy;
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
        if (amount == 0) return;
        Coins = Math.Max(0, Coins + amount);
        Debug.Log($"[PlayerEconomy] AddCoins({amount}) -> Total: {Coins}");
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendCoins(long amount)
    {
        if (amount <= 0) return false;
        if (Coins < amount)
        {
            Debug.LogWarning($"[PlayerEconomy] Cannot spend {amount} coins (have: {Coins})");
            return false;
        }
        Coins -= amount;
        Debug.Log($"[PlayerEconomy] SpendCoins({amount}) -> Remaining: {Coins}");
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddShards(int amount)
    {
        if (amount == 0) return;
        Shards = Math.Max(0, Shards + amount);
        Debug.Log($"[PlayerEconomy] AddShards({amount}) -> Total: {Shards}");
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool SpendShards(int amount)
    {
        if (amount <= 0) return false;
        if (Shards < amount)
        {
            Debug.LogWarning($"[PlayerEconomy] Cannot spend {amount} shards (have: {Shards})");
            return false;
        }
        Shards -= amount;
        Debug.Log($"[PlayerEconomy] SpendShards({amount}) -> Remaining: {Shards}");
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddEnergy(int amount)
    {
        if (amount == 0) return;
        Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
        Debug.Log($"[PlayerEconomy] AddEnergy({amount}) -> Total: {Energy}/{MaxEnergy}");
        Save();
        OnEconomyChanged?.Invoke();
    }

    public bool ConsumeEnergy(int amount)
    {
        if (amount <= 0) return false;
        if (Energy < amount)
        {
            Debug.LogWarning($"[PlayerEconomy] Cannot consume {amount} energy (have: {Energy})");
            return false;
        }
        Energy = Mathf.Max(0, Energy - amount);
        Debug.Log($"[PlayerEconomy] ConsumeEnergy({amount}) -> Remaining: {Energy}/{MaxEnergy}");
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    [ContextMenu("Reset Economy")]
    public void ResetEconomy()
    {
        Coins = defaultCoins;
        Shards = defaultShards;
        MaxEnergy = defaultMaxEnergy;
        Energy = defaultEnergy;
        Save();
        OnEconomyChanged?.Invoke();
        Debug.Log("[PlayerEconomy] Economy reset to defaults");
    }

    public void ClearSavedData()
    {
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_SHARDS);
        PlayerPrefs.DeleteKey(KEY_ENERGY);
        PlayerPrefs.Save();
        Debug.Log("[PlayerEconomy] Saved data cleared");
        Load();
    }

    [ContextMenu("Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("=== PLAYER ECONOMY STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Coins: {Coins:N0}");
        Debug.Log($"Shards: {Shards}");
        Debug.Log($"Energy: {Energy}/{MaxEnergy}");
        Debug.Log("============================");
    }
}