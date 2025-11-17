using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ✅ FIXED: Gameplay coin counter (TEMPORARY - reset setiap level)
/// BUKAN total coins dari PlayerEconomy!
/// </summary>
public class CoinCounterUI : MonoBehaviour
{
    public static CoinCounterUI Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Text component untuk display GAMEPLAY coin count")]
    public TMP_Text coinText;

    [Header("Animation Settings")]
    [Tooltip("Durasi counting animation (detik)")]
    public float countDuration = 0.5f;

    [Tooltip("Curve untuk counting animation")]
    public AnimationCurve countCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Format")]
    [Tooltip("Use number formatting? (1,000 vs 1000)")]
    public bool useNumberFormatting = false; // Changed to false for gameplay

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ✅ CRITICAL: Gameplay coins (temporary, reset per level)
    private long gameplayCoins = 0;
    private long targetCoins = 0;
    private Coroutine countCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Auto-find text if not assigned
        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        if (coinText == null)
        {
            coinText = GetComponentInChildren<TMP_Text>();
        }

        if (coinText == null)
        {
            LogError("TMP_Text not found! Assign coinText in Inspector.");
            return;
        }

        // ✅ CRITICAL: Start from 0 every level
        ResetCounter();

        Log("✓ CoinCounterUI initialized (Gameplay Counter)");
    }

    /// <summary>
    /// ✅ Reset counter ke 0 (dipanggil di start level)
    /// </summary>
    public void ResetCounter()
    {
        gameplayCoins = 0;
        targetCoins = 0;
        UpdateText(gameplayCoins);
        
        Log("✓ Counter reset to 0");
    }

    /// <summary>
    /// Add coins dengan counting animation
    /// Called dari CollectibleAnimationManager
    /// </summary>
    public void AddCoins(long amount)
    {
        if (amount <= 0) return;

        targetCoins += amount;

        // Stop previous animation
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
        }

        // Start counting animation
        countCoroutine = StartCoroutine(CountToTarget());

        Log($"✓ Adding {amount} coins (current: {gameplayCoins} → target: {targetCoins})");
    }

    IEnumerator CountToTarget()
    {
        long startValue = gameplayCoins;
        long endValue = targetCoins;

        float elapsed = 0f;

        while (elapsed < countDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / countDuration;
            float curveT = countCurve.Evaluate(t);

            // Lerp value
            gameplayCoins = (long)Mathf.Lerp(startValue, endValue, curveT);

            UpdateText(gameplayCoins);

            yield return null;
        }

        // Ensure final value
        gameplayCoins = endValue;
        UpdateText(gameplayCoins);

        countCoroutine = null;
    }

    void UpdateText(long value)
    {
        if (coinText == null) return;

        if (useNumberFormatting)
        {
            coinText.text = value.ToString("N0"); // 1,000
        }
        else
        {
            coinText.text = value.ToString(); // 1000
        }
    }

    /// <summary>
    /// Get total gameplay coins collected this level
    /// </summary>
    public long GetGameplayCoins()
    {
        return gameplayCoins;
    }

    /// <summary>
    /// ✅ NEW: Save gameplay coins ke PlayerEconomy (dipanggil saat level complete)
    /// </summary>
    public void SaveToPlayerEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogWarning("PlayerEconomy.Instance is NULL!");
            return;
        }

        if (gameplayCoins > 0)
        {
            PlayerEconomy.Instance.AddCoins(gameplayCoins);
            Log($"✓ Saved {gameplayCoins} coins to PlayerEconomy");
        }
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CoinCounterUI] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[CoinCounterUI] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[CoinCounterUI] {message}");
    }

    [ContextMenu("Test: Add 10 Coins")]
    void Test_Add10()
    {
        AddCoins(10);
    }

    [ContextMenu("Test: Add 100 Coins")]
    void Test_Add100()
    {
        AddCoins(100);
    }

    [ContextMenu("Debug: Reset Counter")]
    void Debug_Reset()
    {
        ResetCounter();
    }

    [ContextMenu("Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("=== COIN COUNTER STATUS ===");
        Debug.Log($"Gameplay Coins: {gameplayCoins}");
        Debug.Log($"Target Coins: {targetCoins}");
        Debug.Log($"Is Counting: {countCoroutine != null}");
        Debug.Log("==========================");
    }
}