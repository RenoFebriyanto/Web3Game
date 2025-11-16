using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    private string walletAddress; // Wallet address dari Phantom

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

// ============================================================
// GAMEMANAGER.CS - REPLACE method OnWalletConnected yang ada
// dengan code ini
// ============================================================

/// <summary>
/// Dipanggil dari JavaScript saat wallet connected
/// </summary>
public void OnWalletConnected(string address) 
{
    walletAddress = address;
    Debug.Log("Wallet connected: " + address);
    
    // Update UI atau mulai game logic
    PlayerPrefs.SetString("WalletAddress", address);
    
    // ✅ Initialize KulinoCoinManager
    if (KulinoCoinManager.Instance != null)
    {
        KulinoCoinManager.Instance.Initialize(address);
        Debug.Log("[GameManager] ✓ KulinoCoinManager initialized");
    }
    else
    {
        Debug.LogWarning("[GameManager] ⚠️ KulinoCoinManager.Instance tidak ditemukan!");
        Debug.LogWarning("[GameManager] Pastikan GameObject 'KulinoCoinManager' ada di scene");
    }
}

/// <summary>
/// Get current wallet address
/// </summary>
public string GetWalletAddress() 
{
    return walletAddress;
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

    /// <summary>
/// ✅ NEW: Callback dari JavaScript setelah Phantom payment
/// Dipanggil via: unityInstance.SendMessage('GameManager', 'OnPhantomPaymentResult', json)
/// </summary>
public void OnPhantomPaymentResult(string resultJson)
{
    Debug.Log($"[GameManager] 💰 Payment result received: {resultJson}");
    
    try
    {
        // Reuse ClaimResult class yang sudah ada
        var result = JsonUtility.FromJson<ClaimResult>(resultJson);
        
        if (result.success)
        {
            Debug.Log($"✓✓✓ PAYMENT SUCCESS!");
            Debug.Log($"Transaction: {result.txHash}");
            LogPaymentSuccess(result.txHash);
            
            
            // Notify ShopManager untuk grant reward
            var shopManager = FindFirstObjectByType<ShopManager>();
            if (shopManager != null)
            {
                Debug.Log("[GameManager] ✓ Notifying ShopManager to grant reward");
                // ShopManager akan handle grant reward via OnPaymentSuccess event
            }
            else
            {
                Debug.LogWarning("[GameManager] ⚠️ ShopManager not found");
            }
            
            // Refresh Kulino Coin balance
            if (KulinoCoinManager.Instance != null)
            {
                Debug.Log("[GameManager] 🔄 Refreshing Kulino Coin balance...");
                StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
            }
            
            Debug.Log("[GameManager] ✓ Purchase completed successfully!");
        }
        else
        {

            Debug.LogError($"✗✗✗ PAYMENT FAILED: {result.error}");
            Debug.LogError($"[GameManager] Payment error: {result.error}");
            LogPaymentFailure(result.error);
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[GameManager] ❌ Error parsing payment result: {ex.Message}");
        Debug.LogError($"[GameManager] Raw JSON: {resultJson}");
    }
}

/// <summary>
/// Helper: Refresh balance dengan delay
/// </summary>

IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (KulinoCoinManager.Instance != null)
    {
        KulinoCoinManager.Instance.RefreshBalance();
        Debug.Log("[GameManager] ✓ Kulino Coin balance refreshed");
    }
}

// ✅ NEW METHODS:
void LogPaymentSuccess(string txHash)
{
    Debug.Log("=== PAYMENT SUCCESS ===");
    Debug.Log($"TX Hash: {txHash}");
    Debug.Log($"Time: {System.DateTime.Now}");
    Debug.Log("=======================");
    
    // TODO: Send ke analytics backend
    // Analytics.TrackEvent("payment_success", new Dictionary<string, object> {
    //     { "tx_hash", txHash },
    //     { "timestamp", System.DateTime.UtcNow.ToString() }
    // });
}

void LogPaymentFailure(string error)
{
    Debug.LogError("=== PAYMENT FAILED ===");
    Debug.LogError($"Error: {error}");
    Debug.LogError($"Time: {System.DateTime.Now}");
    Debug.LogError("======================");
    
    // TODO: Send ke error tracking
    // Sentry.CaptureException(new Exception($"Payment failed: {error}"));
}
}