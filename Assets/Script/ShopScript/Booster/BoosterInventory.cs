using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent singleton that stores counts of purchased boosters (by itemId).
/// Uses PlayerPrefs for persistence (key: BoosterCount_{itemId}).
/// </summary>
public class BoosterInventory : MonoBehaviour
{
    public static BoosterInventory Instance { get; private set; }

    const string PREF_KEY_PREFIX = "BoosterCount_";

    Dictionary<string, int> cache = new Dictionary<string, int>();

    public event Action<string, int> OnBoosterChanged;
    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    int LoadSaved(string id)
    {
        return PlayerPrefs.GetInt(PREF_KEY_PREFIX + id, 0);
    }

    public int GetBoosterCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        if (cache.TryGetValue(itemId, out var v)) return v;
        var loaded = LoadSaved(itemId);
        cache[itemId] = loaded;
        return loaded;
    }

    public void AddBooster(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        int cur = GetBoosterCount(itemId);
        int nxt = cur + amount;
        cache[itemId] = nxt;
        PlayerPrefs.SetInt(PREF_KEY_PREFIX + itemId, nxt);
        PlayerPrefs.Save();
        OnBoosterChanged?.Invoke(itemId, nxt);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[BoosterInventory] Added {amount} x {itemId} => {nxt}");
    }

    public bool UseBooster(string itemId)
    {
        int cur = GetBoosterCount(itemId);
        if (cur <= 0) return false;
        int nxt = Mathf.Max(0, cur - 1);
        cache[itemId] = nxt;
        PlayerPrefs.SetInt(PREF_KEY_PREFIX + itemId, nxt);
        PlayerPrefs.Save();
        OnBoosterChanged?.Invoke(itemId, nxt);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[BoosterInventory] Used 1 x {itemId} => {nxt}");
        return true;
    }

    public void SetBoosterCount(string itemId, int newCount)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        cache[itemId] = Mathf.Max(0, newCount);
        PlayerPrefs.SetInt(PREF_KEY_PREFIX + itemId, cache[itemId]);
        PlayerPrefs.Save();
        OnBoosterChanged?.Invoke(itemId, cache[itemId]);
        OnInventoryChanged?.Invoke();
    }
}
