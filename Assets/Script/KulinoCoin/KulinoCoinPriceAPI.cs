using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ FIXED v7.0: Kulino Coin Price API - Complete Fix
/// - Added missing variables (currentPrice, lastSuccessfulFetch, fetchAttempts)
/// - Fixed TryCoinGeckoAPI method signature
/// - Added LogError method
/// - Fixed all variable references
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

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    // ✅ FIXED: Declare ALL missing variables
    private double cachedPrice = 0;
    private float lastFetchTime = 0;
    private double currentPrice = 0;
    private DateTime lastSuccessfulFetch = DateTime.MinValue;
    private int fetchAttempts = 0;
    private bool isFetching = false;
    private const int maxFetchAttempts = 3;

    // Events
    public event Action<double> OnPriceUpdated;

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
        gameObject.name = "[KulinoCoinPriceAPI - PERSISTENT]";
        
        // ✅ Initialize current price with default
        currentPrice = kulinoCoinPriceIDR;
        
        Log("✅ KulinoCoinPriceAPI initialized");
    }

    void Start()
    {
        StartCoroutine(InitialFetchDelayed());
        
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

    public void FetchPrice()
    {
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

        bool success = false;
        
        for (int attempt = 0; attempt < maxFetchAttempts && !success; attempt++)
        {
            if (attempt > 0)
            {
                Log($"Retry attempt {attempt + 1}/{maxFetchAttempts}");
                yield return new WaitForSeconds(2f);
            }
            
            // ✅ FIXED: Call without callback parameter
            yield return StartCoroutine(TryCoinGeckoAPI());
            
            // ✅ Check if fetch was successful
            if (currentPrice > 0)
            {
                success = true;
            }
        }

        if (!success && !string.IsNullOrEmpty(fallbackPriceUrl))
        {
            Log("Trying fallback API...");
            yield return StartCoroutine(TryFallbackAPI());
        }

        if (!success)
        {
            LogWarning($"All price fetch attempts failed. Using default: Rp {kulinoCoinPriceIDR:N2}");
            
            if (cachedPrice == 0)
            {
                cachedPrice = kulinoCoinPriceIDR;
                lastFetchTime = Time.time;
            }
        }

        isFetching = false;
    }

    /// <summary>
    /// ✅ FIXED: Removed callback parameter - direct return via class variables
    /// </summary>
    IEnumerator TryCoinGeckoAPI()
    {
        // ✅ FIXED: Use correct variable name
        string contractLower = kulinoContractAddress.ToLower().Trim();
        
        string url = $"https://api.coingecko.com/api/v3/simple/token_price/solana" +
                     $"?contract_addresses={contractLower}" +
                     $"&vs_currencies=idr";
        
        Log($"API URL: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Log($"API Response: {json}");

                if (json.Contains("rate limit") || json.Contains("429") || json.Contains("Too Many Requests"))
                {
                    LogWarning("⚠️ CoinGecko API rate limited!");
                    yield break;
                }

                string trimmed = json.Trim();
                if (trimmed == "{}" || trimmed == "[]" || string.IsNullOrWhiteSpace(trimmed))
                {
                    LogWarning("⚠️ CoinGecko returned empty response");
                    LogWarning($"  Contract used: {contractLower}");
                    yield break;
                }

                double price = ParseCoinGeckoResponse(json);
                
                if (price > 0)
                {
                    // ✅ FIXED: Update both currentPrice and cached price
                    currentPrice = price;
                    kulinoCoinPriceIDR = price;
                    cachedPrice = price;
                    lastFetchTime = Time.time;
                    lastSuccessfulFetch = DateTime.UtcNow;
                    
                    // ✅ Trigger event
                    OnPriceUpdated?.Invoke(price);
                    
                    Log($"✅ CoinGecko API success: Rp {price:N2}");
                }
                else
                {
                    LogWarning("Invalid price from CoinGecko - will use fallback");
                }
            }
            else
            {
                string error = req.error;
                long responseCode = req.responseCode;
                
                LogWarning($"CoinGecko API failed: {error} (Code: {responseCode})");
                
                if (responseCode == 429)
                {
                    LogWarning("⚠️ Rate limited by CoinGecko");
                }
                else if (responseCode == 404)
                {
                    LogWarning("⚠️ Contract not found on CoinGecko");
                }
            }
        }
    }

    double ParseCoinGeckoResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            LogWarning("Empty response from CoinGecko");
            return 0;
        }

        string trimmed = json.Trim();
        if (trimmed == "{}" || trimmed == "[]")
        {
            LogWarning("CoinGecko returned empty JSON");
            return 0;
        }

        try
        {
            int idrIndex = json.IndexOf("\"idr\"");
            if (idrIndex < 0)
            {
                LogWarning("No 'idr' field in CoinGecko response");
                return 0;
            }

            int colonIndex = json.IndexOf(":", idrIndex);
            if (colonIndex < 0)
            {
                LogWarning("Invalid format - no colon after 'idr'");
                return 0;
            }

            int endIndex = json.IndexOf(",", colonIndex);
            if (endIndex < 0)
            {
                endIndex = json.IndexOf("}", colonIndex);
            }
            
            if (endIndex < 0)
            {
                LogWarning("Invalid format - cannot find end of value");
                return 0;
            }

            string valueStr = json.Substring(colonIndex + 1, endIndex - colonIndex - 1).Trim();
            
            if (double.TryParse(valueStr, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out double price))
            {
                if (price <= 0)
                {
                    LogWarning($"Invalid price from CoinGecko: {price}");
                    return 0;
                }

                Log($"✅ Parsed CoinGecko price: Rp {price:N2}");
                return price;
            }
            else
            {
                LogWarning($"Failed to parse price value: {valueStr}");
                return 0;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"Failed to parse CoinGecko response: {ex.Message}");
            return 0;
        }
    }

    IEnumerator TryFallbackAPI()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(fallbackPriceUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"Fallback API failed: {request.error}");
            }
            else
            {
                try
                {
                    string response = request.downloadHandler.text;
                    
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
                                currentPrice = price;
                                cachedPrice = price;
                                lastFetchTime = Time.time;
                                lastSuccessfulFetch = DateTime.UtcNow;
                                OnPriceUpdated?.Invoke(price);
                                
                                Log($"✅ Fallback price: Rp {price:N2}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Fallback parse error: {ex.Message}");
                }
            }
        }
    }

    public double ConvertIDRToKulinoCoin(double idrAmount)
    {
        double price = GetCurrentPrice();
        if (price <= 0) return 0;
        return idrAmount / price;
    }

    public double ConvertKulinoCoinToIDR(double kcAmount)
    {
        return kcAmount * GetCurrentPrice();
    }

    public double GetCurrentPrice()
    {
        if (!IsCacheValid() && !isFetching)
        {
            FetchPrice();
        }
        return kulinoCoinPriceIDR;
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[KulinoCoinPrice] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[KulinoCoinPrice] {message}");
    }

    // ✅ FIXED: Added missing LogError method
    void LogError(string message)
    {
        Debug.LogError($"[KulinoCoinPrice] {message}");
    }

    [ContextMenu("🔄 Fetch Price Now")]
    void Context_FetchPrice()
    {
        StartCoroutine(FetchPriceCoroutine());
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== KULINO COIN PRICE STATUS ===");
        Debug.Log($"Current Price: Rp {kulinoCoinPriceIDR:N2} per KC");
        Debug.Log($"Cached Price: Rp {cachedPrice:N2}");
        Debug.Log($"Cache Valid: {IsCacheValid()}");
        Debug.Log($"Is Fetching: {isFetching}");
        Debug.Log($"Last Fetch: {lastSuccessfulFetch}");
        Debug.Log("================================");
    }
}