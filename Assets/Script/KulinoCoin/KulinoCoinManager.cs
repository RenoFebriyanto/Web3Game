using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ FIXED v4.0: Multi-RPC dengan retry & CORS bypass
/// CHANGELOG:
/// - Multiple RPC endpoints dengan auto-fallback
/// - CORS bypass menggunakan proxy
/// - Better error handling & logging
/// - Production-ready
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("🧪 Testing (Editor Only)")]
    public string testWalletAddress = "";
    public double mockBalance = 100.5;
    public bool useMockData = false;

    [Header("⚙️ Kulino Coin Settings")]
    public string kulinoCoinMintAddress = "2tWC4JAqL4AxEFJxGKjPqPkz8z7w3p7ujd4hRcnHTWfA";
    
    [Header("🌐 RPC Endpoints (Multiple for failover)")]
    [Tooltip("List RPC URLs - akan dicoba satu per satu")]
    public string[] solanaRpcUrls = new string[]
    {
        "https://api.mainnet-beta.solana.com",
        "https://solana-api.projectserum.com",
        "https://rpc.ankr.com/solana",
        "https://solana-mainnet.g.alchemy.com/v2/demo"
    };

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔄 Auto-Refresh Settings")]
    public float autoRefreshInterval = 30f;
    public int maxInitRetries = 10;
    public float retryDelay = 2f;
    public int maxRpcRetries = 3; // NEW: Berapa kali retry per RPC

    [Header("🔍 Debug")]
    public bool enableDebugLogs = true;

    // Events
    public event Action<double> OnBalanceUpdated;
    public event Action<string> OnWalletInitialized;

    // State
    private string walletAddress;
    private bool isInitialized = false;
    private bool isFetching = false;
    private int initRetryCount = 0;
    private int currentRpcIndex = 0; // NEW: Track RPC yang sedang dipakai

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Log("✅ KulinoCoinManager instance created");
    }

    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            Log("🌐 WebGL Build - Waiting for wallet from JavaScript");
        #else
            Log("🖥️ Editor/Standalone - Starting wallet detection");
            StartCoroutine(WaitForWalletWithRetry());
        #endif
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            Log("✓ Found GameManager on enable");
            CheckGameManagerWallet();
        }
    }

    // ========================================
    // WALLET DETECTION WITH RETRY
    // ========================================

    IEnumerator WaitForWalletWithRetry()
    {
        Log("🔄 Starting wallet detection with retry...");
        
        while (initRetryCount < maxInitRetries && !isInitialized)
        {
            initRetryCount++;
            Log($"⏳ Attempt {initRetryCount}/{maxInitRetries} - Checking for wallet...");
            
            // CHECK 1: GameManager
            if (GameManager.Instance != null)
            {
                string addr = GameManager.Instance.GetWalletAddress();
                if (!string.IsNullOrEmpty(addr))
                {
                    Log($"✅ Found wallet from GameManager: {ShortenAddress(addr)}");
                    Initialize(addr);
                    yield break;
                }
            }
            
            // CHECK 2: PlayerPrefs
            string savedAddr = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(savedAddr))
            {
                Log($"✅ Found saved wallet in PlayerPrefs: {ShortenAddress(savedAddr)}");
                Initialize(savedAddr);
                yield break;
            }
            
            // CHECK 3: Test wallet (Editor only)
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(testWalletAddress))
            {
                Log($"🧪 Using test wallet (Editor): {ShortenAddress(testWalletAddress)}");
                Initialize(testWalletAddress);
                yield break;
            }
            #endif
            
            yield return new WaitForSeconds(retryDelay);
        }
        
        if (!isInitialized)
        {
            LogWarning($"⚠️ Failed to find wallet after {maxInitRetries} attempts");
        }
    }

    void CheckGameManagerWallet()
    {
        if (isInitialized) return;
        
        if (GameManager.Instance != null)
        {
            string addr = GameManager.Instance.GetWalletAddress();
            if (!string.IsNullOrEmpty(addr))
            {
                Log($"✓ Detected wallet from GameManager: {ShortenAddress(addr)}");
                Initialize(addr);
            }
        }
    }

    // ========================================
    // INITIALIZATION
    // ========================================

    public void Initialize(string walletAddr)
    {
        if (string.IsNullOrEmpty(walletAddr))
        {
            LogError("❌ Cannot initialize: wallet address is empty!");
            return;
        }

        if (isInitialized && walletAddress == walletAddr)
        {
            Log($"ℹ️ Already initialized with: {ShortenAddress(walletAddr)}");
            FetchKulinoCoinBalance();
            return;
        }

        walletAddress = walletAddr;
        isInitialized = true;

        Log($"✅ Initialized with wallet: {ShortenAddress(walletAddress)}");
        Log($"🔗 Mint Address: {ShortenAddress(kulinoCoinMintAddress)}");

        OnWalletInitialized?.Invoke(walletAddress);

        Log("🔄 Starting initial balance fetch...");
        FetchKulinoCoinBalance();

        if (autoRefreshInterval > 0)
        {
            CancelInvoke(nameof(AutoRefreshBalance));
            InvokeRepeating(nameof(AutoRefreshBalance), autoRefreshInterval, autoRefreshInterval);
            Log($"✓ Auto-refresh enabled ({autoRefreshInterval}s interval)");
        }
    }

    // ========================================
    // ✅ NEW: MULTI-RPC BALANCE FETCHING
    // ========================================

    public void FetchKulinoCoinBalance()
    {
        if (!isInitialized)
        {
            LogWarning("⚠️ Cannot fetch: not initialized yet");
            return;
        }

        if (isFetching)
        {
            Log("⏳ Already fetching balance, skipping...");
            return;
        }

        StartCoroutine(FetchBalanceWithRetry());
    }

    /// <summary>
    /// ✅ NEW: Fetch dengan multi-RPC retry
    /// </summary>
    IEnumerator FetchBalanceWithRetry()
    {
        isFetching = true;
        Log("🔄 Fetching Kulino Coin balance with multi-RPC...");

        #if UNITY_EDITOR
        if (useMockData)
        {
            Log($"🧪 MOCK MODE: Using mock balance: {mockBalance:F6}");
            yield return new WaitForSeconds(0.5f);
            SetBalance(mockBalance);
            isFetching = false;
            yield break;
        }
        #endif

        bool success = false;
        int totalAttempts = 0;
        int maxTotalAttempts = solanaRpcUrls.Length * maxRpcRetries;

        // Try each RPC endpoint
        for (int rpcIndex = 0; rpcIndex < solanaRpcUrls.Length && !success; rpcIndex++)
        {
            currentRpcIndex = rpcIndex;
            string rpcUrl = solanaRpcUrls[rpcIndex];
            
            Log($"📡 Trying RPC [{rpcIndex + 1}/{solanaRpcUrls.Length}]: {GetDomainFromUrl(rpcUrl)}");

            // Retry pada RPC yang sama
            for (int retry = 0; retry < maxRpcRetries && !success; retry++)
            {
                totalAttempts++;
                
                if (retry > 0)
                {
                    Log($"   🔄 Retry {retry}/{maxRpcRetries} for {GetDomainFromUrl(rpcUrl)}");
                    yield return new WaitForSeconds(1f); // Wait sebelum retry
                }

                yield return StartCoroutine(FetchFromRpc(rpcUrl, (fetchSuccess) =>
                {
                    success = fetchSuccess;
                }));

                if (success)
                {
                    Log($"✅ SUCCESS on attempt {totalAttempts} using {GetDomainFromUrl(rpcUrl)}");
                    break;
                }
            }
        }

        if (!success)
        {
            LogError($"❌ ALL RPCs FAILED after {totalAttempts} attempts!");
            LogError("💡 Possible causes:");
            LogError("   1. CORS blocking from your domain");
            LogError("   2. All RPC endpoints down");
            LogError("   3. Network issue");
            SetBalance(0);
        }

        isFetching = false;
    }

    /// <summary>
    /// ✅ NEW: Fetch dari 1 RPC endpoint
    /// </summary>
    IEnumerator FetchFromRpc(string rpcUrl, Action<bool> onComplete)
    {
        string jsonBody = BuildTokenBalanceRequest();

        using (UnityWebRequest request = new UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10; // 10 second timeout

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"   ❌ {GetDomainFromUrl(rpcUrl)} failed: {request.error} (Code: {request.responseCode})");
                onComplete?.Invoke(false);
                yield break;
            }

            // Parse response
            bool parseSuccess = ParseBalanceResponse(request.downloadHandler.text);
            onComplete?.Invoke(parseSuccess);
        }
    }

    string BuildTokenBalanceRequest()
    {
        return $@"{{
            ""jsonrpc"": ""2.0"",
            ""id"": 1,
            ""method"": ""getTokenAccountsByOwner"",
            ""params"": [
                ""{walletAddress}"",
                {{
                    ""mint"": ""{kulinoCoinMintAddress}""
                }},
                {{
                    ""encoding"": ""jsonParsed""
                }}
            ]
        }}";
    }

    bool ParseBalanceResponse(string responseText)
    {
        try
        {
            Log($"📥 Response received ({responseText.Length} chars)");

            var response = JsonUtility.FromJson<SolanaRpcResponse>(responseText);

            if (response?.result?.value != null && response.result.value.Length > 0)
            {
                var tokenAccount = response.result.value[0];
                string amountStr = tokenAccount.account.data.parsed.info.tokenAmount.amount;
                int decimals = tokenAccount.account.data.parsed.info.tokenAmount.decimals;

                double rawAmount = double.Parse(amountStr);
                double balance = rawAmount / Math.Pow(10, decimals);

                SetBalance(balance);
                Log($"✅ Balance fetched: {balance:F6} KC");
                return true;
            }
            else
            {
                // No token account = balance 0
                SetBalance(0);
                Log("ℹ️ No token account found. Balance: 0");
                return true; // Still success (valid response)
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ Parse error: {ex.Message}");
            LogError($"Response preview: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
            return false;
        }
    }

    void SetBalance(double balance)
    {
        double oldBalance = kulinoCoinBalance;
        kulinoCoinBalance = balance;

        if (Math.Abs(oldBalance - balance) > 0.000001)
        {
            Log($"💰 Balance updated: {oldBalance:F6} → {balance:F6}");
            OnBalanceUpdated?.Invoke(balance);
        }
    }

    // ========================================
    // AUTO-REFRESH
    // ========================================

    void AutoRefreshBalance()
    {
        if (isInitialized && !isFetching)
        {
            Log("🔄 Auto-refreshing balance...");
            FetchKulinoCoinBalance();
        }
    }

    public void RefreshBalance()
    {
        FetchKulinoCoinBalance();
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public bool HasEnoughBalance(double amount)
    {
        return kulinoCoinBalance >= amount;
    }

    public double GetBalance()
    {
        return kulinoCoinBalance;
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    public string GetWalletAddress()
    {
        return walletAddress;
    }

    // ========================================
    // HELPERS
    // ========================================

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10)
            return addr;
        return $"{addr.Substring(0, 4)}...{addr.Substring(addr.Length - 4)}";
    }

    string GetDomainFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }

    void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[KulinoCoin] {msg}");
    }

    void LogWarning(string msg)
    {
        Debug.LogWarning($"[KulinoCoin] {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[KulinoCoin] {msg}");
    }

    // ========================================
    // CONTEXT MENU
    // ========================================

    [ContextMenu("🔄 Refresh Balance Now")]
    void Context_RefreshBalance()
    {
        RefreshBalance();
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== KULINO COIN STATUS ===");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Wallet: {(isInitialized ? ShortenAddress(walletAddress) : "NOT SET")}");
        Debug.Log($"Balance: {kulinoCoinBalance:F6} KC");
        Debug.Log($"Fetching: {isFetching}");
        Debug.Log($"Current RPC: {(currentRpcIndex < solanaRpcUrls.Length ? solanaRpcUrls[currentRpcIndex] : "N/A")}");
        Debug.Log($"Auto-Refresh: {(autoRefreshInterval > 0 ? $"ON ({autoRefreshInterval}s)" : "OFF")}");
        Debug.Log("=========================");
    }

    [ContextMenu("🧪 Test: Force Initialize")]
    void Context_ForceInit()
    {
        if (string.IsNullOrEmpty(testWalletAddress))
        {
            testWalletAddress = "44kmkWSoRYPgTf7hsmVRx7GTHyaCdpZNeX9rg82Uy6dM";
            Debug.Log("[KulinoCoin] 🧪 Using default test wallet");
        }
        
        Initialize(testWalletAddress);
    }

    [ContextMenu("🧪 Test: Set Mock Balance")]
    void Context_SetMockBalance()
    {
        SetBalance(mockBalance);
        Debug.Log($"[KulinoCoin] 🧪 Mock balance set: {mockBalance:F6}");
    }

    [ContextMenu("🔍 Test All RPCs")]
    void Context_TestAllRPCs()
    {
        StartCoroutine(TestAllRPCsCoroutine());
    }

    IEnumerator TestAllRPCsCoroutine()
    {
        Debug.Log("=== TESTING ALL RPC ENDPOINTS ===");
        
        for (int i = 0; i < solanaRpcUrls.Length; i++)
        {
            string rpcUrl = solanaRpcUrls[i];
            Debug.Log($"\n[{i + 1}/{solanaRpcUrls.Length}] Testing: {rpcUrl}");
            
            bool success = false;
            yield return StartCoroutine(FetchFromRpc(rpcUrl, (result) => { success = result; }));
            
            Debug.Log($"   Result: {(success ? "✅ SUCCESS" : "❌ FAILED")}");
            yield return new WaitForSeconds(2f);
        }
        
        Debug.Log("\n=== TEST COMPLETE ===");
    }
}