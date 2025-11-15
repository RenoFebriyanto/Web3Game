using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI Controller untuk Kulino Coin
/// Attach ke GameObject yang ada di PanelEconomy
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

    private double currentBalance = 0;
    private double targetBalance = 0;
    private float animationTime = 0.5f;
    private float animationTimer = 0;

    void Start()
    {
        // Subscribe ke event dari KulinoCoinManager
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.OnBalanceUpdated += OnBalanceUpdated;
            
            // Update dengan balance yang sudah ada
            UpdateBalanceDisplay(KulinoCoinManager.Instance.GetBalance());
        }
        else
        {
            Debug.LogWarning("[KulinoCoinUI] KulinoCoinManager.Instance belum tersedia!");
        }

        // Auto refresh jika enabled
        if (autoRefreshInterval > 0)
        {
            InvokeRepeating(nameof(RefreshBalance), autoRefreshInterval, autoRefreshInterval);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe untuk hindari memory leak
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.OnBalanceUpdated -= OnBalanceUpdated;
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
        Debug.Log($"[KulinoCoinUI] Balance updated: {newBalance:F6}");
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
        // Format dengan 2 decimal untuk balance besar, 6 untuk balance kecil
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
}