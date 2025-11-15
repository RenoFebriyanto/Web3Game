using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// KULINO COIN MANAGER
/// Manages Kulino Coin balance from Solana wallet
/// Author: Kulino Team
/// </summary>
public class KulinoCoinManager : MonoBehaviour
{
    public static KulinoCoinManager Instance { get; private set; }

    [Header("⚙️ Kulino Coin Settings")]
    [Tooltip("Token Mint Address di Solana")]
    public string kulinoCoinMintAddress = "YOUR_TOKEN_MINT_ADDRESS_HERE";

    [Tooltip("Solana RPC URL")]
    public string solanaRpcUrl = "https://api.mainnet-beta.solana.com";

    [Header("💰 Current Balance")]
    public double kulinoCoinBalance = 0;

    [Header("🔍 Debug")]
    public bool enableDebugLogs = true;

    // Events untuk update UI
    public event Action<double> OnBalanceUpdated;

    private string walletAddress;
    private bool isInitialized = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Initialize dengan wallet address
    /// PENTING: Panggil ini dari GameManager setelah wallet connected!
    /// </summary>
    public void Initialize(string walletAddr)
    {
        if (string.IsNullOrEmpty(walletAddr))
        {
            LogError("❌ Wallet address kosong!");
            return;
        }

        walletAddress = walletAddr;
        isInitialized = true;

        Log($"✅ Initialized dengan wallet: {ShortenAddress(walletAddress)}");

        // Langsung fetch balance
        FetchKulinoCoinBalance();
    }

    /// <summary>
    /// Fetch balance dari Solana blockchain
    /// </summary>
    public void FetchKulinoCoinBalance()
    {
        if (!isInitialized)
        {
            LogWarning("⚠️ Belum initialize! Panggil Initialize() dulu.");
            return;
        }

        StartCoroutine(FetchBalanceCoroutine());
    }

    IEnumerator FetchBalanceCoroutine()
    {
        Log("🔄 Fetching Kulino Coin balance...");

        // Build request body untuk Solana RPC
        string jsonBody = BuildTokenBalanceRequest();

        using (UnityWebRequest request = new UnityWebRequest(solanaRpcUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogError($"❌ Gagal fetch balance: {request.error}");
                SetBalance(0);
                yield break;
            }

            // Parse response
            ParseBalanceResponse(request.downloadHandler.text);
        }
    }

    /// <summary>
    /// Build JSON request untuk getTokenAccountsByOwner
    /// </summary>
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

    /// <summary>
    /// Parse response dari Solana RPC
    /// </summary>
    void ParseBalanceResponse(string responseText)
    {
        try
        {
            Log($"📥 Response: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");

            var response = JsonUtility.FromJson<SolanaRpcResponse>(responseText);

            if (response?.result?.value != null && response.result.value.Length > 0)
            {
                // Ambil token account pertama
                var tokenAccount = response.result.value[0];
                string amountStr = tokenAccount.account.data.parsed.info.tokenAmount.amount;
                int decimals = tokenAccount.account.data.parsed.info.tokenAmount.decimals;

                // Convert ke format readable
                double rawAmount = double.Parse(amountStr);
                double balance = rawAmount / Math.Pow(10, decimals);

                SetBalance(balance);
                Log($"✅ Kulino Coin Balance: {balance:F6}");
            }
            else
            {
                // Tidak ada token account = balance 0
                SetBalance(0);
                Log("ℹ️ Tidak ada token account. Balance: 0");
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ Error parsing: {ex.Message}");
            SetBalance(0);
        }
    }

    /// <summary>
    /// Set balance dan trigger event
    /// </summary>
    void SetBalance(double balance)
    {
        kulinoCoinBalance = balance;
        OnBalanceUpdated?.Invoke(balance);
    }

    /// <summary>
    /// Check apakah balance cukup untuk pembelian
    /// </summary>
    public bool HasEnoughBalance(double amount)
    {
        return kulinoCoinBalance >= amount;
    }

    /// <summary>
    /// Get current balance
    /// </summary>
    public double GetBalance()
    {
        return kulinoCoinBalance;
    }

    /// <summary>
    /// Refresh balance (panggil setelah transaksi)
    /// </summary>
    public void RefreshBalance()
    {
        FetchKulinoCoinBalance();
    }

    // ==================== HELPER METHODS ====================

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

    // ==================== CONTEXT MENU (TESTING) ====================

    [ContextMenu("🔄 Test Fetch Balance")]
    void TestFetchBalance()
    {
        FetchKulinoCoinBalance();
    }

    [ContextMenu("📊 Print Current Balance")]
    void TestPrintBalance()
    {
        Debug.Log($"💰 Current Balance: {kulinoCoinBalance:F6} Kulino Coin");
    }
}