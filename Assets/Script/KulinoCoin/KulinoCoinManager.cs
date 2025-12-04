using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ FIXED v8.1: KulinoCoinManager - Scene Detection Fix
/// - Added scene load detection untuk auto-refresh
/// - Force refresh saat kembali ke MainMenu
/// - Better WebGL initialization handling
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("🧪 Testing (Editor Only)")]
    [Tooltip("Paste wallet address (KC holder)")]
    public string testWalletAddress = "";
    public double mockBalance = 95.0;
    public bool useMockData = false;

    [Header("⚙️ Kulino Coin Settings")]
    public string kulinoCoinMintAddress = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
    
    [Header("🌐 RPC Endpoints - OPTIMIZED ORDER")]
    public string[] solanaRpcUrls = new string[]
    {
        "https://solana-mainnet.g.alchemy.com/v2/demo",
        "https://api.mainnet-beta.solana.com",
        "https://solana-api.projectserum.com",
        "https://rpc.ankr.com/solana"
    };

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔄 Settings")]
    public float autoRefreshInterval = 30f;
    public int maxInitRetries = 10;
    public float retryDelay = 2f;
    public int maxRpcRetries = 3;
    public int requestTimeout = 30;

    [Header("🎬 Scene Settings")]
    [Tooltip("Auto refresh saat load scene ini")]
    public string[] refreshOnScenes = new string[] { "MainMenu", "Menu", "Lobby" };

    [Header("🔍 Debug")]
    public bool enableDebugLogs = true;
    public bool enableVerboseLogs = true;

    public event Action<double> OnBalanceUpdated;
    public event Action<string> OnWalletInitialized;

    private string walletAddress;
    private bool isInitialized = false;
    private bool isFetching = false;
    private int initRetryCount = 0;
    private string lastSceneName = "";

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
        
        // ✅ NEW: Subscribe to scene loaded
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Log("✅ KulinoCoinManager initialized");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ NEW: Handle scene transitions
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"🎬 Scene loaded: {scene.name}");
        
        // Skip jika scene sama
        if (lastSceneName == scene.name)
        {
            return;
        }
        
        lastSceneName = scene.name;
        
        // Check if scene needs refresh
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
            Log($"🔄 Scene '{scene.name}' requires refresh - triggering fetch");
            StartCoroutine(DelayedSceneRefresh());
        }
    }

    // ✅ NEW: Delayed refresh saat scene loaded
    IEnumerator DelayedSceneRefresh()
    {
        yield return new WaitForSeconds(1f);
        
        if (isInitialized && !isFetching)
        {
            Log("🔄 Executing scene refresh");
            FetchKulinoCoinBalance();
        }
    }

    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            Log("🌐 WebGL - Waiting for wallet");
            // ✅ CRITICAL: Start checking immediately in WebGL
            StartCoroutine(WaitForWalletWithRetry());
        #else
            Log("🖥️ Editor - Starting detection");
            StartCoroutine(WaitForWalletWithRetry());
        #endif
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            CheckGameManagerWallet();
        }
    }

    IEnumerator WaitForWalletWithRetry()
    {
        Log("🔄 Wallet detection started...");
        
        while (initRetryCount < maxInitRetries && !isInitialized)
        {
            initRetryCount++;
            Log($"⏳ Attempt {initRetryCount}/{maxInitRetries}");
            
            // CHECK 1: Test wallet (Editor)
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(testWalletAddress))
            {
                Log($"🧪 Using test wallet: {ShortenAddress(testWalletAddress)}");
                Initialize(testWalletAddress);
                yield break;
            }
            #endif
            
            // CHECK 2: GameManager
            if (GameManager.Instance != null)
            {
                string addr = GameManager.Instance.GetWalletAddress();
                if (!string.IsNullOrEmpty(addr))
                {
                    Log($"✅ Found from GameManager: {ShortenAddress(addr)}");
                    Initialize(addr);
                    yield break;
                }
            }
            
            // CHECK 3: PlayerPrefs
            string savedAddr = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(savedAddr))
            {
                Log($"✅ Found saved: {ShortenAddress(savedAddr)}");
                Initialize(savedAddr);
                yield break;
            }
            
            yield return new WaitForSeconds(retryDelay);
        }
        
        if (!isInitialized)
        {
            LogWarning($"⚠️ No wallet after {maxInitRetries} attempts");
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
            LogError("❌ Empty wallet!");
            return;
        }

        if (isInitialized && walletAddress == walletAddr)
        {
            Log($"ℹ️ Already initialized: {ShortenAddress(walletAddr)}");
            // ✅ IMPORTANT: Still trigger fetch untuk refresh balance
            if (!isFetching)
            {
                Log("🔄 Triggering refresh for existing wallet");
                FetchKulinoCoinBalance();
            }
            return;
        }

        walletAddress = walletAddr;
        isInitialized = true;

        Log($"✅ INITIALIZED: {ShortenAddress(walletAddress)}");
        Log($"🔗 Mint: {ShortenAddress(kulinoCoinMintAddress)}");

        OnWalletInitialized?.Invoke(walletAddress);

        // ✅ CRITICAL: Force immediate fetch
        Log("🔄 Starting IMMEDIATE balance fetch...");
        StartCoroutine(ForceImmediateFetch());

        if (autoRefreshInterval > 0)
        {
            CancelInvoke(nameof(AutoRefreshBalance));
            InvokeRepeating(nameof(AutoRefreshBalance), autoRefreshInterval, autoRefreshInterval);
            Log($"✓ Auto-refresh: {autoRefreshInterval}s");
        }
    }

    // ✅ Force immediate fetch on init
    IEnumerator ForceImmediateFetch()
    {
        yield return new WaitForSeconds(0.5f);
        FetchKulinoCoinBalance();
    }

    public void FetchKulinoCoinBalance()
    {
        if (!isInitialized)
        {
            LogWarning("⚠️ Not initialized");
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
        Log("═══════════════════════════════════");
        Log("🔄 FETCHING KULINO COIN BALANCE");
        Log($"   Wallet: {ShortenAddress(walletAddress)}");
        Log($"   Mint: {ShortenAddress(kulinoCoinMintAddress)}");
        Log($"   Scene: {SceneManager.GetActiveScene().name}");
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

        // ✅ Try each RPC
        for (int rpcIndex = 0; rpcIndex < solanaRpcUrls.Length && !success; rpcIndex++)
        {
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
                    Log($"✅ SUCCESS from {GetDomainFromUrl(rpcUrl)}");
                    break;
                }
            }
        }

        if (!success)
        {
            LogError($"❌ ALL RPCs FAILED ({totalAttempts} attempts)");
            LogError("💡 Troubleshooting:");
            LogError($"   Wallet: {walletAddress}");
            LogError($"   Mint: {kulinoCoinMintAddress}");
            LogError("   Check wallet has token account");
            
            SetBalance(0);
        }

        isFetching = false;
    }

    IEnumerator FetchFromRpc(string rpcUrl, Action<bool> onComplete)
    {
        string jsonBody = BuildTokenBalanceRequest();

        if (enableVerboseLogs)
        {
            Log($"📤 Request Body:");
            Log(jsonBody);
        }

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
                LogWarning($"   ❌ {GetDomainFromUrl(rpcUrl)}: {request.error} ({elapsed:F1}s)");
                onComplete?.Invoke(false);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            
            Log($"📥 Response ({responseText.Length} chars, {elapsed:F1}s)");
            
            if (enableVerboseLogs)
            {
                Log($"Response: {responseText}");
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

            if (response == null || response.result == null)
            {
                LogError("❌ Invalid response structure");
                return false;
            }

            if (response.result.value == null || response.result.value.Length == 0)
            {
                Log("ℹ️ No token account → Balance: 0 KC");
                Log("💡 Wallet hasn't received Kulino Coin yet");
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

            if (string.IsNullOrEmpty(amountStr))
            {
                LogError("❌ Empty amount");
                return false;
            }

            if (!double.TryParse(amountStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out double rawAmount))
            {
                LogError($"❌ Parse failed: '{amountStr}'");
                return false;
            }

            double balance = rawAmount / Math.Pow(10, decimals);

            Log("═══════════════════════════════════");
            Log($"✅ BALANCE FOUND: {balance:F6} KC");
            Log($"   📊 Raw: {rawAmount}");
            Log($"   📊 Decimals: {decimals}");
            Log($"   📊 Formula: {rawAmount} / 10^{decimals}");
            Log("═══════════════════════════════════");
            
            SetBalance(balance);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"❌ Parse error: {ex.Message}");
            
            if (enableVerboseLogs)
            {
                LogError($"Stack: {ex.StackTrace}");
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
        
        // ✅ CRITICAL: Always invoke event, even if balance sama
        // Ini penting untuk UI refresh
        OnBalanceUpdated?.Invoke(balance);
    }

    void AutoRefreshBalance()
    {
        if (isInitialized && !isFetching)
        {
            Log("🔄 Auto-refresh triggered");
            FetchKulinoCoinBalance();
        }
    }

    public void RefreshBalance()
    {
        Log("🔄 Manual refresh requested");
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

    [ContextMenu("🔄 Force Refresh NOW")]
    void Context_ForceRefresh()
    {
        Log("🔥 FORCE REFRESH TRIGGERED");
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
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Mock Mode: {useMockData}");
        Debug.Log("═══════════════════════════════════");
    }

    [ContextMenu("🧪 Test Wallet Detection")]
    void Context_TestWallet()
    {
        if (string.IsNullOrEmpty(testWalletAddress))
        {
            Debug.LogError("❌ Set testWalletAddress first!");
            return;
        }

        Debug.Log($"🧪 Testing wallet: {ShortenAddress(testWalletAddress)}");
        
        // Reset state
        isInitialized = false;
        kulinoCoinBalance = 0;
        
        // Initialize with test wallet
        Initialize(testWalletAddress);
    }
}