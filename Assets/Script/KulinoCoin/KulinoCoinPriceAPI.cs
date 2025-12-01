using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ FIXED v5.0: Kulino Coin Price API dengan fallback mechanism
/// - Multi-source price fetching
/// - Better error handling
/// - Fallback to default price
/// </summary>
public class KulinoCoinPriceAPI : MonoBehaviour
{
    public static KulinoCoinPriceAPI Instance { get; private set; }

    [Header("🌐 API Settings")]
    [Tooltip("Primary API - CoinGecko")]
    public string primaryApiUrl = "https://api.coingecko.com/api/v3/simple/price";
    
    [Tooltip("Contract address Kulino Coin")]
    public string kulinoContractAddress = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
    
    [Tooltip("Alternative: Manual price URL (if CoinGecko fails)")]
    public string fallbackPriceUrl = "";

    [Header("💰 Current Price")]
    [Tooltip("Harga Kulino Coin dalam IDR (per 1 KC)")]
    public double kulinoCoinPriceIDR = 1500.0; // Default: 1 KC = Rp 1,500

    [Header("⏱️ Update Settings")]
    [Tooltip("Update price setiap N detik")]
    public float updateInterval = 300f; // 5 menit

    [Header("🔄 Cache Settings")]
    public float cacheValidDuration = 60f;
    private double cachedPrice = 0;
    private float lastFetchTime = 0;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    // Events
    public event Action<double> OnPriceUpdated;

    // State
    private bool isFetching = false;
    private int fetchAttempts = 0;
    private const int maxFetchAttempts = 3;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // ✅ CRITICAL: Detach before DontDestroyOnLoad
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
        gameObject.name = "[KulinoCoinPriceAPI - PERSISTENT]";
        
        Log("✅ KulinoCoinPriceAPI initialized");
    }

    void Start()
    {
        // Initial fetch with delay
        StartCoroutine(InitialFetchDelayed());
        
        // Start periodic update
        if (updateInterval > 0)
        {
            InvokeRepeating(nameof(FetchPrice), updateInterval, updateInterval);
        }
    }

    IEnumerator InitialFetchDelayed()
    {
        yield return new WaitForSeconds(2f);
        FetchPrice();
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
        fetchAttempts = 0;
        
        Log("🔄 Fetching Kulino Coin price...");

        // ✅ Method 1: Try CoinGecko with Solana platform ID
        bool success = false;
        
        for (int attempt = 0; attempt < maxFetchAttempts && !success; attempt++)
        {
            if (attempt > 0)
            {
                Log($"Retry attempt {attempt + 1}/{maxFetchAttempts}");
                yield return new WaitForSeconds(2f);
            }
            
            success = yield return StartCoroutine(TryCoinGeckoAPI());
        }

        // ✅ Method 2: If CoinGecko fails, use fallback
        if (!success && !string.IsNullOrEmpty(fallbackPriceUrl))
        {
            Log("Trying fallback API...");
            success = yield return StartCoroutine(TryFallbackAPI());
        }

        // ✅ Method 3: If all fails, use default
        if (!success)
        {
            LogWarning($"All price fetch attempts failed. Using default: Rp {kulinoCoinPriceIDR:N2}");
            
            // Use default but mark as valid
            if (cachedPrice == 0)
            {
                cachedPrice = kulinoCoinPriceIDR;
                lastFetchTime = Time.time;
            }
        }

        isFetching = false;
    }

    /// <summary>
    /// ✅ NEW: Try CoinGecko API with proper Solana format
    /// </summary>
    IEnumerator TryCoinGeckoAPI()
    {
        // ✅ FIXED: Use correct CoinGecko endpoint for Solana tokens
        string apiUrl = $"https://api.coingecko.com/api/v3/simple/token_price/solana?contract_addresses={kulinoContractAddress}&vs_currencies=idr";
        
        Log($"API URL: {apiUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.timeout = 10;
            
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"CoinGecko API failed: {request.error}");
                yield return false;
            }
            else
            {
                try
                {
                    string response = request.downloadHandler.text;
                    Log($"API Response: {response}");

                    double price = ParseCoinGeckoResponse(response);
                    
                    if (price > 0)
                    {
                        kulinoCoinPriceIDR = price;
                        cachedPrice = price;
                        lastFetchTime = Time.time;
                        
                        OnPriceUpdated?.Invoke(price);
                        Log($"✅ Price updated: 1 KC = Rp {price:N2}");
                        
                        yield return true;
                    }
                    else
                    {
                        LogError("Invalid price from CoinGecko");
                        yield return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Parse error: {ex.Message}");
                    yield return false;
                }
            }
        }
    }

    /// <summary>
    /// ✅ FIXED: Parse CoinGecko response with better error handling
    /// </summary>
    double ParseCoinGeckoResponse(string json)
    {
        try
        {
            // Expected format: {"contract_address_lowercase":{"idr":1500.0}}
            string addressLower = kulinoContractAddress.ToLower();
            
            // Check if contract address exists in response
            if (!json.Contains(addressLower))
            {
                LogError($"Contract address {addressLower} not found in response");
                LogError($"Response: {json}");
                return 0;
            }

            // Find IDR value
            string searchPattern = $"\"{addressLower}\"";
            int contractIndex = json.IndexOf(searchPattern);
            
            if (contractIndex < 0)
            {
                LogError("Contract data not found");
                return 0;
            }

            // Find "idr": value
            int idrIndex = json.IndexOf("\"idr\":", contractIndex);
            if (idrIndex < 0)
            {
                LogError("IDR price not found");
                return 0;
            }

            // Extract price value
            int valueStart = idrIndex + 6; // Skip "idr":
            int valueEnd = json.IndexOfAny(new char[] { ',', '}', ' ' }, valueStart);
            
            if (valueEnd < 0) valueEnd = json.Length;

            string priceStr = json.Substring(valueStart, valueEnd - valueStart).Trim();
            
            if (double.TryParse(priceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double price))
            {
                return price;
            }
            else
            {
                LogError($"Failed to parse price: '{priceStr}'");
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
    /// ✅ NEW: Fallback API (custom price endpoint)
    /// </summary>
    IEnumerator TryFallbackAPI()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(fallbackPriceUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"Fallback API failed: {request.error}");
                yield return false;
            }
            else
            {
                try
                {
                    string response = request.downloadHandler.text;
                    
                    // Assume simple JSON: {"price": 1500.0}
                    if (response.Contains("price"))
                    {
                        int priceIndex = response.IndexOf("\"price\":");
                        if (priceIndex >= 0)
                        {
                            int valueStart = priceIndex + 8;
                            int valueEnd = response.IndexOfAny(new char[] { ',', '}' }, valueStart);
                            string priceStr = response.Substring(valueStart, valueEnd - valueStart).Trim();
                            
                            if (double.TryParse(priceStr, out double price) && price > 0)
                            {
                                kulinoCoinPriceIDR = price;
                                cachedPrice = price;
                                lastFetchTime = Time.time;
                                OnPriceUpdated?.Invoke(price);
                                
                                Log($"✅ Fallback price: Rp {price:N2}");
                                yield return true;
                            }
                        }
                    }
                    
                    yield return false;
                }
                catch (Exception ex)
                {
                    LogError($"Fallback parse error: {ex.Message}");
                    yield return false;
                }
            }
        }
    }

    /// <summary>
    /// ✅ Konversi Rupiah ke Kulino Coin
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
    /// ✅ Konversi Kulino Coin ke Rupiah
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
        StartCoroutine(FetchPriceCoroutine());
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
        Debug.Log($"Fetch Attempts: {fetchAttempts}");
        Debug.Log("================================");
    }

    [ContextMenu("🔧 Force Use Default Price")]
    void Context_ForceDefault()
    {
        kulinoCoinPriceIDR = 1500.0;
        cachedPrice = 1500.0;
        lastFetchTime = Time.time;
        Debug.Log("[KulinoCoinPrice] ✓ Forced to default: Rp 1,500");
    }
}   