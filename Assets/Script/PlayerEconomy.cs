using System;
using UnityEngine;

/// <summary>
/// ✅ FIXED v3.0: PlayerEconomy - Scene Transition Fixed
/// - True singleton persistence
/// - Proper save/load
/// - Scene transition handling
/// </summary>
/// 

[DefaultExecutionOrder(-1000)] // ✅ Tambahkan ini
public class PlayerEconomy : MonoBehaviour
{
    public static PlayerEconomy Instance { get; private set; }

    const string KEY_COINS = "Kulino_Coins_v1";
    const string KEY_SHARDS = "Kulino_Shards_v1";
    const string KEY_ENERGY = "Kulino_Energy_v1";
    const string KEY_INITIALIZED = "Kulino_Economy_Initialized";

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

    [Header("Debug")]
    [SerializeField] private bool isInitialized = false;

    void Awake()
    {
        // ✅ ULTRA-STRONG singleton
        if (Instance != null)
        {
            try
            {
                if (Instance.gameObject != null && Instance.enabled && Instance.isInitialized)
                {
                    Debug.LogWarning($"[PlayerEconomy] Valid instance exists - destroying duplicate '{gameObject.name}'");
                    Destroy(gameObject);
                    return;
                }
            }
            catch
            {
                Debug.LogWarning("[PlayerEconomy] Previous instance invalid - taking over");
            }
        }

        Instance = this;

        // ✅ Ensure root object
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        gameObject.name = "[PlayerEconomy - PERSISTENT]";

        Debug.Log("[PlayerEconomy] ✓ Initialized successfully");
        
        // ✅ Load or initialize
        LoadOrInitialize();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.LogWarning("[PlayerEconomy] ⚠️ Instance being destroyed!");
            // Save before destroy
            Save();
        }
    }

    /// <summary>
    /// ✅ NEW: Load saved data or initialize with defaults
    /// </summary>
    void LoadOrInitialize()
    {
        bool wasInitialized = PlayerPrefs.GetInt(KEY_INITIALIZED, 0) == 1;

        if (wasInitialized)
        {
            // ✅ Load saved data
            Load();
            isInitialized = true;
        }
        else
        {
            // ✅ First time - use defaults
            Coins = defaultCoins;
            Shards = defaultShards;
            Energy = defaultEnergy;
            MaxEnergy = defaultMaxEnergy;
            
            Debug.Log($"[PlayerEconomy] First time initialization with defaults: Coins={Coins}, Shards={Shards}, Energy={Energy}");
            
            // ✅ Mark as initialized and save
            PlayerPrefs.SetInt(KEY_INITIALIZED, 1);
            Save();
            isInitialized = true;
        }

        OnEconomyChanged?.Invoke();
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
        
        long oldCoins = Coins;
        Coins = Math.Max(0, Coins + amount);
        
        Debug.Log($"[PlayerEconomy] AddCoins({amount}) -> {oldCoins} → {Coins}");
        
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
        
        long oldCoins = Coins;
        Coins -= amount;
        
        Debug.Log($"[PlayerEconomy] SpendCoins({amount}) -> {oldCoins} → {Coins}");
        
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddShards(int amount)
    {
        if (amount == 0) return;
        
        int oldShards = Shards;
        Shards = Math.Max(0, Shards + amount);
        
        Debug.Log($"[PlayerEconomy] AddShards({amount}) -> {oldShards} → {Shards}");
        
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
        
        int oldShards = Shards;
        Shards -= amount;
        
        Debug.Log($"[PlayerEconomy] SpendShards({amount}) -> {oldShards} → {Shards}");
        
        Save();
        OnEconomyChanged?.Invoke();
        return true;
    }

    public void AddEnergy(int amount)
    {
        if (amount == 0) return;
        
        int oldEnergy = Energy;
        Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
        
        Debug.Log($"[PlayerEconomy] AddEnergy({amount}) -> {oldEnergy} → {Energy}/{MaxEnergy}");
        
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
        
        int oldEnergy = Energy;
        Energy = Mathf.Max(0, Energy - amount);
        
        Debug.Log($"[PlayerEconomy] ConsumeEnergy({amount}) -> {oldEnergy} → {Energy}/{MaxEnergy}");
        
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
        PlayerPrefs.DeleteKey(KEY_INITIALIZED);
        PlayerPrefs.Save();
        Debug.Log("[PlayerEconomy] Saved data cleared");
        LoadOrInitialize();
    }

    [ContextMenu("Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("=== PLAYER ECONOMY STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Coins: {Coins:N0}");
        Debug.Log($"Shards: {Shards}");
        Debug.Log($"Energy: {Energy}/{MaxEnergy}");
        Debug.Log($"Has Save Data: {PlayerPrefs.GetInt(KEY_INITIALIZED, 0) == 1}");
        Debug.Log("============================");
    }

    [ContextMenu("Debug: Force Save")]
    void Debug_ForceSave()
    {
        Save();
        Debug.Log("[PlayerEconomy] ✓ Force saved");
    }
}