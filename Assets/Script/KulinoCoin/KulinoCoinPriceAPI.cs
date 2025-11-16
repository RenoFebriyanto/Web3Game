using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetch harga Kulino Coin dalam Rupiah dari API
/// </summary>
public class KulinoCoinPriceAPI : MonoBehaviour
{
    public static KulinoCoinPriceAPI Instance { get; private set; }

    [Header("API Settings")]
    [Tooltip("API endpoint untuk fetch harga (contoh: CoinGecko, Binance, etc)")]
    public string priceApiUrl = "https://api.coingecko.com/api/v3/simple/price?ids=kulino&vs_currencies=idr";

    [Tooltip("Update price setiap N detik")]
    public float updateInterval = 60f; // Update tiap 1 menit

    [Header("Current Price")]
    public double kulinoCoinPriceIDR = 1.5; // Default: 1 KC = 1.5 Rupiah

    public event Action<double> OnPriceUpdated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Start periodic update
        InvokeRepeating(nameof(FetchPrice), 0f, updateInterval);
    }

    public void FetchPrice()
    {
        StartCoroutine(FetchPriceCoroutine());
    }

    IEnumerator FetchPriceCoroutine()
    {
        Debug.Log("[KulinoCoinPrice] Fetching price...");

        using (UnityWebRequest request = UnityWebRequest.Get(priceApiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[KulinoCoinPrice] Failed to fetch: {request.error}");
                yield break;
            }

            try
            {
                // Parse response (sesuaikan dengan format API Anda)
                string response = request.downloadHandler.text;
                // Contoh response: {"kulino":{"idr":1.5}}
                
                // Simple parsing (gunakan JsonUtility atau Newtonsoft.Json untuk yang lebih kompleks)
                double price = ParsePrice(response);
                
                if (price > 0)
                {
                    kulinoCoinPriceIDR = price;
                    OnPriceUpdated?.Invoke(price);
                    Debug.Log($"[KulinoCoinPrice] ✓ Updated: 1 KC = Rp {price:N2}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KulinoCoinPrice] Parse error: {ex.Message}");
            }
        }
    }

    double ParsePrice(string json)
    {
        // TODO: Implement proper JSON parsing
        // Contoh sederhana (sesuaikan dengan format API Anda):
        // {"kulino":{"idr":1.5}}
        
        try
        {
            int start = json.IndexOf("\"idr\":") + 6;
            int end = json.IndexOf("}", start);
            string priceStr = json.Substring(start, end - start);
            return double.Parse(priceStr);
        }
        catch
        {
            return kulinoCoinPriceIDR; // Fallback ke harga default
        }
    }

    /// <summary>
    /// Konversi Rupiah ke Kulino Coin
    /// </summary>
    public double ConvertIDRToKulinoCoin(double idrAmount)
    {
        if (kulinoCoinPriceIDR <= 0) return 0;
        return idrAmount / kulinoCoinPriceIDR;
    }

    /// <summary>
    /// Konversi Kulino Coin ke Rupiah
    /// </summary>
    public double ConvertKulinoCoinToIDR(double kcAmount)
    {
        return kcAmount * kulinoCoinPriceIDR;
    }

    [ContextMenu("Test: Fetch Price Now")]
    void Test_FetchPrice()
    {
        FetchPrice();
    }

    [ContextMenu("Test: Convert 15000 IDR to KC")]
    void Test_ConvertIDR()
    {
        double kc = ConvertIDRToKulinoCoin(15000);
        Debug.Log($"15,000 IDR = {kc:F6} Kulino Coin (1 KC = Rp {kulinoCoinPriceIDR:N2})");
    }
}