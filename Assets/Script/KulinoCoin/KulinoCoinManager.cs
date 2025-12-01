using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ FIXED v6.0: KulinoCoinManager - Balance Detection Fixed
/// - Improved RPC endpoint handling
/// - Better error logging for debugging
/// - Fixed parsing for real wallet balance
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("🧪 Testing (Editor Only)")]
    public string testWalletAddress = "";
    public double mockBalance = 100.5;
    public bool useMockData = false;

    [Header("⚙️ Kulino Coin Settings")]
    public string kulinoCoinMintAddress = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
    
    [Header("🌐 RPC Endpoints (Priority Order)")]
    public string[] solanaRpcUrls = new string[]
    {
        "https://api.mainnet-beta.solana.com", // ✅ Best for token accounts
        "https://rpc.ankr.com/solana",
        "https://solana-api.projectserum.com"
    };

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔄 Auto-Refresh Settings")]
    public float autoRefreshInterval = 30f;
    public int maxInitRetries = 10;
    public float retryDelay = 2f;
    public int maxRpcRetries = 3;

    [Header("🔍 Debug")]
    public bool enableDebugLogs = true;
    public bool enableVerboseLogs = false; // ✅ NEW: Extra detailed logs

    public event Action<double> OnBalanceUpdated;
    public event Action<string> OnWalletInitialized;

    private string walletAddress;
    private bool isInitialized = false;
    private bool isFetching = false;
    private int initRetryCount = 0;
    private int currentRpcIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[KulinoCoinManager - PERSISTENT]";
        
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

        // ✅ IMPROVED: Try each RPC with better error handling
        for (int rpcIndex = 0; rpcIndex < solanaRpcUrls.Length && !success; rpcIndex++)
        {
            currentRpcIndex = rpcIndex;
            string rpcUrl = solanaRpcUrls[rpcIndex];
            
            Log($"📡 Trying RPC [{rpcIndex + 1}/{solanaRpcUrls.Length}]: {GetDomainFromUrl(rpcUrl)}");

            for (int retry = 0; retry < maxRpcRetries && !success; retry++)
            {
                totalAttempts++;
                
                if (retry > 0)
                {
                    Log($"   🔄 Retry {retry}/{maxRpcRetries}");
                    yield return new WaitForSeconds(1f);
                }

                bool fetchSuccess = false;
                yield return StartCoroutine(FetchFromRpc(rpcUrl, (result) => { fetchSuccess = result; }));
                success = fetchSuccess;

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
            LogError("💡 Troubleshooting:");
            LogError($"   - Wallet: {ShortenAddress(walletAddress)}");
            LogError($"   - Mint: {ShortenAddress(kulinoCoinMintAddress)}");
            LogError("   - Check if wallet has token account created");
            LogError("   - Verify mint address is correct");
            
            SetBalance(0);
        }

        isFetching = false;
    }

    IEnumerator FetchFromRpc(string rpcUrl, Action<bool> onComplete)
    {
        string jsonBody = BuildTokenBalanceRequest();

        if (enableVerboseLogs)
        {
            Log($"📤 Request body: {jsonBody}");
        }

        using (UnityWebRequest request = new UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"   ❌ {GetDomainFromUrl(rpcUrl)} failed: {request.error} (Code: {request.responseCode})");
                onComplete?.Invoke(false);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            
            if (enableVerboseLogs)
            {
                Log($"📥 Full Response: {responseText}");
            }
            else
            {
                Log($"📥 Response from {GetDomainFromUrl(rpcUrl)} ({responseText.Length} chars)");
            }

            bool parseSuccess = ParseBalanceResponse(responseText);
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
            var response = JsonUtility.FromJson<SolanaRpcResponse>(responseText);

            if (response == null)
            {
                LogError("❌ JsonUtility returned null");
                return false;
            }

            if (response.result == null)
            {
                LogError("❌ Response.result is null");
                if (enableVerboseLogs) Log($"Raw response: {responseText}");
                return false;
            }

            if (response.result.value == null || response.result.value.Length == 0)
            {
                Log("ℹ️ No token account found. Balance: 0 KC");
                Log("💡 This means the wallet doesn't have a Kulino Coin token account yet");
                SetBalance(0);
                return true;
            }

            var tokenAccount = response.result.value[0];
            
            if (tokenAccount.account?.data?.parsed?.info?.tokenAmount == null)
            {
                LogError("❌ Token account structure incomplete");
                if (enableVerboseLogs)
                {
                    Log($"Account: {(tokenAccount.account != null ? "OK" : "NULL")}");
                    Log($"Data: {(tokenAccount.account?.data != null ? "OK" : "NULL")}");
                    Log($"Parsed: {(tokenAccount.account?.data?.parsed != null ? "OK" : "NULL")}");
                }
                return false;
            }

            string amountStr = tokenAccount.account.data.parsed.info.tokenAmount.amount;
            int decimals = tokenAccount.account.data.parsed.info.tokenAmount.decimals;

            if (string.IsNullOrEmpty(amountStr))
            {
                LogError("❌ Amount string is empty");
                return false;
            }

            if (!double.TryParse(amountStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double rawAmount))
            {
                LogError($"❌ Failed to parse amount: '{amountStr}'");
                return false;
            }

            double balance = rawAmount / Math.Pow(10, decimals);

            SetBalance(balance);
            Log($"✅ Balance parsed successfully: {balance:F6} KC");
            Log($"   📊 Raw amount: {rawAmount}");
            Log($"   📊 Decimals: {decimals}");
            Log($"   📊 Calculated: {rawAmount} / 10^{decimals} = {balance}");
            
            return true;
        }
        catch (Exception ex)
        {
            LogError($"❌ Parse error: {ex.Message}");
            LogError($"Stack: {ex.StackTrace}");
            
            if (enableVerboseLogs)
            {
                LogError($"Problematic response: {responseText}");
            }
            
            return false;
        }
    }

    void SetBalance(double balance)
    {
        double oldBalance = kulinoCoinBalance;
        kulinoCoinBalance = balance;

        Log($"💰 Balance updated: {oldBalance:F6} → {balance:F6} KC");
        OnBalanceUpdated?.Invoke(balance);
    }

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
        Debug.Log($"Mock Data: {useMockData}");
        Debug.Log("=========================");
    }

    [ContextMenu("🔧 Test Specific Wallet")]
    void Context_TestWallet()
    {
        if (string.IsNullOrEmpty(testWalletAddress))
        {
            Debug.LogError("[KulinoCoin] Please set testWalletAddress in Inspector first!");
            return;
        }

        Debug.Log($"[KulinoCoin] 🧪 Testing wallet: {ShortenAddress(testWalletAddress)}");
        Initialize(testWalletAddress);
    }

    [ContextMenu("🔍 Toggle Verbose Logs")]
    void Context_ToggleVerbose()
    {
        enableVerboseLogs = !enableVerboseLogs;
        Debug.Log($"[KulinoCoin] Verbose logs: {(enableVerboseLogs ? "ENABLED" : "DISABLED")}");
    }
}