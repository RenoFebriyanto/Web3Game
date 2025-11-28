using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ✅ FIXED: UI Controller untuk Kulino Coin dengan proper initialization
/// REPLACE existing KulinoCoinUI.cs dengan script ini
/// </summary>
public class KulinoCoinUI : MonoBehaviour
{
    [Header("📱 UI References")]
    [Tooltip("Text untuk menampilkan balance Kulino Coin")]
    public TMP_Text kulinoCoinText;

    [Tooltip("Optional: Icon/Image Kulino Coin")]
    public Image kulinoCoinIcon;

    [Header("🔄 Auto Refresh")]
    [Tooltip("Auto refresh balance setiap X detik (0 = disable)")]
    public float autoRefreshInterval = 30f;

    [Header("💫 Animation")]
    [Tooltip("Animate saat balance update")]
    public bool animateOnUpdate = true;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private double currentBalance = 0;
    private double targetBalance = 0;
    private float animationTime = 0.5f;
    private float animationTimer = 0;
    private bool isSubscribed = false;

    void Start()
    {
        Log("🎮 KulinoCoinUI Started");
        
        // Wait for managers then subscribe
        StartCoroutine(InitializeWithRetry());
    }

    void OnEnable()
    {
        // Re-subscribe when enabled
        StartCoroutine(InitializeWithRetry());
    }

    void OnDisable()
    {
        UnsubscribeFromManager();
    }

    void OnDestroy()
    {
        UnsubscribeFromManager();
    }

    /// <summary>
    /// ✅ FIX: Retry mechanism untuk ensure manager ready
    /// </summary>
    IEnumerator InitializeWithRetry()
    {
        int maxRetries = 10;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            if (KulinoCoinManager.Instance != null)
            {
                Log("✅ KulinoCoinManager found!");
                SubscribeToManager();
                
                // Initial display update
                yield return new WaitForSeconds(0.5f);
                ForceUpdateDisplay();
                
                // Setup auto-refresh
                if (autoRefreshInterval > 0)
                {
                    InvokeRepeating(nameof(RefreshDisplay), autoRefreshInterval, autoRefreshInterval);
                }
                
                yield break; // Success, exit coroutine
            }
            
            retryCount++;
            Log($"⏳ Waiting for KulinoCoinManager... ({retryCount}/{maxRetries})");
            yield return new WaitForSeconds(1f);
        }
        
        LogError("❌ KulinoCoinManager not found after retries!");
    }

    void Update()
    {
        // Handle animation
        if (animateOnUpdate && animationTimer > 0)
        {
            animationTimer -= Time.deltaTime;
            float t = 1f - (animationTimer / animationTime);
            
            double displayBalance = Mathf.Lerp(
                (float)currentBalance, 
                (float)targetBalance, 
                t
            );

            UpdateTextDisplay(displayBalance);

            if (animationTimer <= 0)
            {
                currentBalance = targetBalance;
            }
        }
    }

    void SubscribeToManager()
    {
        if (KulinoCoinManager.Instance == null)
        {
            LogWarning("⚠️ Cannot subscribe - Manager is null");
            return;
        }

        if (isSubscribed)
        {
            Log("ℹ️ Already subscribed");
            return;
        }

        // Unsubscribe first (safety)
        KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
        KulinoCoinManager.Instance.OnWalletInitialized -= OnWalletInitialized;
        
        // Subscribe
        KulinoCoinManager.Instance.OnBalanceUpdated += OnBalanceUpdated;
        KulinoCoinManager.Instance.OnWalletInitialized += OnWalletInitialized;
        
        isSubscribed = true;
        Log("✅ Subscribed to KulinoCoinManager events");
    }

    void UnsubscribeFromManager()
    {
        if (KulinoCoinManager.Instance != null && isSubscribed)
        {
            KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
            KulinoCoinManager.Instance.OnWalletInitialized -= OnWalletInitialized;
            isSubscribed = false;
            Log("✓ Unsubscribed from KulinoCoinManager");
        }
    }

    /// <summary>
    /// Callback when balance updated
    /// </summary>
    void OnBalanceUpdated(double newBalance)
    {
        Log($"💰 Balance updated: {newBalance:F6} KC");
        
        if (animateOnUpdate)
        {
            targetBalance = newBalance;
            animationTimer = animationTime;
        }
        else
        {
            currentBalance = newBalance;
            targetBalance = newBalance;
            UpdateTextDisplay(newBalance);
        }
    }

    /// <summary>
    /// Callback when wallet initialized
    /// </summary>
    void OnWalletInitialized(string walletAddress)
    {
        Log($"👛 Wallet initialized: {ShortenAddress(walletAddress)}");
        
        // Trigger immediate refresh after wallet init
        StartCoroutine(RefreshAfterDelay(1f));
    }

    IEnumerator RefreshAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ForceUpdateDisplay();
    }

    /// <summary>
    /// Update UI text dengan format yang benar
    /// </summary>
    void UpdateTextDisplay(double balance)
    {
        if (kulinoCoinText == null)
        {
            LogWarning("⚠️ kulinoCoinText is NULL!");
            return;
        }

        kulinoCoinText.text = FormatBalance(balance);
    }

    /// <summary>
    /// Format balance untuk display
    /// </summary>
    string FormatBalance(double balance)
    {
        // Format sesuai magnitude
        if (balance >= 1000000)
            return $"{(balance / 1000000):F2}M KC";
        else if (balance >= 1000)
            return $"{(balance / 1000):F2}K KC";
        else if (balance >= 1)
            return $"{balance:F2} KC";
        else if (balance > 0)
            return $"{balance:F6} KC";
        else
            return "0.00 KC";
    }

    /// <summary>
    /// Manual refresh display
    /// </summary>
    void RefreshDisplay()
    {
        ForceUpdateDisplay();
    }

    /// <summary>
    /// Force update display from manager
    /// </summary>
    void ForceUpdateDisplay()
    {
        if (KulinoCoinManager.Instance != null)
        {
            double balance = KulinoCoinManager.Instance.GetBalance();
            currentBalance = balance;
            targetBalance = balance;
            UpdateTextDisplay(balance);
            
            Log($"🔄 Force updated: {balance:F6} KC");
        }
        else
        {
            LogWarning("⚠️ Cannot force update - Manager is null");
            UpdateTextDisplay(0);
        }
    }

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10)
            return addr;
        return $"{addr.Substring(0, 4)}...{addr.Substring(addr.Length - 4)}";
    }

    void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[KulinoCoinUI] {msg}");
    }

    void LogWarning(string msg)
    {
        Debug.LogWarning($"[KulinoCoinUI] {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[KulinoCoinUI] {msg}");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public double GetCurrentBalance()
    {
        return currentBalance;
    }

    public void ManualRefresh()
    {
        Log("🔄 Manual refresh triggered");
        ForceUpdateDisplay();
    }

    // ========================================
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("🔄 Force Refresh Now")]
    void Context_ForceRefresh()
    {
        ForceUpdateDisplay();
    }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== KULINO COIN UI STATUS ===");
        Debug.Log($"Subscribed: {isSubscribed}");
        Debug.Log($"Current Balance: {currentBalance:F6}");
        Debug.Log($"Target Balance: {targetBalance:F6}");
        Debug.Log($"Text Component: {(kulinoCoinText != null ? "OK" : "NULL")}");
        Debug.Log($"Manager Exists: {(KulinoCoinManager.Instance != null ? "YES" : "NO")}");
        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log($"Manager Balance: {KulinoCoinManager.Instance.GetBalance():F6}");
            Debug.Log($"Manager Initialized: {KulinoCoinManager.Instance.IsInitialized()}");
        }
        Debug.Log("============================");
    }

    [ContextMenu("🧪 Test: Set Mock Balance")]
    void Context_TestMockBalance()
    {
        double testBalance = Random.Range(50f, 500f);
        OnBalanceUpdated(testBalance);
        Debug.Log($"[KulinoCoinUI] 🧪 Test balance: {testBalance:F2}");
    }
}