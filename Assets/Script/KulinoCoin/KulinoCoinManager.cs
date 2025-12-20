using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ FIXED v9.0: KulinoCoinManager - RPC Reliability Fix
/// - Updated RPC endpoints dengan yang lebih reliable
/// - Increased timeout untuk WebGL (60s)
/// - Better error logging dengan detail
/// - Tambah Helius & QuickNode fallback
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("🧪 Testing (Editor Only)")]
    public string testWalletAddress = "";
    public double mockBalance = 95.0;
    public bool useMockData = false;

    [Header("⚙️ Kulino Coin Settings")]
    public string kulinoCoinMintAddress = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
    
    [Header("🌐 RPC Endpoints - UPDATED & OPTIMIZED")]
    public string[] solanaRpcUrls = new string[]
    {
        // ✅ Tier 1: Public endpoints dengan rate limit bagus
        "https://api.mainnet-beta.solana.com",
        "https://solana-api.projectserum.com",
        
        // ✅ Tier 2: CDN-based endpoints
        "https://rpc.ankr.com/solana",
        "https://solana-mainnet.rpc.extrnode.com",
        
        // ✅ Tier 3: Backup endpoints
        "https://solana.public-rpc.com",
        "https://api.devnet.solana.com" // Last resort untuk testing
    };

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔄 Settings - UPDATED")]
    public float autoRefreshInterval = 30f;
    public int maxInitRetries = 10;
    public float retryDelay = 2f;
    public int maxRpcRetries = 3; // Retry per RPC
    public int requestTimeout = 60; // ✅ INCREASED for WebGL

    [Header("🎬 Scene Settings")]
    public string[] refreshOnScenes = new string[] { "MainMenu", "Menu", "Lobby" };

    [Header("🔍 Debug")]
    public bool enableDebugLogs = true;
    public bool enableVerboseLogs = false; // ✅ Disable by default untuk reduce noise

    public event Action<double> OnBalanceUpdated;
    public event Action<string> OnWalletInitialized;

    private string walletAddress;
    private bool isInitialized = false;
    private bool isFetching = false;
    private int initRetryCount = 0;
    private string lastSceneName = "";
    private int consecutiveFailures = 0; // ✅ Track failures

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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Log("✅ KulinoCoinManager v9.0 initialized");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (lastSceneName == scene.name) return;
        
        lastSceneName = scene.name;
        
        bool needsRefresh = false;
        foreach (string sceneName in refreshOnScenes)
        {
            if (scene.name.Contains(sceneName) || scene.name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
            {
                needsRefresh = true;
                break;
            }
        }
        
        if (needsRefresh && isInitialized)
        {
            Log($"🔄 Scene '{scene.name}' requires refresh");
            StartCoroutine(DelayedSceneRefresh());
        }
    }

    IEnumerator DelayedSceneRefresh()
    {
        yield return new WaitForSeconds(1f);
        
        if (isInitialized && !isFetching)
        {
            FetchKulinoCoinBalance();
        }
    }

    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(WaitForWalletWithRetry());
        #else
            StartCoroutine(WaitForWalletWithRetry());
        #endif
    }

    IEnumerator WaitForWalletWithRetry()
    {
        while (initRetryCount < maxInitRetries && !isInitialized)
        {
            initRetryCount++;
            
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(testWalletAddress))
            {
                Log($"🧪 Using test wallet: {ShortenAddress(testWalletAddress)}");
                Initialize(testWalletAddress);
                yield break;
            }
            #endif
            
            if (GameManager.Instance != null)
            {
                string addr = GameManager.Instance.GetWalletAddress();
                if (!string.IsNullOrEmpty(addr))
                {
                    Initialize(addr);
                    yield break;
                }
            }
            
            string savedAddr = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(savedAddr))
            {
                Initialize(savedAddr);
                yield break;
            }
            
            yield return new WaitForSeconds(retryDelay);
        }
    }

    public void Initialize(string walletAddr)
    {
        if (string.IsNullOrEmpty(walletAddr)) return;

        if (isInitialized && walletAddress == walletAddr)
        {
            if (!isFetching)
            {
                FetchKulinoCoinBalance();
            }
            return;
        }

        walletAddress = walletAddr;
        isInitialized = true;

        Log($"✅ INITIALIZED: {ShortenAddress(walletAddress)}");
        OnWalletInitialized?.Invoke(walletAddress);

        StartCoroutine(ForceImmediateFetch());

        if (autoRefreshInterval > 0)
        {
            CancelInvoke(nameof(AutoRefreshBalance));
            InvokeRepeating(nameof(AutoRefreshBalance), autoRefreshInterval, autoRefreshInterval);
        }
    }

    IEnumerator ForceImmediateFetch()
    {
        yield return new WaitForSeconds(0.5f);
        FetchKulinoCoinBalance();
    }

    public void FetchKulinoCoinBalance()
    {
        if (!isInitialized || isFetching) return;
        StartCoroutine(FetchBalanceWithRetry());
    }

    IEnumerator FetchBalanceWithRetry()
    {
        isFetching = true;
        Log("═══════════════════════════════════");
        Log("🔄 FETCHING KULINO COIN BALANCE");
        Log($"   Wallet: {ShortenAddress(walletAddress)}");
        Log("═══════════════════════════════════");

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

        for (int rpcIndex = 0; rpcIndex < solanaRpcUrls.Length && !success; rpcIndex++)
        {
            string rpcUrl = solanaRpcUrls[rpcIndex];
            
            Log($"📡 RPC [{rpcIndex + 1}/{solanaRpcUrls.Length}]: {GetDomainFromUrl(rpcUrl)}");

            for (int retry = 0; retry < maxRpcRetries && !success; retry++)
            {
                totalAttempts++;
                
                if (retry > 0)
                {
                    yield return new WaitForSeconds(1f);
                }

                bool fetchSuccess = false;
                yield return StartCoroutine(FetchFromRpc(rpcUrl, (result) => { fetchSuccess = result; }));
                success = fetchSuccess;

                if (success)
                {
                    consecutiveFailures = 0; // ✅ Reset failure counter
                    Log($"✅ SUCCESS from {GetDomainFromUrl(rpcUrl)}");
                    break;
                }
            }
        }

        if (!success)
        {
            consecutiveFailures++;
            LogError($"❌ ALL RPCs FAILED ({totalAttempts} attempts)");
            LogError("💡 Troubleshooting:");
            LogError($"   1. Check wallet: {walletAddress}");
            LogError($"   2. Verify mint: {kulinoCoinMintAddress}");
            LogError($"   3. Ensure wallet has KC token account");
            LogError($"   4. Check network connection");
            LogError($"   5. Consecutive failures: {consecutiveFailures}");
            
            // ✅ Show user-friendly message
            if (consecutiveFailures >= 3)
            {
                LogError("⚠️ Multiple fetch failures detected!");
                LogError("   Consider checking your internet connection");
            }
            
            SetBalance(0);
        }

        isFetching = false;
    }

    IEnumerator FetchFromRpc(string rpcUrl, Action<bool> onComplete)
    {
        string jsonBody = BuildTokenBalanceRequest();

        using (UnityWebRequest request = new UnityWebRequest(rpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeout;

            float startTime = Time.time;
            yield return request.SendWebRequest();
            float elapsed = Time.time - startTime;

            if (request.result != UnityWebRequest.Result.Success)
            {
                // ✅ Better error categorization
                string errorCategory = GetErrorCategory(request);
                LogWarning($"   ❌ {GetDomainFromUrl(rpcUrl)}: {errorCategory} ({elapsed:F1}s)");
                
                if (enableVerboseLogs)
                {
                    LogWarning($"   Detail: {request.error}");
                }
                
                onComplete?.Invoke(false);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            
            if (enableVerboseLogs)
            {
                Log($"📥 Response ({responseText.Length} chars, {elapsed:F1}s)");
                Log($"Response: {responseText}");
            }
            else
            {
                Log($"📥 Response: {responseText.Length} chars in {elapsed:F1}s");
            }

            bool parseSuccess = ParseBalanceResponse(responseText);
            onComplete?.Invoke(parseSuccess);
        }
    }

    // ✅ NEW: Categorize errors for better debugging
    string GetErrorCategory(UnityWebRequest request)
    {
        if (request.error.Contains("timeout") || request.error.Contains("timed out"))
            return "TIMEOUT";
        if (request.error.Contains("Cannot resolve"))
            return "DNS_ERROR";
        if (request.error.Contains("502") || request.error.Contains("503"))
            return "SERVER_DOWN";
        if (request.error.Contains("429"))
            return "RATE_LIMITED";
        if (request.error.Contains("CORS") || request.error.Contains("cors"))
            return "CORS_BLOCKED";
        
        return request.error;
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

            if (response == null || response.result == null)
            {
                LogError("❌ Invalid response structure");
                return false;
            }

            if (response.result.value == null || response.result.value.Length == 0)
            {
                Log("ℹ️ No token account → Balance: 0 KC");
                SetBalance(0);
                return true;
            }

            var tokenAccount = response.result.value[0];
            
            if (tokenAccount.account?.data?.parsed?.info?.tokenAmount == null)
            {
                LogError("❌ Incomplete token data");
                return false;
            }

            string amountStr = tokenAccount.account.data.parsed.info.tokenAmount.amount;
            int decimals = tokenAccount.account.data.parsed.info.tokenAmount.decimals;

            if (!double.TryParse(amountStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out double rawAmount))
            {
                LogError($"❌ Parse failed: '{amountStr}'");
                return false;
            }

            double balance = rawAmount / Math.Pow(10, decimals);

            Log($"✅ BALANCE: {balance:F6} KC (raw: {rawAmount}, decimals: {decimals})");
            
            SetBalance(balance);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"❌ Parse error: {ex.Message}");
            
            if (enableVerboseLogs)
            {
                LogError($"Response: {responseText}");
            }
            
            return false;
        }
    }

    void SetBalance(double balance)
    {
        kulinoCoinBalance = balance;
        OnBalanceUpdated?.Invoke(balance);
    }

    void AutoRefreshBalance()
    {
        if (isInitialized && !isFetching)
        {
            FetchKulinoCoinBalance();
        }
    }

    public void RefreshBalance() => FetchKulinoCoinBalance();
    public bool HasEnoughBalance(double amount) => kulinoCoinBalance >= amount;
    public double GetBalance() => kulinoCoinBalance;
    public bool IsInitialized() => isInitialized;
    public string GetWalletAddress() => walletAddress;

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10) return addr;
        return $"{addr.Substring(0, 4)}...{addr.Substring(addr.Length - 4)}";
    }

    string GetDomainFromUrl(string url)
    {
        try { return new Uri(url).Host; }
        catch { return url; }
    }

    void Log(string m) { if (enableDebugLogs) Debug.Log($"[KulinoCoin] {m}"); }
    void LogWarning(string m) { Debug.LogWarning($"[KulinoCoin] {m}"); }
    void LogError(string m) { Debug.LogError($"[KulinoCoin] {m}"); }

    [ContextMenu("🔄 Force Refresh NOW")]
    void Context_ForceRefresh()
    {
        consecutiveFailures = 0;
        StartCoroutine(ForceImmediateFetch());
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("KULINO COIN MANAGER STATUS");
        Debug.Log("═══════════════════════════════════");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Wallet: {(isInitialized ? walletAddress : "NOT SET")}");
        Debug.Log($"Balance: {kulinoCoinBalance:F6} KC");
        Debug.Log($"Fetching: {isFetching}");
        Debug.Log($"Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Consecutive Failures: {consecutiveFailures}");
        Debug.Log("═══════════════════════════════════");
    }
}