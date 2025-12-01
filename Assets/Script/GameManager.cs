using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ ULTRA-FIXED: GameManager v5.0 - Unbreakable Singleton
/// - Scene change protection
/// - Wallet persistence
/// - Dynamic KC price integration
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

    // ✅ CRITICAL: Scene persistence tracking
    private static bool isPersistent = false;

    void Awake()
    {
        // ✅ ULTRA-STRONG SINGLETON - Never destroyed
        if (Instance != null)
        {
            if (Instance != this)
            {
                // Check if old instance is valid
                try
                {
                    if (Instance.gameObject != null && Instance.enabled)
                    {
                        Debug.LogWarning($"[GameManager] ⚠️ Duplicate detected on '{gameObject.name}' - destroying");
                        Destroy(gameObject);
                        return;
                    }
                }
                catch
                {
                    // Old instance is broken, take over
                    Debug.Log("[GameManager] 🔄 Taking over from broken instance");
                }
            }
        }

        Instance = this;

        // ✅ CRITICAL: Detach from parent BEFORE DontDestroyOnLoad
        if (transform.parent != null)
        {
            transform.SetParent(null);
            Debug.Log("[GameManager] Detached from parent");
        }
        
        // ✅ CRITICAL: Mark as persistent
        DontDestroyOnLoad(gameObject);
        isPersistent = true;
        gameObject.name = "[★ GameManager - PERSISTENT ★]";

        // ✅ Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[GameManager] ✅ PERSISTENT singleton initialized");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Debug.LogWarning("[GameManager] ⚠️ Main instance destroyed!");
        }
    }

    // ✅ NEW: Re-check on scene load
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");
        
        // ✅ Re-verify singleton status
        if (!isPersistent || gameObject == null)
        {
            Debug.LogError("[GameManager] ❌ CRITICAL: Lost persistence!");
            RecreateInstance();
        }
        
        // ✅ Re-notify wallet if available
        if (!string.IsNullOrEmpty(walletAddress))
        {
            StartCoroutine(RenotifyWalletDelayed());
        }
    }

    IEnumerator RenotifyWalletDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[GameManager] 🔄 Re-notifying wallet: {ShortenAddress(walletAddress)}");
        InitializeKulinoCoinManager(walletAddress);
    }

    void RecreateInstance()
    {
        Debug.Log("[GameManager] 🔧 Attempting to recreate...");
        
        var existing = FindFirstObjectByType<GameManager>();
        if (existing != null && existing != this)
        {
            Instance = existing;
            Debug.Log("[GameManager] ✓ Found existing instance");
        }
        else
        {
            Debug.LogError("[GameManager] ❌ Cannot recreate - original instance lost!");
        }
    }

    void Start()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClick);
        }

        SetStatus("Ready");
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
    // URL PARAMETER PARSING
    // ========================================
    
    IEnumerator ParseURLAndConnect()
    {
        yield return new WaitForSeconds(1f);

        string url = GetCurrentURL();
        Debug.Log($"[GameManager] 🔍 Current URL: {url}");

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[GameManager] ⚠️ Could not get URL");
            yield break;
        }

        string walletParam = GetURLParameter(url, "wallet");
        
        if (!string.IsNullOrEmpty(walletParam))
        {
            Debug.Log($"[GameManager] 🎯 Found wallet in URL: {ShortenAddress(walletParam)}");
            OnWalletConnected(walletParam);
            
            // ✅ Retry mechanism for KulinoCoinManager
            for (int i = 0; i < 10; i++)
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
        return "http://localhost/game?wallet=TEST_WALLET";
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

        InitializeKulinoCoinManager(address);
    }

    void InitializeKulinoCoinManager(string address)
    {
        Debug.Log("[GameManager] 🔄 Initializing KulinoCoinManager...");
        
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.Initialize(address);
            Debug.Log("[GameManager] ✓ KulinoCoinManager initialized");
            
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
            Debug.LogWarning("[GameManager] ⚠️ Request in progress!");
            return;
        }

        Debug.Log("[GameManager] 💰 Claim button clicked!");

        isRequestInProgress = true;
        requestStartTime = Time.time;

        if (claimButton != null)
            claimButton.interactable = false;

        SetStatus("Waiting for signature...");

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
            Debug.Log($"[GameManager] 📤 Calling RequestClaim");
            RequestClaim(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ RequestClaim failed: {e}");
            FinishRequest(false, "js_call_failed");
        }
#else
        Debug.Log($"[GameManager] [EDITOR] Would call JS");
        Invoke(nameof(EditorSimulateResult), 1f);
#endif
    }

    public void OnClaimResult(string json)
    {
        Debug.Log($"[GameManager] 📥 OnClaimResult: {json}");

        try
        {
            var res = JsonUtility.FromJson<ClaimResult>(json);

            if (res != null && res.success)
            {
                Debug.Log($"[GameManager] ✅ SUCCESS! TX: {res.txHash}");
                FinishRequest(true, res.txHash ?? "ok");
            }
            else
            {
                string errorMsg = res != null ? (res.error ?? "unknown") : "parse_error";
                Debug.LogError($"[GameManager] ❌ FAILED: {errorMsg}");
                FinishRequest(false, errorMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Parse error: {e}");
            FinishRequest(false, "parse_exception");
        }
    }

    void FinishRequest(bool success, string info)
    {
        isRequestInProgress = false;

        if (claimButton != null)
            claimButton.interactable = true;

        string statusMsg = success ? $"✅ Success: {info}" : $"❌ Failed: {info}";
        SetStatus(statusMsg);

        if (success)
        {
            StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
        }
    }

    void SetStatus(string txt)
    {
        if (statusText != null)
            statusText.text = txt;
    }

    // ========================================
    // PHANTOM PAYMENT
    // ========================================

    public void OnPhantomPaymentResult(string resultJson)
    {
        Debug.Log($"[GameManager] 💳 Payment result: {resultJson}");

        try
        {
            var result = JsonUtility.FromJson<ClaimResult>(resultJson);

            if (result.success)
            {
                Debug.Log($"[GameManager] ✅ PAYMENT SUCCESS!");
                Debug.Log($"[GameManager] TX: {result.txHash}");
                
                var shopManager = FindFirstObjectByType<ShopManager>();
                if (shopManager != null)
                {
                    shopManager.OnPaymentConfirmed();
                }

                StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
            }
            else
            {
                Debug.LogError($"[GameManager] ❌ PAYMENT FAILED: {result.error}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] ❌ Parse error: {ex.Message}");
        }
    }

    // ========================================
    // KULINO COIN BALANCE REFRESH
    // ========================================

    IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log("[GameManager] 🔄 Refreshing KC balance...");
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

    void EditorSimulateResult()
    {
        var fake = new ClaimResult()
        {
            success = true,
            txHash = "EDITOR_FAKE_TX"
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
        Debug.Log($"Persistent: {isPersistent}");
        Debug.Log($"Wallet: {ShortenAddress(walletAddress)}");
        Debug.Log($"KulinoCoinManager: {(KulinoCoinManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log("==========================");
    }
}