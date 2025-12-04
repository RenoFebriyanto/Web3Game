using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ✅ FIXED v2.0: KulinoCoinUI - Force Refresh on Enable
/// - Aggressive refresh saat OnEnable
/// - Better subscription handling
/// - Scene transition detection
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

    [Header("🔄 Force Refresh Settings")]
    [Tooltip("Force fetch balance saat UI enabled")]
    public bool forceRefreshOnEnable = true;
    
    [Tooltip("Delay sebelum force refresh (seconds)")]
    public float forceRefreshDelay = 0.5f;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = true;

    private double currentBalance = 0;
    private double targetBalance = 0;
    private float animationTime = 0.5f;
    private float animationTimer = 0;
    private bool isSubscribed = false;

    void OnEnable()
    {
        Log("🔄 UI Enabled - Starting initialization");
        
        // ✅ Subscribe to manager
        SubscribeToManager();
        
        // ✅ CRITICAL: Force immediate display update
        StartCoroutine(ForceRefreshOnEnable());
    }

    void OnDisable()
    {
        Log("⏸️ UI Disabled");
        UnsubscribeFromManager();
    }

    // ✅ NEW: Aggressive refresh saat enable
    IEnumerator ForceRefreshOnEnable()
    {
        Log("🔥 Force refresh sequence started");
        
        // Try immediate update first
        if (KulinoCoinManager.Instance != null)
        {
            double balance = KulinoCoinManager.Instance.GetBalance();
            Log($"📊 Immediate balance: {balance:F6} KC");
            UpdateBalanceDisplay(balance);
        }
        
        // Wait for manager to be ready
        int attempts = 0;
        while (attempts < 10 && KulinoCoinManager.Instance == null)
        {
            attempts++;
            Log($"⏳ Waiting for KulinoCoinManager... ({attempts}/10)");
            yield return new WaitForSeconds(0.5f);
        }
        
        if (KulinoCoinManager.Instance == null)
        {
            LogWarning("⚠️ KulinoCoinManager not found after retries!");
            yield break;
        }
        
        // Force manager to refresh if enabled
        if (forceRefreshOnEnable && KulinoCoinManager.Instance.IsInitialized())
        {
            yield return new WaitForSeconds(forceRefreshDelay);
            
            Log("🔥 Triggering manager refresh");
            KulinoCoinManager.Instance.RefreshBalance();
        }
        
        Log("✓ Force refresh complete");
    }

    void Start()
    {
        Log("▶️ UI Started");
        
        // Subscribe if not already subscribed
        if (!isSubscribed)
        {
            SubscribeToManager();
        }
        
        // Initial update
        StartCoroutine(InitializeUIWithRetry());

        // Auto refresh jika enabled
        if (autoRefreshInterval > 0)
        {
            InvokeRepeating(nameof(RefreshBalance), autoRefreshInterval, autoRefreshInterval);
            Log($"✓ Auto-refresh: every {autoRefreshInterval}s");
        }
    }

    IEnumerator InitializeUIWithRetry()
    {
        int maxRetries = 5;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            if (KulinoCoinManager.Instance != null)
            {
                double currentBalance = KulinoCoinManager.Instance.GetBalance();
                UpdateBalanceDisplay(currentBalance);
                Log($"✓ UI initialized with balance: {currentBalance:F6} KC");
                yield break;
            }
            
            retryCount++;
            Log($"⏳ Waiting for KulinoCoinManager... (attempt {retryCount}/{maxRetries})");
            yield return new WaitForSeconds(1f);
        }
        
        LogWarning("⚠️ Failed to initialize UI - KulinoCoinManager not found after retries");
    }

    void SubscribeToManager()
    {
        if (KulinoCoinManager.Instance != null)
        {
            // Unsubscribe first to avoid duplicates
            KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
            
            // Subscribe
            KulinoCoinManager.Instance.OnBalanceUpdated += OnBalanceUpdated;
            isSubscribed = true;
            
            Log("✓ Subscribed to KulinoCoinManager");
            
            // ✅ CRITICAL: Get current balance immediately
            double balance = KulinoCoinManager.Instance.GetBalance();
            if (balance > 0 || KulinoCoinManager.Instance.IsInitialized())
            {
                Log($"📊 Got balance on subscribe: {balance:F6} KC");
                UpdateBalanceDisplay(balance);
            }
        }
        else
        {
            LogWarning("⚠️ Cannot subscribe - KulinoCoinManager not found");
            isSubscribed = false;
        }
    }

    void UnsubscribeFromManager()
    {
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
            isSubscribed = false;
            Log("✓ Unsubscribed from KulinoCoinManager");
        }
    }

    void Update()
    {
        // Animate balance jika enabled
        if (animateOnUpdate && animationTimer > 0)
        {
            animationTimer -= Time.deltaTime;
            float t = 1f - (animationTimer / animationTime);
            double displayBalance = Mathf.Lerp((float)currentBalance, (float)targetBalance, t);

            if (kulinoCoinText != null)
            {
                kulinoCoinText.text = FormatBalance(displayBalance);
            }

            if (animationTimer <= 0)
            {
                currentBalance = targetBalance;
            }
        }
    }

    /// <summary>
    /// Callback saat balance updated dari KulinoCoinManager
    /// </summary>
    void OnBalanceUpdated(double newBalance)
    {
        Log($"📥 Balance event received: {newBalance:F6} KC");
        UpdateBalanceDisplay(newBalance);
    }

    /// <summary>
    /// Update tampilan balance di UI
    /// </summary>
    void UpdateBalanceDisplay(double balance)
    {
        Log($"🔄 Updating display: {balance:F6} KC");
        
        if (animateOnUpdate)
        {
            // Animate dari current ke target
            targetBalance = balance;
            animationTimer = animationTime;
        }
        else
        {
            // Direct update
            currentBalance = balance;
            targetBalance = balance;

            if (kulinoCoinText != null)
            {
                kulinoCoinText.text = FormatBalance(balance);
            }
        }
    }

    /// <summary>
    /// Format balance untuk display
    /// </summary>
    string FormatBalance(double balance)
    {
        if (balance >= 1000)
        {
            return balance.ToString("N2"); // 1,234.56
        }
        else if (balance >= 1)
        {
            return balance.ToString("F2"); // 12.34
        }
        else
        {
            return balance.ToString("F6"); // 0.123456
        }
    }

    /// <summary>
    /// Manual refresh balance
    /// </summary>
    void RefreshBalance()
    {
        if (KulinoCoinManager.Instance != null && KulinoCoinManager.Instance.IsInitialized())
        {
            Log("🔄 Auto-refresh triggered");
            KulinoCoinManager.Instance.RefreshBalance();
        }
    }

    /// <summary>
    /// Public method untuk manual update (dari button dll)
    /// </summary>
    public void OnRefreshButtonClick()
    {
        Log("🔄 Manual refresh button clicked");
        RefreshBalance();
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

    [ContextMenu("🧪 Test: Force Update Display")]
    void Test_ForceUpdateDisplay()
    {
        if (KulinoCoinManager.Instance != null)
        {
            double balance = KulinoCoinManager.Instance.GetBalance();
            UpdateBalanceDisplay(balance);
            Debug.Log($"[KulinoCoinUI] 🧪 Forced update: {balance:F6} KC");
        }
        else
        {
            Debug.LogError("[KulinoCoinUI] ❌ KulinoCoinManager not found!");
        }
    }

    [ContextMenu("🔄 Test: Trigger Manager Refresh")]
    void Test_TriggerManagerRefresh()
    {
        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log("[KulinoCoinUI] 🔄 Triggering manager refresh");
            KulinoCoinManager.Instance.RefreshBalance();
        }
        else
        {
            Debug.LogError("[KulinoCoinUI] ❌ KulinoCoinManager not found!");
        }
    }

    [ContextMenu("📊 Print UI Status")]
    void Test_PrintStatus()
    {
        Debug.Log("=== KULINO COIN UI STATUS ===");
        Debug.Log($"Subscribed: {isSubscribed}");
        Debug.Log($"Current Balance: {currentBalance:F6} KC");
        Debug.Log($"Target Balance: {targetBalance:F6} KC");
        Debug.Log($"Animation Timer: {animationTimer:F2}s");
        Debug.Log($"Text Component: {(kulinoCoinText != null ? "OK" : "NULL")}");
        Debug.Log($"Manager Available: {(KulinoCoinManager.Instance != null ? "YES" : "NO")}");
        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log($"Manager Balance: {KulinoCoinManager.Instance.GetBalance():F6} KC");
            Debug.Log($"Manager Initialized: {KulinoCoinManager.Instance.IsInitialized()}");
        }
        Debug.Log("=============================");
    }
}