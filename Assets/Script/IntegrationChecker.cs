using UnityEngine;

/// <summary>
/// ✅ INTEGRATION CHECKER
/// Script untuk memastikan semua komponen Web3 + Mobile + Kulino Coin bekerja dengan baik
/// 
/// CHECKLIST:
/// 1. ✓ Wallet address terdeteksi dari website
/// 2. ✓ KulinoCoinManager initialized dengan address
/// 3. ✓ Balance Kulino Coin ter-fetch dari Solana
/// 4. ✓ Orientation Manager aktif (mobile landscape)
/// 5. ✓ In-game coin display ter-update
/// 
/// CARA PAKAI:
/// - Attach ke GameObject "GameManager" atau buat GameObject baru
/// - Check Console untuk hasil validasi
/// - Lihat Inspector untuk status real-time
/// </summary>
[DefaultExecutionOrder(-500)]
public class IntegrationChecker : MonoBehaviour
{
    [Header("📊 Integration Status (Read-Only)")]
    [SerializeField] private bool walletConnected = false;
    [SerializeField] private string walletAddress = "";
    [SerializeField] private bool kulinoCoinManagerReady = false;
    [SerializeField] private bool orientationManagerReady = false;
    [SerializeField] private double kulinoCoinBalance = 0;
    [SerializeField] private bool isLandscapeMode = false;
    [SerializeField] private bool isMobileDevice = false;

    [Header("🔧 Settings")]
    [Tooltip("Auto-check every N seconds")]
    public float checkInterval = 5f;
    
    [Tooltip("Show detailed logs")]
    public bool verboseLogging = true;

    private float nextCheckTime = 0f;
    private int checkCount = 0;

    void Start()
    {
        LogHeader("🚀 INTEGRATION CHECKER STARTED");
        LogInfo("Waiting for managers to initialize...");
        
        // First check after 2 seconds
        Invoke(nameof(PerformFullCheck), 2f);
    }

    void Update()
{
    if (Time.time >= nextCheckTime)
    {
        // ✅ FIX: Skip check if scene is loading
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded)
        {
            PerformFullCheck();
        }
        
        nextCheckTime = Time.time + checkInterval;
        checkCount++;
    }
}

    void PerformFullCheck()
    {
        LogHeader($"🔍 CHECK #{checkCount + 1} - {System.DateTime.Now:HH:mm:ss}");

        // 1. Check GameManager & Wallet
        CheckWalletConnection();

        // 2. Check KulinoCoinManager
        CheckKulinoCoinManager();

        // 3. Check OrientationManager (Mobile)
        CheckOrientationManager();

        // 4. Final Summary
        PrintSummary();
    }

    // Di IntegrationChecker.cs - REPLACE CheckWalletConnection() method

void CheckWalletConnection()
{
    LogSection("1️⃣ WALLET CONNECTION");

    // ✅ FIX: Better null checking
    if (GameManager.Instance == null || GameManager.Instance.gameObject == null)
    {
        LogError("❌ GameManager.Instance is NULL or destroyed!");
        LogInfo("💡 This usually happens during scene transitions");
        LogInfo("💡 GameManager should auto-recreate on next frame");
        walletConnected = false;
        walletAddress = "";
        return;
    }

    try
    {
        walletAddress = GameManager.Instance.GetWalletAddress();
        walletConnected = !string.IsNullOrEmpty(walletAddress);

        if (walletConnected)
        {
            LogSuccess($"✓ Wallet Connected: {ShortenAddress(walletAddress)}");
        }
        else
        {
            LogWarning("⚠️ Wallet NOT connected");
            LogInfo("💡 User needs to connect wallet on website first");
        }
    }
    catch (System.Exception ex)
    {
        LogError($"❌ Error checking wallet: {ex.Message}");
        walletConnected = false;
        walletAddress = "";
    }
}

    void CheckKulinoCoinManager()
{
    LogSection("2️⃣ KULINO COIN MANAGER");

    if (KulinoCoinManager.Instance != null)
    {
        kulinoCoinManagerReady = KulinoCoinManager.Instance.IsInitialized();
        kulinoCoinBalance = KulinoCoinManager.Instance.GetBalance();

        if (kulinoCoinManagerReady)
        {
            LogSuccess($"✓ KulinoCoinManager Initialized");
            LogSuccess($"✓ Balance: {kulinoCoinBalance:F6} KULINO");
            
            // ✅ NEW: Check if address matches
            string managerWallet = KulinoCoinManager.Instance.GetWalletAddress();
            if (!string.IsNullOrEmpty(managerWallet) && managerWallet == walletAddress)
            {
                LogSuccess($"✓ Wallet address matches: {ShortenAddress(managerWallet)}");
            }
            else if (!string.IsNullOrEmpty(managerWallet))
            {
                LogWarning($"⚠️ Address mismatch!");
                LogWarning($"   GameManager: {ShortenAddress(walletAddress)}");
                LogWarning($"   KulinoCoin:  {ShortenAddress(managerWallet)}");
            }
            else
            {
                LogWarning("⚠️ KulinoCoinManager has no wallet address!");
            }
        }
        else
        {
            LogWarning("⚠️ KulinoCoinManager NOT initialized yet");
            LogInfo($"💡 Balance: {kulinoCoinBalance:F6} (may be 0 if not initialized)");
            
            // ✅ NEW: Try to initialize if GameManager has address
            if (!string.IsNullOrEmpty(walletAddress))
            {
                LogInfo("💡 Attempting to initialize with GameManager's address...");
                KulinoCoinManager.Instance.Initialize(walletAddress);
            }
        }
    }
    else
    {
        LogError("❌ KulinoCoinManager.Instance is NULL!");
        LogInfo("💡 Make sure 'KulinoCoinManager' GameObject exists in scene");
    }
}

    void CheckOrientationManager()
    {
        LogSection("3️⃣ ORIENTATION MANAGER (MOBILE)");

        if (OrientationManager.Instance != null)
        {
            orientationManagerReady = true;
            isMobileDevice = OrientationManager.Instance.IsMobile();
            isLandscapeMode = OrientationManager.Instance.IsLandscapeMode();

            LogSuccess("✓ OrientationManager Active");
            LogInfo($"   Platform: {(isMobileDevice ? "MOBILE 📱" : "DESKTOP 🖥️")}");
            LogInfo($"   Mode: {(isLandscapeMode ? "LANDSCAPE ✓" : "PORTRAIT ⚠️")}");

            if (isMobileDevice && !isLandscapeMode)
            {
                LogWarning("⚠️ Mobile device in PORTRAIT mode");
                LogInfo("💡 Rotation prompt should be visible");
            }
        }
        else
        {
            LogWarning("⚠️ OrientationManager.Instance is NULL");
            LogInfo("💡 OrientationManager may not be needed for desktop");
        }
    }

    void PrintSummary()
    {
        LogSection("📋 SUMMARY");

        int passCount = 0;
        int totalChecks = 5;

        // Check 1: GameManager
        if (GameManager.Instance != null)
        {
            LogSuccess("✓ GameManager: OK");
            passCount++;
        }
        else
        {
            LogError("✗ GameManager: MISSING");
        }

        // Check 2: Wallet Connection
        if (walletConnected)
        {
            LogSuccess($"✓ Wallet: Connected ({ShortenAddress(walletAddress)})");
            passCount++;
        }
        else
        {
            LogWarning("✗ Wallet: Not Connected");
        }

        // Check 3: KulinoCoinManager
        if (kulinoCoinManagerReady)
        {
            LogSuccess($"✓ KulinoCoin: {kulinoCoinBalance:F2} KC");
            passCount++;
        }
        else
        {
            LogWarning("✗ KulinoCoin: Not Ready");
        }

        // Check 4: Orientation (optional for desktop)
        if (!isMobileDevice || (isMobileDevice && isLandscapeMode))
        {
            LogSuccess("✓ Orientation: OK");
            passCount++;
        }
        else
        {
            LogWarning("✗ Orientation: Portrait Mode");
        }

        // Check 5: Overall Integration
        bool allCriticalPass = (GameManager.Instance != null) && 
                               walletConnected && 
                               kulinoCoinManagerReady;

        if (allCriticalPass)
        {
            LogSuccess("✓ Integration: READY");
            passCount++;
        }
        else
        {
            LogWarning("✗ Integration: NOT READY");
        }

        LogHeader($"🎯 RESULT: {passCount}/{totalChecks} PASSED");

        if (passCount == totalChecks)
        {
            LogSuccess("✅✅✅ ALL SYSTEMS OPERATIONAL ✅✅✅");
        }
        else if (passCount >= 3)
        {
            LogWarning("⚠️ PARTIAL - Some components need attention");
        }
        else
        {
            LogError("❌ CRITICAL - Multiple components missing");
        }

        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }

    // ========================================
    // LOGGING HELPERS
    // ========================================

    void LogHeader(string msg)
    {
        Debug.Log($"\n<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n{msg}\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
    }

    void LogSection(string msg)
    {
        if (verboseLogging)
            Debug.Log($"\n<color=yellow>▶ {msg}</color>");
    }

    void LogSuccess(string msg)
    {
        if (verboseLogging)
            Debug.Log($"<color=lime>{msg}</color>");
    }

    void LogInfo(string msg)
    {
        if (verboseLogging)
            Debug.Log($"<color=white>{msg}</color>");
    }

    void LogWarning(string msg)
    {
        Debug.LogWarning($"<color=orange>{msg}</color>");
    }

    void LogError(string msg)
    {
        Debug.LogError($"<color=red>{msg}</color>");
    }

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10)
            return addr;
        return $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
    }

    // ========================================
    // CONTEXT MENU (MANUAL CHECKS)
    // ========================================

    [ContextMenu("🔍 Run Full Check Now")]
    void Context_RunFullCheck()
    {
        PerformFullCheck();
    }

    [ContextMenu("🔄 Force Refresh All")]
    void Context_ForceRefreshAll()
    {
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
        }

        Invoke(nameof(PerformFullCheck), 2f);
        Debug.Log("[IntegrationChecker] 🔄 Force refresh initiated");
    }

    [ContextMenu("📊 Print Detailed Status")]
    void Context_PrintDetailedStatus()
    {
        Debug.Log("=== DETAILED INTEGRATION STATUS ===");
        Debug.Log($"Wallet Connected: {walletConnected}");
        Debug.Log($"Wallet Address: {walletAddress}");
        Debug.Log($"KulinoCoin Ready: {kulinoCoinManagerReady}");
        Debug.Log($"Kulino Balance: {kulinoCoinBalance:F6}");
        Debug.Log($"Orientation Ready: {orientationManagerReady}");
        Debug.Log($"Is Mobile: {isMobileDevice}");
        Debug.Log($"Is Landscape: {isLandscapeMode}");
        Debug.Log($"Check Count: {checkCount}");
        Debug.Log("===================================");
    }
}