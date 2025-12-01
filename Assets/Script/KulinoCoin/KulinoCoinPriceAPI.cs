using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ IMPROVED: Kulino Coin Price API dengan CoinGecko Integration
/// Fetch harga real-time Kulino Coin dalam Rupiah
/// </summary>
public class KulinoCoinPriceAPI : MonoBehaviour
{
    public static KulinoCoinPriceAPI Instance { get; private set; }

    [Header("🌐 API Settings")]
    [Tooltip("URL API untuk fetch harga Kulino Coin")]
    public string priceApiUrl = "https://api.coingecko.com/api/v3/simple/token_price/solana";
    
    [Tooltip("Contract address Kulino Coin")]
    public string kulinoContractAddress = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";

    [Header("💰 Current Price")]
    [Tooltip("Harga Kulino Coin dalam IDR (per 1 KC)")]
    public double kulinoCoinPriceIDR = 1500.0; // Default: 1 KC = Rp 1,500

    [Header("⏱️ Update Settings")]
    [Tooltip("Update price setiap N detik")]
    public float updateInterval = 300f; // 5 menit

    [Header("🔄 Cache Settings")]
    public float cacheValidDuration = 60f; // Cache valid 1 menit
    private double cachedPrice = 0;
    private float lastFetchTime = 0;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    // Events
    public event Action<double> OnPriceUpdated;

    // State
    private bool isFetching = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Log("✅ KulinoCoinPriceAPI initialized");
    }

    void Start()
    {
        // Initial fetch
        FetchPrice();
        
        // Start periodic update
        if (updateInterval > 0)
        {
            InvokeRepeating(nameof(FetchPrice), updateInterval, updateInterval);
        }
    }

    /// <summary>
    /// Fetch harga Kulino Coin dari API
    /// </summary>
    public void FetchPrice()
    {
        // Check cache
        if (IsCacheValid())
        {
            Log($"Using cached price: Rp {cachedPrice:N2}");
            return;
        }

        if (!isFetching)
        {
            StartCoroutine(FetchPriceCoroutine());
        }
    }

    bool IsCacheValid()
    {
        return cachedPrice > 0 && (Time.time - lastFetchTime) < cacheValidDuration;
    }

    IEnumerator FetchPriceCoroutine()
    {
        isFetching = true;
        Log("🔄 Fetching Kulino Coin price...");

        // Build API URL dengan parameter
        string apiUrl = $"{priceApiUrl}?contract_addresses={kulinoContractAddress}&vs_currencies=idr";
        
        Log($"API URL: {apiUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            // Set timeout
            request.timeout = 10;
            
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogError($"Failed to fetch price: {request.error}");
                
                // Fallback: gunakan harga default atau cached
                if (cachedPrice > 0)
                {
                    kulinoCoinPriceIDR = cachedPrice;
                    LogWarning($"Using cached price: Rp {cachedPrice:N2}");
                }
                else
                {
                    LogWarning($"Using default price: Rp {kulinoCoinPriceIDR:N2}");
                }
            }
            else
            {
                try
                {
                    string response = request.downloadHandler.text;
                    Log($"API Response: {response}");

                    // Parse response
                    double price = ParsePriceFromResponse(response);
                    
                    if (price > 0)
                    {
                        kulinoCoinPriceIDR = price;
                        cachedPrice = price;
                        lastFetchTime = Time.time;
                        
                        OnPriceUpdated?.Invoke(price);
                        Log($"✅ Price updated: 1 KC = Rp {price:N2}");
                    }
                    else
                    {
                        LogError("Invalid price from API");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Parse error: {ex.Message}");
                }
            }
        }

        isFetching = false;
    }

    /// <summary>
    /// Parse harga dari JSON response CoinGecko
    /// Format: {"contract_address_lowercase":{"idr":1500.0}}
    /// </summary>
    double ParsePriceFromResponse(string json)
    {
        try
        {
            // Simple JSON parsing untuk CoinGecko format
            string addressLower = kulinoContractAddress.ToLower();
            
            // Find price value
            string searchKey = $"\"{addressLower}\"";
            int startIndex = json.IndexOf(searchKey);
            
            if (startIndex < 0)
            {
                LogError("Contract address not found in response");
                return 0;
            }

            // Find "idr": value
            int idrIndex = json.IndexOf("\"idr\":", startIndex);
            if (idrIndex < 0)
            {
                LogError("IDR price not found in response");
                return 0;
            }

            // Extract price value
            int valueStart = idrIndex + 6; // Skip "idr":
            int valueEnd = json.IndexOfAny(new char[] { ',', '}' }, valueStart);
            
            if (valueEnd < 0) valueEnd = json.Length;

            string priceStr = json.Substring(valueStart, valueEnd - valueStart).Trim();
            
            if (double.TryParse(priceStr, out double price))
            {
                return price;
            }
            else
            {
                LogError($"Failed to parse price: {priceStr}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            LogError($"Parse exception: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// ✅ NEW: Konversi Rupiah ke Kulino Coin
    /// </summary>
    public double ConvertIDRToKulinoCoin(double idrAmount)
    {
        if (kulinoCoinPriceIDR <= 0)
        {
            LogError("Invalid Kulino Coin price!");
            return 0;
        }

        double kcAmount = idrAmount / kulinoCoinPriceIDR;
        Log($"Convert: Rp {idrAmount:N0} = {kcAmount:F6} KC (rate: Rp {kulinoCoinPriceIDR:N2}/KC)");
        
        return kcAmount;
    }

    /// <summary>
    /// ✅ NEW: Konversi Kulino Coin ke Rupiah
    /// </summary>
    public double ConvertKulinoCoinToIDR(double kcAmount)
    {
        double idrAmount = kcAmount * kulinoCoinPriceIDR;
        Log($"Convert: {kcAmount:F6} KC = Rp {idrAmount:N0} (rate: Rp {kulinoCoinPriceIDR:N2}/KC)");
        
        return idrAmount;
    }

    /// <summary>
    /// Get current price dengan cache check
    /// </summary>
    public double GetCurrentPrice()
    {
        // Fetch jika cache expired
        if (!IsCacheValid() && !isFetching)
        {
            FetchPrice();
        }

        return kulinoCoinPriceIDR;
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[KulinoCoinPrice] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[KulinoCoinPrice] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[KulinoCoinPrice] {message}");
    }

    // ========================================
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("🔄 Fetch Price Now")]
    void Context_FetchPrice()
    {
        FetchPrice();
    }

    [ContextMenu("💰 Test: Convert Rp 15,000 to KC")]
    void Context_TestConvert()
    {
        double kc = ConvertIDRToKulinoCoin(15000);
        Debug.Log($"Rp 15,000 = {kc:F6} Kulino Coin");
        Debug.Log($"Current rate: 1 KC = Rp {kulinoCoinPriceIDR:N2}");
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== KULINO COIN PRICE STATUS ===");
        Debug.Log($"Current Price: Rp {kulinoCoinPriceIDR:N2} per KC");
        Debug.Log($"Cached Price: Rp {cachedPrice:N2}");
        Debug.Log($"Cache Valid: {IsCacheValid()}");
        Debug.Log($"Is Fetching: {isFetching}");
        Debug.Log($"Last Fetch: {(Time.time - lastFetchTime):F1}s ago");
        Debug.Log("================================");
    }
}