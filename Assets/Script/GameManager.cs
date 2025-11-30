using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED: GameManager v4.0 - Complete Wallet Integration
/// - Dual notification system (GameManager + KulinoCoinManager)
/// - URL parameter parsing untuk auto-connect
/// - Mobile support dengan retry mechanism
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("🔐 Wallet")]
    private string walletAddress;

    [Header("💰 Claim Settings")]
    public string gameId = "unity-demo";
    public int claimAmount = 1;
    public int claimTimeoutSeconds = 60;

    [Header("🪙 Kulino Coin")]
    public int kulinoCoinDecimals = 6;

    [Header("🎨 UI References")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    private bool isRequestInProgress = false;
    private float requestStartTime = 0f;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestClaim(string message);
#endif

    void Awake()
    {
        // Ultra-strong singleton
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

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[GameManager - PERSISTENT]";

        Debug.Log("[GameManager] ✓ Singleton initialized");
    }

    void Start()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClick);
        }

        SetStatus("Ready");

        // ✅ CRITICAL: Parse URL parameters untuk auto-connect
        StartCoroutine(ParseURLAndConnect());
    }

    void Update()
    {
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
    // ✅ NEW: URL PARAMETER PARSING
    // ========================================
    
    IEnumerator ParseURLAndConnect()
    {
        // Wait for JavaScript bridge to be ready
        yield return new WaitForSeconds(1f);

        string url = GetCurrentURL();
        Debug.Log($"[GameManager] 🔍 Current URL: {url}");

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[GameManager] ⚠️ Could not get URL");
            yield break;
        }

        // Parse wallet parameter
        string walletParam = GetURLParameter(url, "wallet");
        
        if (!string.IsNullOrEmpty(walletParam))
        {
            Debug.Log($"[GameManager] 🎯 Found wallet in URL: {ShortenAddress(walletParam)}");
            
            // ✅ IMMEDIATE NOTIFICATION
            OnWalletConnected(walletParam);
            
            // ✅ RETRY MECHANISM - Ensure KulinoCoinManager catches it
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.5f);
                
                if (KulinoCoinManager.Instance != null)
                {
                    if (!KulinoCoinManager.Instance.IsInitialized())
                    {
                        Debug.Log($"[GameManager] 🔄 Retry #{i+1} - Initializing KulinoCoinManager");
                        KulinoCoinManager.Instance.Initialize(walletParam);
                    }
                    else
                    {
                        Debug.Log("[GameManager] ✅ KulinoCoinManager already initialized");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] ⚠️ No wallet parameter in URL");
        }
    }

    string GetCurrentURL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            return Application.absoluteURL;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Failed to get URL: {e.Message}");
            return "";
        }
#else
        return "http://localhost/game?wallet=TEST_WALLET_ADDRESS";
#endif
    }

    string GetURLParameter(string url, string paramName)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return "";
            
            int queryStart = url.IndexOf('?');
            if (queryStart < 0) return "";
            
            string query = url.Substring(queryStart + 1);
            string[] pairs = query.Split('&');
            
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == paramName)
                {
                    return Uri.UnescapeDataString(keyValue[1]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] URL parse error: {e.Message}");
        }
        
        return "";
    }

    // ========================================
    // WALLET CONNECTION
    // ========================================
    
    public void OnWalletConnected(string address)
    {
        walletAddress = address;
        Debug.Log($"[GameManager] 👛 Wallet connected: {ShortenAddress(address)}");

        PlayerPrefs.SetString("WalletAddress", address);
        PlayerPrefs.Save();

        // ✅ DUAL NOTIFICATION SYSTEM
        InitializeKulinoCoinManager(address);
    }

    void InitializeKulinoCoinManager(string address)
    {
        Debug.Log("[GameManager] 🔄 Initializing KulinoCoinManager...");
        
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.Initialize(address);
            Debug.Log("[GameManager] ✓ KulinoCoinManager initialized with address");
            
            StartCoroutine(FetchBalanceDelayed(1f));
        }
        else
        {
            Debug.LogError("[GameManager] ❌ KulinoCoinManager.Instance NOT FOUND!");
        }
    }

    IEnumerator FetchBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (KulinoCoinManager.Instance != null && KulinoCoinManager.Instance.IsInitialized())
        {
            Debug.Log("[GameManager] 🔄 Triggering balance fetch...");
            KulinoCoinManager.Instance.FetchKulinoCoinBalance();
        }
    }

    public string GetWalletAddress()
    {
        return walletAddress;
    }

    // ========================================
    // CLAIM SYSTEM
    // ========================================

    public void OnClaimButtonClick()
    {
        if (isRequestInProgress)
        {
            Debug.LogWarning("[GameManager] ⚠️ Request already in progress!");
            return;
        }

        Debug.Log("[GameManager] 💰 Claim button clicked!");

        isRequestInProgress = true;
        requestStartTime = Time.time;

        if (claimButton != null)
            claimButton.interactable = false;

        SetStatus("Waiting for wallet signature...");

        var payload = new ClaimPayload()
        {
            address = "",
            gameId = gameId,
            amount = claimAmount,
            nonce = Guid.NewGuid().ToString(),
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(payload);

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
        Debug.Log($"[GameManager] [EDITOR] Would call JS RequestClaim: {json}");
        Invoke(nameof(EditorSimulateResult), 1f);
#endif
    }

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
    // ✅ PHANTOM PAYMENT (SHOP INTEGRATION)
    // ========================================

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

                var shopManager = FindFirstObjectByType<ShopManager>();
                if (shopManager != null)
                {
                    Debug.Log("[GameManager] ✓ Notifying ShopManager");
                    shopManager.OnPaymentConfirmed();
                }

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

    IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
    {
        Debug.Log($"[GameManager] ⏳ Waiting {delay}s before refreshing balance...");
        
        yield return new WaitForSeconds(delay);

        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log("[GameManager] 🔄 Refreshing Kulino Coin balance...");
            KulinoCoinManager.Instance.RefreshBalance();
        }
        else
        {
            Debug.LogWarning("[GameManager] ⚠️ KulinoCoinManager not found");
        }
    }

    [ContextMenu("🔄 Refresh Kulino Coin Balance")]
    public void RefreshKulinoCoinBalance()
    {
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
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

    public string GetFormattedAmount()
    {
        float amount = (float)claimAmount / Mathf.Pow(10, kulinoCoinDecimals);
        return amount.ToString($"F{kulinoCoinDecimals}");
    }

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

    [ContextMenu("📊 Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== GAMEMANAGER STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Wallet: {ShortenAddress(walletAddress)}");
        Debug.Log($"Game ID: {gameId}");
        Debug.Log($"KulinoCoinManager: {(KulinoCoinManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log("==========================");
    }
}