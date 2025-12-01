using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ✅ FIXED v6.0: Kulino Coin Price API - Syntax errors fixed
/// - Fixed missing semicolons
/// - Improved error handling
/// - Better fallback mechanism
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
            
            yield return StartCoroutine(TryCoinGeckoAPI((result) => { success = result; })); // ✅ FIXED: Added semicolon
        }

        if (!success && !string.IsNullOrEmpty(fallbackPriceUrl))
        {
            Log("Trying fallback API...");
            yield return StartCoroutine(TryFallbackAPI((result) => { success = result; })); // ✅ FIXED: Added semicolon
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

    IEnumerator TryCoinGeckoAPI(Action<bool> onComplete)
    {
        string apiUrl = $"https://api.coingecko.com/api/v3/simple/token_price/solana?contract_addresses={kulinoContractAddress}&vs_currencies=idr";
        
        Log($"API URL: {apiUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.timeout = 10;
            
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"CoinGecko API failed: {request.error}");
                onComplete?.Invoke(false);
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
                        
                        onComplete?.Invoke(true);
                    }
                    else
                    {
                        LogError("Invalid price from CoinGecko");
                        onComplete?.Invoke(false);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Parse error: {ex.Message}");
                    onComplete?.Invoke(false);
                }
            }
        }
    }

    double ParseCoinGeckoResponse(string json)
    {
        try
        {
            string addressLower = kulinoContractAddress.ToLower();
            
            if (!json.Contains(addressLower))
            {
                LogError($"Contract address {addressLower} not found in response");
                return 0;
            }

            int idrIndex = json.IndexOf("\"idr\":");
            if (idrIndex < 0)
            {
                LogError("IDR price not found");
                return 0;
            }

            int valueStart = idrIndex + 6;
            int valueEnd = json.IndexOfAny(new char[] { ',', '}', ' ' }, valueStart);
            
            if (valueEnd < 0) valueEnd = json.Length;

            string priceStr = json.Substring(valueStart, valueEnd - valueStart).Trim();
            
            if (double.TryParse(priceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double price))
            {
                return price;
            }

            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Parse exception: {ex.Message}");
            return 0;
        }
    }

    IEnumerator TryFallbackAPI(Action<bool> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(fallbackPriceUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogWarning($"Fallback API failed: {request.error}");
                onComplete?.Invoke(false);
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
                                cachedPrice = price;
                                lastFetchTime = Time.time;
                                OnPriceUpdated?.Invoke(price);
                                
                                Log($"✅ Fallback price: Rp {price:N2}");
                                onComplete?.Invoke(true);
                                yield break;
                            }
                        }
                    }
                    
                    onComplete?.Invoke(false);
                }
                catch (Exception ex)
                {
                    LogError($"Fallback parse error: {ex.Message}");
                    onComplete?.Invoke(false);
                }
            }
        }
    }

    public double ConvertIDRToKulinoCoin(double idrAmount)
    {
        if (kulinoCoinPriceIDR <= 0) return 0;
        return idrAmount / kulinoCoinPriceIDR;
    }

    public double ConvertKulinoCoinToIDR(double kcAmount)
    {
        return kcAmount * kulinoCoinPriceIDR;
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
        Debug.Log("================================");
    }
}