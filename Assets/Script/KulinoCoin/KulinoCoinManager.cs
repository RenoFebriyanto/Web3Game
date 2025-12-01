using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ FIXED v7.0: KulinoCoinManager - 95 KC Detection Fixed
/// - RPC endpoints sama dengan website (script-index.js)
/// - Timeout diperpanjang (30 detik)
/// - Better error logging
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("🧪 Testing (Editor Only)")]
    [Tooltip("Paste wallet address Anda yang punya 95 KC")]
    public string testWalletAddress = "";
    public double mockBalance = 95.0;
    public bool useMockData = false;

    [Header("⚙️ Kulino Coin Settings")]
    public string kulinoCoinMintAddress = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
    
    [Header("🌐 RPC Endpoints (SAME AS WEBSITE)")]
    [Tooltip("✅ FIXED: Urutan sama dengan js/script-index.js")]
    public string[] solanaRpcUrls = new string[]
    {
        "https://solana-mainnet.g.alchemy.com/v2/demo", // ✅ Alchemy (paling stabil)
        "https://api.mainnet-beta.solana.com",
        "https://solana-api.projectserum.com",
        "https://rpc.ankr.com/solana"
    };

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔄 Auto-Refresh Settings")]
    public float autoRefreshInterval = 30f;
    public int maxInitRetries = 10;
    public float retryDelay = 2f;
    public int maxRpcRetries = 2; // ✅ Reduced (cepat pindah ke RPC lain)

    [Header("⏱️ Timeout Settings")]
    [Tooltip("✅ FIXED: Timeout diperpanjang 30 detik")]
    public int requestTimeout = 30; // ✅ 30 detik (lebih dari cukup)

    [Header("🔍 Debug")]
    public bool enableDebugLogs = true;
    public bool enableVerboseLogs = true; // ✅ Enable untuk troubleshooting

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
        
        Log("✅ KulinoCoinManager initialized");
    }

    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            Log("🌐 WebGL - Waiting for wallet from JavaScript");
        #else
            Log("🖥️ Editor - Starting wallet detection");
            StartCoroutine(WaitForWalletWithRetry());
        #endif
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            Log("✓ GameManager found on enable");
            CheckGameManagerWallet();
        }
    }

    IEnumerator WaitForWalletWithRetry()
    {
        Log("🔄 Starting wallet detection...");
        
        while (initRetryCount < maxInitRetries && !isInitialized)
        {
            initRetryCount++;
            Log($"⏳ Attempt {initRetryCount}/{maxInitRetries}");
            
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
                Log($"✅ Found saved wallet: {ShortenAddress(savedAddr)}");
                Initialize(savedAddr);
                yield break;
            }
            
            // CHECK 3: Test wallet (Editor only)
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(testWalletAddress))
            {
                Log($"🧪 Using test wallet: {ShortenAddress(testWalletAddress)}");
                Initialize(testWalletAddress);
                yield break;
            }
            #endif
            
            yield return new WaitForSeconds(retryDelay);
        }
        
        if (!isInitialized)
        {
            LogWarning($"⚠️ No wallet found after {maxInitRetries} attempts");
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
                Log($"✓ Wallet from GameManager: {ShortenAddress(addr)}");
                Initialize(addr);
            }
        }
    }

    public void Initialize(string walletAddr)
    {
        if (string.IsNullOrEmpty(walletAddr))
        {
            LogError("❌ Empty wallet address!");
            return;
        }

        if (isInitialized && walletAddress == walletAddr)
        {
            Log($"ℹ️ Already initialized: {ShortenAddress(walletAddr)}");
            FetchKulinoCoinBalance();
            return;
        }

        walletAddress = walletAddr;
        isInitialized = true;

        Log($"✅ Initialized with: {ShortenAddress(walletAddress)}");
        Log($"🔗 Mint: {ShortenAddress(kulinoCoinMintAddress)}");

        OnWalletInitialized?.Invoke(walletAddress);

        Log("🔄 Starting balance fetch...");
        FetchKulinoCoinBalance();

        if (autoRefreshInterval > 0)
        {
            CancelInvoke(nameof(AutoRefreshBalance));
            InvokeRepeating(nameof(AutoRefreshBalance), autoRefreshInterval, autoRefreshInterval);
            Log($"✓ Auto-refresh enabled ({autoRefreshInterval}s)");
        }
    }

    public void FetchKulinoCoinBalance()
    {
        if (!isInitialized)
        {
            LogWarning("⚠️ Not initialized yet");
            return;
        }

        if (isFetching)
        {
            Log("⏳ Already fetching...");
            return;
        }

        StartCoroutine(FetchBalanceWithRetry());
    }

    IEnumerator FetchBalanceWithRetry()
    {
        isFetching = true;
        Log("🔄 Fetching Kulino Coin balance...");

        #if UNITY_EDITOR
        if (useMockData)
        {
            Log($"🧪 MOCK MODE: {mockBalance:F6} KC");
            yield return new WaitForSeconds(0.5f);
            SetBalance(mockBalance);
            isFetching = false;
            yield break;
        }
        #endif

        bool success = false;
        int totalAttempts = 0;

        // ✅ Try each RPC
        for (int rpcIndex = 0; rpcIndex < solanaRpcUrls.Length && !success; rpcIndex++)
        {
            currentRpcIndex = rpcIndex;
            string rpcUrl = solanaRpcUrls[rpcIndex];
            
            Log($"📡 RPC [{rpcIndex + 1}/{solanaRpcUrls.Length}]: {GetDomainFromUrl(rpcUrl)}");

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
                    Log($"✅ SUCCESS using {GetDomainFromUrl(rpcUrl)}");
                    break;
                }
            }
        }

        if (!success)
        {
            LogError($"❌ ALL RPCs FAILED after {totalAttempts} attempts!");
            LogError("💡 Troubleshooting:");
            LogError($"   Wallet: {ShortenAddress(walletAddress)}");
            LogError($"   Mint: {ShortenAddress(kulinoCoinMintAddress)}");
            LogError("   Check if wallet has token account");
            
            SetBalance(0);
        }

        isFetching = false;
    }

    IEnumerator FetchFromRpc(string rpcUrl, Action<bool> onComplete)
    {
        string jsonBody = BuildTokenBalanceRequest();

        if (enableVerboseLogs)
        {
            Log($"📤 Request: {jsonBody}");
        }

        using (UnityWebRequest request = new UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeout; // ✅ 30 detik

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"   ❌ {GetDomainFromUrl(rpcUrl)}: {request.error}");
                onComplete?.Invoke(false);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            
            if (enableVerboseLogs)
            {
                Log($"📥 Response ({responseText.Length} chars): {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
            }
            else
            {
                Log($"📥 Response from {GetDomainFromUrl(rpcUrl)}: {responseText.Length} chars");
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
                LogError("❌ Result is null");
                return false;
            }

            if (response.result.value == null || response.result.value.Length == 0)
            {
                Log("ℹ️ No token account. Balance: 0 KC");
                Log("💡 Wallet belum punya Kulino Coin token account");
                SetBalance(0);
                return true;
            }

            var tokenAccount = response.result.value[0];
            
            if (tokenAccount.account?.data?.parsed?.info?.tokenAmount == null)
            {
                LogError("❌ Token account structure incomplete");
                return false;
            }

            string amountStr = tokenAccount.account.data.parsed.info.tokenAmount.amount;
            int decimals = tokenAccount.account.data.parsed.info.tokenAmount.decimals;

            if (string.IsNullOrEmpty(amountStr))
            {
                LogError("❌ Amount string empty");
                return false;
            }

            if (!double.TryParse(amountStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double rawAmount))
            {
                LogError($"❌ Parse failed: '{amountStr}'");
                return false;
            }

            double balance = rawAmount / Math.Pow(10, decimals);

            SetBalance(balance);
            Log($"✅ Balance: {balance:F6} KC");
            Log($"   📊 Raw: {rawAmount}");
            Log($"   📊 Decimals: {decimals}");
            Log($"   📊 Calc: {rawAmount} / 10^{decimals} = {balance}");
            
            return true;
        }
        catch (Exception ex)
        {
            LogError($"❌ Parse error: {ex.Message}");
            LogError($"Stack: {ex.StackTrace}");
            
            if (enableVerboseLogs)
            {
                LogError($"Response: {responseText}");
            }
            
            return false;
        }
    }

    void SetBalance(double balance)
    {
        double oldBalance = kulinoCoinBalance;
        kulinoCoinBalance = balance;

        Log($"💰 Balance: {oldBalance:F6} → {balance:F6} KC");
        OnBalanceUpdated?.Invoke(balance);
    }

    void AutoRefreshBalance()
    {
        if (isInitialized && !isFetching)
        {
            Log("🔄 Auto-refresh...");
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

    [ContextMenu("🔄 Refresh Balance")]
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
        Debug.Log($"RPC: {(currentRpcIndex < solanaRpcUrls.Length ? solanaRpcUrls[currentRpcIndex] : "N/A")}");
        Debug.Log($"Mock: {useMockData}");
        Debug.Log("=========================");
    }

    [ContextMenu("🔧 Test Wallet")]
    void Context_TestWallet()
    {
        if (string.IsNullOrEmpty(testWalletAddress))
        {
            Debug.LogError("Set testWalletAddress first!");
            return;
        }

        Debug.Log($"🧪 Testing: {ShortenAddress(testWalletAddress)}");
        Initialize(testWalletAddress);
    }
}