using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Developer helper to set player economy for testing WITHOUT modifying PlayerEconomy.cs.
/// - Uses public Add/Spend/Consume methods to adjust values (safe).
/// - Optionally attempts to set MaxEnergy via reflection (private setter) if requested.
/// - Provides ContextMenu commands visible in Inspector for quick use.
/// </summary>
[DisallowMultipleComponent]
public class DevEconomySetter : MonoBehaviour
{
    [Header("Absolute values to apply (leave 0 to ignore)")]
    public long targetCoins = 0;
    public int targetShards = 0;
    public int targetEnergy = 0;

    [Header("Optional: change MaxEnergy (will attempt via reflection)")]
    public int targetMaxEnergy = 0; // 0 = ignore

    [Header("Runtime helpers")]
    public bool applyOnStart = false;

    void Start()
    {
        if (applyOnStart)
        {
            ApplyAbsoluteValues();
        }
    }

    #region Inspector / Context Menu commands

    [ContextMenu("Apply Absolute Values (use public API)")]
    public void ApplyAbsoluteValues()
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogWarning("[DevEconomySetter] PlayerEconomy.Instance is null. Ensure PlayerEconomy exists in the scene.");
            return;
        }

        // coins
        try
        {
            long curCoins = PlayerEconomy.Instance.Coins;
            long desiredCoins = targetCoins;
            long diffCoins = desiredCoins - curCoins;
            if (diffCoins > 0)
            {
                PlayerEconomy.Instance.AddCoins(diffCoins);
            }
            else if (diffCoins < 0)
            {
                // remove by spending repeatedly if necessary (SpendCoins takes int/long)
                long toRemove = -diffCoins;
                // Spend as much as possible in one call
                bool ok = PlayerEconomy.Instance.SpendCoins(toRemove);
                if (!ok)
                {
                    // fallback: spend what's possible (loop until match or zero)
                    while (toRemove > 0 && PlayerEconomy.Instance.Coins > 0)
                    {
                        long available = PlayerEconomy.Instance.Coins;
                        long removeNow = Math.Min(available, toRemove);
                        PlayerEconomy.Instance.SpendCoins(removeNow);
                        toRemove -= removeNow;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[DevEconomySetter] Error setting coins: " + ex);
        }

        // shards
        try
        {
            int curShards = PlayerEconomy.Instance.Shards;
            int desiredShards = targetShards;
            int diffShards = desiredShards - curShards;
            if (diffShards > 0) PlayerEconomy.Instance.AddShards(diffShards);
            else if (diffShards < 0)
            {
                int toRemove = -diffShards;
                bool ok = PlayerEconomy.Instance.SpendShards(toRemove);
                if (!ok)
                {
                    while (toRemove > 0 && PlayerEconomy.Instance.Shards > 0)
                    {
                        int avail = PlayerEconomy.Instance.Shards;
                        int rem = Math.Min(avail, toRemove);
                        PlayerEconomy.Instance.SpendShards(rem);
                        toRemove -= rem;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[DevEconomySetter] Error setting shards: " + ex);
        }

        // optional max energy
        if (targetMaxEnergy > 0)
        {
            bool okSetMax = TrySetMaxEnergyReflection(targetMaxEnergy);
            if (!okSetMax)
            {
                Debug.LogWarning("[DevEconomySetter] Could not set MaxEnergy via reflection. You can still adjust Energy value, but MaxEnergy unchanged.");
            }
        }

        // energy
        try
        {
            int curEnergy = PlayerEconomy.Instance.Energy;
            int desiredEnergy = targetEnergy;

            // clamp desiredEnergy by MaxEnergy (use current MaxEnergy)
            int maxE = PlayerEconomy.Instance.MaxEnergy;
            desiredEnergy = Mathf.Clamp(desiredEnergy, 0, maxE);

            int diffEnergy = desiredEnergy - curEnergy;
            if (diffEnergy > 0) PlayerEconomy.Instance.AddEnergy(diffEnergy);
            else if (diffEnergy < 0)
            {
                int toRemove = -diffEnergy;
                bool ok = PlayerEconomy.Instance.ConsumeEnergy(toRemove);
                if (!ok)
                {
                    // consume in loop until target reached
                    while (toRemove > 0 && PlayerEconomy.Instance.Energy > 0)
                    {
                        int avail = PlayerEconomy.Instance.Energy;
                        int rem = Math.Min(avail, toRemove);
                        PlayerEconomy.Instance.ConsumeEnergy(rem);
                        toRemove -= rem;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[DevEconomySetter] Error setting energy: " + ex);
        }

        Debug.Log("[DevEconomySetter] ApplyAbsoluteValues completed. New state -> Coins: " +
                  PlayerEconomy.Instance.Coins + " Shards: " + PlayerEconomy.Instance.Shards +
                  " Energy: " + PlayerEconomy.Instance.Energy + "/" + PlayerEconomy.Instance.MaxEnergy);
    }

    [ContextMenu("Add (delta) values")]
    public void AddDeltaValues()
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogWarning("[DevEconomySetter] PlayerEconomy.Instance is null.");
            return;
        }

        if (targetCoins != 0) PlayerEconomy.Instance.AddCoins(targetCoins);
        if (targetShards != 0) PlayerEconomy.Instance.AddShards(targetShards);
        if (targetEnergy != 0) PlayerEconomy.Instance.AddEnergy(targetEnergy);

        Debug.Log("[DevEconomySetter] AddDeltaValues applied.");
    }

    [ContextMenu("Reset economy to PlayerEconomy defaults")]
    public void ResetToPlayerDefaults()
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogWarning("[DevEconomySetter] PlayerEconomy.Instance is null.");
            return;
        }
        PlayerEconomy.Instance.ResetEconomy();
        Debug.Log("[DevEconomySetter] Called PlayerEconomy.ResetEconomy()");
    }

    [ContextMenu("Clear saved PlayerPrefs keys for economy")]
    public void ClearSavedEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogWarning("[DevEconomySetter] PlayerEconomy.Instance is null.");
            return;
        }

        PlayerEconomy.Instance.ClearSavedData();
        Debug.Log("[DevEconomySetter] ClearSavedEconomy called.");
    }

    #endregion

    #region Reflection helper for MaxEnergy

    bool TrySetMaxEnergyReflection(int newMax)
    {
        try
        {
            var peType = PlayerEconomy.Instance.GetType();
            // try property first
            var prop = peType.GetProperty("MaxEnergy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(PlayerEconomy.Instance, newMax);
                // ensure energy clamped to new max
                if (PlayerEconomy.Instance.Energy > newMax)
                {
                    int diff = PlayerEconomy.Instance.Energy - newMax;
                    PlayerEconomy.Instance.ConsumeEnergy(diff);
                }
                Debug.Log("[DevEconomySetter] MaxEnergy set via property reflection to " + newMax);
                return true;
            }

            // fallback: try backing field (compiler-generated)
            var field = peType.GetField("<MaxEnergy>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(PlayerEconomy.Instance, newMax);
                if (PlayerEconomy.Instance.Energy > newMax)
                {
                    int diff = PlayerEconomy.Instance.Energy - newMax;
                    PlayerEconomy.Instance.ConsumeEnergy(diff);
                }
                Debug.Log("[DevEconomySetter] MaxEnergy set via backing field to " + newMax);
                return true;
            }

            Debug.LogWarning("[DevEconomySetter] No writable MaxEnergy property or backing field found.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError("[DevEconomySetter] Reflection error setting MaxEnergy: " + ex);
            return false;
        }
    }

    #endregion
}
