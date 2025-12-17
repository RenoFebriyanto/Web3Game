using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ✅ FIXED v2.0: EconomyUIController - Retry Connection
/// - Waits for PlayerEconomy to be ready
/// - Auto-reconnects after scene load
/// </summary>
public class EconomyUIController : MonoBehaviour
{
    [Header("TMP Text References")]
    public TMP_Text coinsTMP;
    public TMP_Text shardsTMP;
    public TMP_Text energyTMP;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool isSubscribed = false;
    private Coroutine connectionCoroutine;

    void OnEnable()
    {
        // Start connection retry coroutine
        if (connectionCoroutine != null)
            StopCoroutine(connectionCoroutine);

        connectionCoroutine = StartCoroutine(ConnectToPlayerEconomy());
    }

    void OnDisable()
    {
        UnsubscribeFromEconomy();

        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
            connectionCoroutine = null;
        }
    }

    /// <summary>
    /// ✅ Retry connection until PlayerEconomy is ready
    /// </summary>
    IEnumerator ConnectToPlayerEconomy()
    {
        int attempts = 0;
        const int maxAttempts = 20;

        while (attempts < maxAttempts)
        {
            attempts++;

            if (PlayerEconomy.Instance != null)
            {
                Log($"✅ Connected to PlayerEconomy (attempt {attempts})");
                SubscribeToEconomy();
                Refresh();
                yield break; // Success!
            }

            Log($"⏳ Waiting for PlayerEconomy... ({attempts}/{maxAttempts})");
            yield return new WaitForSeconds(0.1f);
        }

        LogError("❌ Failed to connect to PlayerEconomy after max attempts!");
    }

    void SubscribeToEconomy()
    {
        if (isSubscribed || PlayerEconomy.Instance == null)
            return;

        PlayerEconomy.Instance.OnEconomyChanged += Refresh;
        isSubscribed = true;
        Log("✓ Subscribed to economy events");
    }

    void UnsubscribeFromEconomy()
    {
        if (!isSubscribed || PlayerEconomy.Instance == null)
            return;

        PlayerEconomy.Instance.OnEconomyChanged -= Refresh;
        isSubscribed = false;
        Log("✓ Unsubscribed from economy events");
    }

    /// <summary>
    /// ✅ Refresh UI display
    /// </summary>
    void Refresh()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogError("Cannot refresh: PlayerEconomy.Instance is null");
            return;
        }

        // Get values
        string coinsStr = PlayerEconomy.Instance.Coins.ToString("N0");
        string shardsStr = PlayerEconomy.Instance.Shards.ToString();
        string energyStr = $"{PlayerEconomy.Instance.Energy}/{PlayerEconomy.Instance.MaxEnergy}";

        // Update UI
        if (coinsTMP != null) coinsTMP.text = coinsStr;
        if (shardsTMP != null) shardsTMP.text = shardsStr;
        if (energyTMP != null) energyTMP.text = energyStr;

        Log($"UI Updated: {coinsStr} coins, {shardsStr} shards, {energyStr} energy");
    }

    /// <summary>
    /// ✅ Manual refresh button (optional)
    /// </summary>
    [ContextMenu("Force Refresh")]
    public void ForceRefresh()
    {
        if (PlayerEconomy.Instance != null)
        {
            Refresh();
            Debug.Log("[EconomyUI] Force refreshed");
        }
        else
        {
            Debug.LogError("[EconomyUI] Cannot force refresh: PlayerEconomy not found");
        }
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[EconomyUI] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[EconomyUI] {message}");
    }
}