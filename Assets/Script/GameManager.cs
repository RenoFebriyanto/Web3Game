using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// FIXED: GameManager dengan Singleton + Fix float-to-int conversion
/// Handles Phantom wallet integration untuk Kulino Coin rewards
/// </summary>
public class GameManager : MonoBehaviour
{
    // ✅ SINGLETON PATTERN (diperlukan oleh KulinoCoinRewardSystem)
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    [Header("Claim Settings")]
    public string gameId = "unity-demo";

    [Tooltip("Amount dalam SMALLEST UNIT (0.000001 SOL = 1000 lamports untuk example)")]
    public int claimAmount = 1; // ✅ Sudah int, tidak perlu diubah

    [Tooltip("Timeout untuk request claim (detik)")]
    public int claimTimeoutSeconds = 60;

    [Header("Kulino Coin Settings")]
    [Tooltip("Kulino Coin decimals (default 6)")]
    public int kulinoCoinDecimals = 6;

    bool isRequestInProgress = false;
    float requestStartTime = 0f;

    // JS binding (calls global JS function RequestClaim(message))
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestClaim(string message);
#endif

    void Awake()
    {
        // ✅ Setup singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Duplicate instance found! Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimButtonClick);

        SetStatus("Ready");
    }

    void Update()
    {
        // ✅ FIX: Explicit conversion float to int untuk timeout check
        if (isRequestInProgress)
        {
            float elapsedTime = Time.time - requestStartTime;

            if (elapsedTime > (float)claimTimeoutSeconds) // ✅ Cast int to float untuk comparison
            {
                Debug.LogWarning("[GameManager] Claim request timeout, resetting UI.");
                FinishRequest(false, "timeout");
            }
        }
    }

    /// <summary>
    /// ✅ PUBLIC METHOD: Dipanggil dari KulinoCoinRewardSystem
    /// </summary>
    public void OnClaimButtonClick()
    {
        if (isRequestInProgress)
        {
            Debug.LogWarning("[GameManager] Request already in progress!");
            return;
        }

        // Disable UI
        isRequestInProgress = true;
        requestStartTime = Time.time;

        if (claimButton != null)
            claimButton.interactable = false;

        SetStatus("Waiting for wallet signature...");

        // Prepare payload
        var payload = new ClaimPayload()
        {
            address = "", // will be filled by JS (Phantom)
            gameId = gameId,
            amount = claimAmount,
            nonce = Guid.NewGuid().ToString(),
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(payload);

        // Call JS
#if UNITY_WEBGL && !UNITY_EDITOR
        try 
        {
            Debug.Log($"[GameManager] Calling RequestClaim with payload: {json}");
            RequestClaim(json);
        } 
        catch (Exception e) 
        {
            Debug.LogError("[GameManager] RequestClaim JS call failed: " + e);
            FinishRequest(false, "js_call_failed");
        }
#else
        // Editor fallback: simulate response after 1s
        Debug.Log($"[GameManager] [EDITOR] Would call JS RequestClaim: {json}");
        Invoke(nameof(EditorSimulateResult), 1f);
#endif
    }

    /// <summary>
    /// Called from JS when server returns result:
    /// window.unityInstance.SendMessage('GameManager','OnClaimResult', JSON.stringify(result));
    /// </summary>
    public void OnClaimResult(string json)
    {
        Debug.Log($"[GameManager] OnClaimResult received: {json}");

        try
        {
            var res = JsonUtility.FromJson<ClaimResult>(json);

            if (res != null && res.success)
            {
                FinishRequest(true, res.txHash ?? "ok");
            }
            else
            {
                string errorMsg = res != null ? (res.error ?? "unknown") : "parse_error";
                FinishRequest(false, errorMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[GameManager] Failed to parse OnClaimResult: " + e);
            FinishRequest(false, "parse_exception");
        }
    }

    void FinishRequest(bool ok, string info)
    {
        isRequestInProgress = false;

        if (claimButton != null)
            claimButton.interactable = true;

        string statusMsg = ok ? $"Claim success: {info}" : $"Claim failed: {info}";
        SetStatus(statusMsg);

        Debug.Log($"[GameManager] {statusMsg}");

        // Optional: trigger UI feedback, update coin counter, etc.
        if (ok)
        {
            // Success feedback
            Debug.Log("[GameManager] ✓ Kulino Coin claim successful!");
        }
    }

    void SetStatus(string txt)
    {
        if (statusText != null)
            statusText.text = txt;

        Debug.Log($"[GameManager] Status: {txt}");
    }

    // Editor test helper
    void EditorSimulateResult()
    {
        var fake = new ClaimResult()
        {
            success = true,
            txHash = "EDITOR_FAKE_TX_" + System.DateTime.Now.Ticks
        };

        OnClaimResult(JsonUtility.ToJson(fake));
    }


    // ============================================================
// TAMBAHKAN CODE INI DI GameManager.cs YANG SUDAH ADA
// ============================================================

// Di bagian OnWalletConnected method, TAMBAHKAN:

public void OnWalletConnected(string address) 
{
    walletAddress = address;
    Debug.Log("Wallet connected: " + address);
    
    // Simpan ke PlayerPrefs
    PlayerPrefs.SetString("WalletAddress", address);
    
    // ✅ TAMBAHAN BARU: Initialize KulinoCoinManager
    if (KulinoCoinManager.Instance != null)
    {
        KulinoCoinManager.Instance.Initialize(address);
        Debug.Log("[GameManager] ✓ KulinoCoinManager initialized");
    }
    else
    {
        Debug.LogError("[GameManager] ✗ KulinoCoinManager.Instance tidak ditemukan!");
    }
}

// ============================================================
// OPTIONAL: Tambahkan method untuk refresh Kulino Coin balance
// ============================================================

[ContextMenu("🔄 Refresh Kulino Coin Balance")]
public void RefreshKulinoCoinBalance()
{
    if (KulinoCoinManager.Instance != null)
    {
        KulinoCoinManager.Instance.RefreshBalance();
        Debug.Log("[GameManager] Refreshing Kulino Coin balance...");
    }
    else
    {
        Debug.LogError("[GameManager] KulinoCoinManager not found!");
    }
}



    // ========================================
    // HELPER CLASSES
    // ========================================

    [Serializable]
    class ClaimPayload
    {
        public string address;
        public string gameId;
        public int amount;
        public string nonce;
        public long ts;
    }

    [Serializable]
    class ClaimResult
    {
        public bool success;
        public string error;
        public string txHash;
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Get amount dalam format human-readable (dengan decimals)
    /// </summary>
    public string GetFormattedAmount()
    {
        float amount = (float)claimAmount / Mathf.Pow(10, kulinoCoinDecimals);
        return amount.ToString($"F{kulinoCoinDecimals}");
    }

    [ContextMenu("Test: Trigger Claim")]
    void Context_TestClaim()
    {
        OnClaimButtonClick();
    }
}