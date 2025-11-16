using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// FIXED: UI Controller untuk Kulino Coin dengan proper initialization
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

    void OnEnable()
    {
        // ✅ Subscribe saat enable (penting untuk UI yang di-toggle)
        SubscribeToManager();
        // Force refresh display
    if (KulinoCoinManager.Instance != null)
    {
        UpdateBalanceDisplay(KulinoCoinManager.Instance.GetBalance());
    }
    }

    void OnDisable()
    {
        // ✅ Unsubscribe saat disable
        UnsubscribeFromManager();
    }

    void Start()
    {
        SubscribeToManager();
    
    // ✅ FIX: Force update display immediately
    if (KulinoCoinManager.Instance != null)
    {
        double currentBalance = KulinoCoinManager.Instance.GetBalance();
        UpdateBalanceDisplay(currentBalance);
        Debug.Log($"[KulinoCoinUI] ✓ Initial balance: {currentBalance:F6}");
    }
    else
    {
        Debug.LogWarning("[KulinoCoinUI] ⚠️ KulinoCoinManager not found at Start");
    }

        // Auto refresh jika enabled
        if (autoRefreshInterval > 0)
        {
            InvokeRepeating(nameof(RefreshBalance), autoRefreshInterval, autoRefreshInterval);
        }
    }

    void SubscribeToManager()
{
    if (KulinoCoinManager.Instance != null)
    {
        // Unsubscribe dulu untuk hindari duplicate
        KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
        // Subscribe
        KulinoCoinManager.Instance.OnBalanceUpdated += OnBalanceUpdated;
        Debug.Log("[KulinoCoinUI] ✓ Subscribed to KulinoCoinManager");
    }
    else
    {
        Debug.LogWarning("[KulinoCoinUI] ⚠️ Cannot subscribe - KulinoCoinManager not found");
    }
}

void UnsubscribeFromManager()
{
    if (KulinoCoinManager.Instance != null)
    {
        KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
        Debug.Log("[KulinoCoinUI] ✓ Unsubscribed");
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
        Log($"Balance updated: {newBalance:F6}");
        UpdateBalanceDisplay(newBalance);
    }

    /// <summary>
    /// Update tampilan balance di UI
    /// </summary>
    void UpdateBalanceDisplay(double balance)
    {
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
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
        }
    }

    /// <summary>
    /// Public method untuk manual update (dari button dll)
    /// </summary>
    public void OnRefreshButtonClick()
    {
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
            UpdateBalanceDisplay(KulinoCoinManager.Instance.GetBalance());
            Debug.Log($"[KulinoCoinUI] 🧪 Forced update: {KulinoCoinManager.Instance.GetBalance():F6}");
        }
    }
}