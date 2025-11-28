using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// KULINO COIN MANAGER - ENHANCED v3.0
/// Manages Kulino Coin balance dari Solana wallet
/// 
/// IMPROVEMENTS:
/// ✅ Better initialization with retry
/// ✅ Stronger wallet address detection
/// ✅ Auto-refresh after wallet connect
/// ✅ Fallback mechanisms
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("🧪 Testing (Editor Only)")]
    [Tooltip("Wallet address untuk testing di Editor")]
    public string testWalletAddress = "";
    
    [Tooltip("Mock balance untuk testing (hanya di Editor)")]
    public double mockBalance = 100.5;
    
    [Tooltip("Use mock data untuk testing?")]
    public bool useMockData = false;

    [Header("⚙️ Kulino Coin Settings")]
    [Tooltip("Token Mint Address di Solana")]
    public string kulinoCoinMintAddress = "2tWC4JAqL4AxEFJxGKjPqPkz8z7w3p7ujd4hRcnHTWfA";
    
    [Tooltip("Solana RPC URL")]
    public string solanaRpcUrl = "https://api.mainnet-beta.solana.com";

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔄 Auto-Refresh Settings")]
    [Tooltip("Auto-refresh setiap N detik (0 = disable)")]
    public float autoRefreshInterval = 30f;
    
    [Tooltip("Max retry untuk initialization")]
    public int maxInitRetries = 10;
    
    [Tooltip("Delay antar retry (detik)")]
    public float retryDelay = 2f;

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
        // Start wallet detection
        StartCoroutine(WaitForWalletWithRetry());
    }

    void OnEnable()
    {
        // Subscribe to GameManager wallet events if exists
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
            
            // Wait before retry
            yield return new WaitForSeconds(retryDelay);
        }
        
        // Max retries reached
        if (!isInitialized)
        {
            LogWarning($"⚠️ Failed to find wallet after {maxInitRetries} attempts");
            LogWarning("💡 Wallet will initialize when GameManager connects");
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
            Log($"ℹ️ Already initialized with same wallet: {ShortenAddress(walletAddr)}");
            return;
        }

        walletAddress = walletAddr;
        isInitialized = true;

        Log($"✅ Initialized with wallet: {ShortenAddress(walletAddress)}");
        Log($"🔗 Mint Address: {ShortenAddress(kulinoCoinMintAddress)}");

        // Fire event
        OnWalletInitialized?.Invoke(walletAddress);

        // Fetch balance immediately
        FetchKulinoCoinBalance();

        // Setup auto-refresh
        if (autoRefreshInterval > 0)
        {
            CancelInvoke(nameof(AutoRefreshBalance));
            InvokeRepeating(nameof(AutoRefreshBalance), autoRefreshInterval, autoRefreshInterval);
            Log($"✓ Auto-refresh enabled ({autoRefreshInterval}s interval)");
        }
    }

    // ========================================
    // BALANCE FETCHING
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

        StartCoroutine(FetchBalanceCoroutine());
    }

    IEnumerator FetchBalanceCoroutine()
    {
        isFetching = true;
        Log("🔄 Fetching Kulino Coin balance...");

#if UNITY_EDITOR
        // Mock mode (Editor only)
        if (useMockData)
        {
            Log($"🧪 MOCK MODE: Using mock balance: {mockBalance:F6}");
            yield return new WaitForSeconds(0.5f);
            SetBalance(mockBalance);
            isFetching = false;
            yield break;
        }
#endif

        // Build request
        string jsonBody = BuildTokenBalanceRequest();

        using (UnityWebRequest request = new UnityWebRequest(solanaRpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15; // 15 second timeout

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogError($"❌ Request failed: {request.error}");
                LogError($"Response Code: {request.responseCode}");
                SetBalance(0);
                isFetching = false;
                yield break;
            }

            // Parse response
            ParseBalanceResponse(request.downloadHandler.text);
        }

        isFetching = false;
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

    void ParseBalanceResponse(string responseText)
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
            }
            else
            {
                // No token account = balance 0
                SetBalance(0);
                Log("ℹ️ No token account found. Balance: 0");
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ Parse error: {ex.Message}");
            LogError($"Response: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
            SetBalance(0);
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
        Debug.Log($"Retry Count: {initRetryCount}/{maxInitRetries}");
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
}