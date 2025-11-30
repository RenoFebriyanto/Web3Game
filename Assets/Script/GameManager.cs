using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// FIXED: GameManager v3.0 - No Duplicate Methods
/// Handles Phantom wallet integration + Kulino Coin detection
/// </summary>
public class GameManager : MonoBehaviour
{
    // ✅ SINGLETON PATTERN
    public static GameManager Instance { get; private set; }

    // ========================================
    // WALLET & CLAIM SETTINGS
    // ========================================
    [Header("🔐 Wallet")]
    private string walletAddress;

    [Header("💰 Claim Settings")]
    public string gameId = "unity-demo";
    [Tooltip("Amount in smallest unit")]
    public int claimAmount = 1;
    [Tooltip("Timeout for claim request (seconds)")]
    public int claimTimeoutSeconds = 60;

    [Header("🪙 Kulino Coin")]
    [Tooltip("Kulino Coin decimals (default 6)")]
    public int kulinoCoinDecimals = 6;

    [Header("🎨 UI References")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    // ========================================
    // PRIVATE VARIABLES
    // ========================================
    private bool isRequestInProgress = false;
    private float requestStartTime = 0f;

    // ========================================
    // JS BINDINGS
    // ========================================
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestClaim(string message);
#endif

    // ========================================
    // UNITY LIFECYCLE
    // ========================================
    // Di GameManager.cs - REPLACE Awake() method

void Awake()
{
    // ✅ FIX: Ultra-strong singleton
    if (Instance != null && Instance != this)
    {
        try
        {
            if (Instance.gameObject != null && Instance.enabled)
            {
                Debug.LogWarning($"[GameManager] Valid instance exists - destroying duplicate '{gameObject.name}'");
                Destroy(gameObject);
                return;
            }
        }
        catch
        {
            Debug.LogWarning("[GameManager] Previous instance invalid - taking over");
        }
    }

    Instance = this;

    // ✅ CRITICAL: Mark persistent
    if (transform.parent != null)
    {
        transform.SetParent(null);
    }
    
    DontDestroyOnLoad(gameObject);
    gameObject.name = "[GameManager - PERSISTENT]";

    Debug.Log("[GameManager] ✓ Singleton initialized");
}

// ✅ NEW: Prevent accidental destruction
void OnDestroy()
{
    if (Instance == this)
    {
        Debug.LogWarning("[GameManager] ⚠️ Instance being destroyed!");
        // Don't clear instance immediately - let new scene create new one if needed
    }
}

    void Start()
    {
        // Setup claim button
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClick);
            Debug.Log("[GameManager] ✓ Claim button registered");
        }

        SetStatus("Ready");
    }

    void Update()
    {
        // Check claim timeout
        if (isRequestInProgress)
        {
            float elapsedTime = Time.time - requestStartTime;

            if (elapsedTime > (float)claimTimeoutSeconds)
            {
                Debug.LogWarning("[GameManager] ⏱️ Claim request timeout!");
                FinishRequest(false, "timeout");
            }
        }
    }

    // ========================================
    // WALLET CONNECTION
    // ========================================
    
    /// <summary>
/// ✅ FIXED: Called from JavaScript when wallet connected
/// </summary>
public void OnWalletConnected(string address)
{
    walletAddress = address;
    Debug.Log($"[GameManager] 👛 Wallet connected: {ShortenAddress(address)}");

    // Save to PlayerPrefs
    PlayerPrefs.SetString("WalletAddress", address);
    PlayerPrefs.Save();

    // ✅ NEW: INITIALIZE KULINO COIN MANAGER IMMEDIATELY
    InitializeKulinoCoinManager(address);
}

/// <summary>
/// ✅ NEW: Initialize KulinoCoinManager dengan wallet address
/// </summary>
void InitializeKulinoCoinManager(string address)
{
    Debug.Log("[GameManager] 🔄 Initializing KulinoCoinManager...");
    
    if (KulinoCoinManager.Instance != null)
    {
        // ✅ CRITICAL: Set address dan trigger fetch
        KulinoCoinManager.Instance.Initialize(address);
        Debug.Log("[GameManager] ✓ KulinoCoinManager initialized with address");
        
        // ✅ Force immediate balance fetch
        StartCoroutine(FetchBalanceDelayed(1f));
    }
    else
    {
        Debug.LogError("[GameManager] ❌ KulinoCoinManager.Instance NOT FOUND!");
        Debug.LogError("[GameManager] Make sure GameObject 'KulinoCoinManager' exists in scene");
    }
}

/// <summary>
/// ✅ NEW: Delayed balance fetch untuk ensure initialization complete
/// </summary>
IEnumerator FetchBalanceDelayed(float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (KulinoCoinManager.Instance != null && KulinoCoinManager.Instance.IsInitialized())
    {
        Debug.Log("[GameManager] 🔄 Triggering balance fetch...");
        KulinoCoinManager.Instance.FetchKulinoCoinBalance();
    }
}

    /// <summary>
    /// Get current wallet address
    /// </summary>
    public string GetWalletAddress()
    {
        return walletAddress;
    }

    // ========================================
    // CLAIM SYSTEM
    // ========================================

    /// <summary>
    /// ✅ PUBLIC: Called from KulinoCoinRewardSystem or UI
    /// </summary>
    public void OnClaimButtonClick()
    {
        if (isRequestInProgress)
        {
            Debug.LogWarning("[GameManager] ⚠️ Request already in progress!");
            return;
        }

        Debug.Log("[GameManager] 💰 Claim button clicked!");

        // Disable UI
        isRequestInProgress = true;
        requestStartTime = Time.time;

        if (claimButton != null)
            claimButton.interactable = false;

        SetStatus("Waiting for wallet signature...");

        // Prepare payload
        var payload = new ClaimPayload()
        {
            address = "", // Will be filled by JS (Phantom)
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
            Debug.Log($"[GameManager] 📤 Calling RequestClaim: {json}");
            RequestClaim(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ RequestClaim JS call failed: {e}");
            FinishRequest(false, "js_call_failed");
        }
#else
        // Editor fallback
        Debug.Log($"[GameManager] [EDITOR] Would call JS RequestClaim: {json}");
        Invoke(nameof(EditorSimulateResult), 1f);
#endif
    }

    /// <summary>
    /// ✅ Called from JavaScript when claim result received
    /// Format: window.unityInstance.SendMessage('GameManager','OnClaimResult', JSON.stringify(result))
    /// </summary>
    public void OnClaimResult(string json)
    {
        Debug.Log($"[GameManager] 📥 OnClaimResult received: {json}");

        try
        {
            var res = JsonUtility.FromJson<ClaimResult>(json);

            if (res != null && res.success)
            {
                Debug.Log($"[GameManager] ✅ Claim SUCCESS! TX: {res.txHash}");
                FinishRequest(true, res.txHash ?? "ok");
            }
            else
            {
                string errorMsg = res != null ? (res.error ?? "unknown") : "parse_error";
                Debug.LogError($"[GameManager] ❌ Claim FAILED: {errorMsg}");
                FinishRequest(false, errorMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Failed to parse OnClaimResult: {e}");
            FinishRequest(false, "parse_exception");
        }
    }

    void FinishRequest(bool success, string info)
    {
        isRequestInProgress = false;

        if (claimButton != null)
            claimButton.interactable = true;

        string statusMsg = success ? $"✅ Claim success: {info}" : $"❌ Claim failed: {info}";
        SetStatus(statusMsg);

        Debug.Log($"[GameManager] {statusMsg}");

        if (success)
        {
            // ✅ Refresh Kulino Coin balance after successful claim
            StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
        }
    }

    void SetStatus(string txt)
    {
        if (statusText != null)
            statusText.text = txt;

        Debug.Log($"[GameManager] 📢 Status: {txt}");
    }

    // ========================================
    // PHANTOM PAYMENT (SHOP INTEGRATION)
    // ========================================

    /// <summary>
    /// ✅ Called from JavaScript after Phantom payment
    /// Format: unityInstance.SendMessage('GameManager', 'OnPhantomPaymentResult', json)
    /// </summary>
    public void OnPhantomPaymentResult(string resultJson)
    {
        Debug.Log($"[GameManager] 💳 Payment result received: {resultJson}");

        try
        {
            var result = JsonUtility.FromJson<ClaimResult>(resultJson);

            if (result.success)
            {
                Debug.Log($"[GameManager] ✅ PAYMENT SUCCESS!");
                Debug.Log($"[GameManager] TX Hash: {result.txHash}");
                
                LogPaymentSuccess(result.txHash);

                // Notify ShopManager
                var shopManager = FindFirstObjectByType<ShopManager>();
                if (shopManager != null)
                {
                    Debug.Log("[GameManager] ✓ Notifying ShopManager");
                    shopManager.OnPaymentConfirmed();
                }

                // Refresh balance
                StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
            }
            else
            {
                Debug.LogError($"[GameManager] ❌ PAYMENT FAILED: {result.error}");
                LogPaymentFailure(result.error);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] ❌ Error parsing payment result: {ex.Message}");
            Debug.LogError($"[GameManager] Raw JSON: {resultJson}");
        }
    }

    void LogPaymentSuccess(string txHash)
    {
        Debug.Log("=== 💳 PAYMENT SUCCESS ===");
        Debug.Log($"TX Hash: {txHash}");
        Debug.Log($"Time: {System.DateTime.Now}");
        Debug.Log("=========================");
    }

    void LogPaymentFailure(string error)
    {
        Debug.LogError("=== ❌ PAYMENT FAILED ===");
        Debug.LogError($"Error: {error}");
        Debug.LogError($"Time: {System.DateTime.Now}");
        Debug.LogError("========================");
    }

    // ========================================
    // KULINO COIN BALANCE REFRESH
    // ========================================

    /// <summary>
    /// ✅ FIXED: Single method untuk refresh balance dengan delay
    /// </summary>
    IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
    {
        Debug.Log($"[GameManager] ⏳ Waiting {delay}s before refreshing balance...");
        
        yield return new WaitForSeconds(delay);

        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log("[GameManager] 🔄 Refreshing Kulino Coin balance...");
            KulinoCoinManager.Instance.RefreshBalance();
            Debug.Log("[GameManager] ✓ Balance refresh triggered");
        }
        else
        {
            Debug.LogWarning("[GameManager] ⚠️ KulinoCoinManager not found for balance refresh");
        }
    }

    /// <summary>
    /// Public method untuk manual refresh
    /// </summary>
    [ContextMenu("🔄 Refresh Kulino Coin Balance")]
    public void RefreshKulinoCoinBalance()
    {
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
            Debug.Log("[GameManager] ✓ Manual balance refresh triggered");
        }
        else
        {
            Debug.LogError("[GameManager] ❌ KulinoCoinManager not found!");
        }
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10)
            return addr;
        return $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
    }

    /// <summary>
    /// Get formatted amount (human-readable with decimals)
    /// </summary>
    public string GetFormattedAmount()
    {
        float amount = (float)claimAmount / Mathf.Pow(10, kulinoCoinDecimals);
        return amount.ToString($"F{kulinoCoinDecimals}");
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

    // ========================================
    // DATA CLASSES
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
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("🧪 Test: Trigger Claim")]
    void Context_TestClaim()
    {
        OnClaimButtonClick();
    }

    [ContextMenu("🧪 Test: Simulate Wallet Connect")]
    void Context_TestWalletConnect()
    {
        string testAddress = "8xGxMockWalletForTesting123456789ABC";
        OnWalletConnected(testAddress);
        Debug.Log($"[GameManager] 🧪 Test wallet connected: {ShortenAddress(testAddress)}");
    }

    [ContextMenu("📊 Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== GAMEMANAGER STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Wallet: {ShortenAddress(walletAddress)}");
        Debug.Log($"Game ID: {gameId}");
        Debug.Log($"Claim Amount: {claimAmount}");
        Debug.Log($"Request in Progress: {isRequestInProgress}");
        Debug.Log($"KulinoCoinManager: {(KulinoCoinManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log("==========================");
    }
}