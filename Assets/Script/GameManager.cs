using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// ✅ FIXED v7.0: GameManager - Kulino Coin Detection Fixed
/// - Better URL parsing
/// - Proper wallet propagation
/// - Scene transition handling
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("🎮 Scene Settings")]
    [Tooltip("Scenes where GameManager should persist")]
    public string[] persistentScenes = new string[] { "Gameplay", "Game", "Level" };
    
    [Tooltip("Scenes where GameManager should NOT exist")]
    public string[] excludedScenes = new string[] { "MainMenu", "Menu", "Lobby" };

    [Header("🔐 Wallet")]
    private string walletAddress;
    private bool walletInitialized = false;

    [Header("💰 Claim Settings")]
    public string gameId = "unity-demo";
    public int claimAmount = 1;
    public int claimTimeoutSeconds = 60;

    [Header("🎨 UI References")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    private bool isRequestInProgress = false;
    private float requestStartTime = 0f;
    private bool isPersistent = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestClaim(string message);
#endif

    void Awake()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (IsExcludedScene(currentScene))
        {
            Debug.Log($"[GameManager] ❌ Scene '{currentScene}' excluded - destroying");
            Destroy(gameObject);
            return;
        }

        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning($"[GameManager] Duplicate detected - destroying");
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;

        if (ShouldPersist(currentScene))
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            isPersistent = true;
            gameObject.name = "[GameManager - PERSISTENT]";
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log($"[GameManager] ✅ Persistent mode in scene: {currentScene}");
        }
        else
        {
            gameObject.name = "[GameManager - LOCAL]";
            Debug.Log($"[GameManager] ✅ Local mode in scene: {currentScene}");
        }
    }

    void OnDestroy()
    {
        if (isPersistent)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");
        
        if (IsExcludedScene(scene.name))
        {
            Debug.Log($"[GameManager] ⚠️ Entered excluded scene '{scene.name}' - destroying");
            Destroy(gameObject);
            return;
        }

        if (!string.IsNullOrEmpty(walletAddress) && walletInitialized)
        {
            StartCoroutine(RenotifyWalletDelayed());
        }
    }

    IEnumerator RenotifyWalletDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[GameManager] 🔄 Re-notifying wallet: {ShortenAddress(walletAddress)}");
        
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.Initialize(walletAddress);
        }
    }

    bool ShouldPersist(string sceneName)
    {
        foreach (string scene in persistentScenes)
        {
            if (sceneName.Contains(scene) || sceneName.Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    bool IsExcludedScene(string sceneName)
    {
        foreach (string scene in excludedScenes)
        {
            if (sceneName.Contains(scene) || sceneName.Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    void Start()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClick);
        }

        SetStatus("Ready");
        
        // ✅ CRITICAL: Start URL parsing immediately
        StartCoroutine(ParseURLAndConnect());
    }

    void Update()
    {
        if (isRequestInProgress)
        {
            float elapsedTime = Time.time - requestStartTime;
            if (elapsedTime > (float)claimTimeoutSeconds)
            {
                Debug.LogWarning("[GameManager] ⏱️ Claim request timeout!");
                FinishRequest(false, "timeout");
            }
        }
    }

    /// <summary>
    /// ✅ FIXED: Better URL parsing dengan multiple attempts
    /// </summary>
    IEnumerator ParseURLAndConnect()
    {
        Debug.Log("[GameManager] 🔍 Starting URL parse...");
        
        // ✅ Wait for JavaScript to be ready
        yield return new WaitForSeconds(0.5f);

        string url = GetCurrentURL();
        Debug.Log($"[GameManager] 📍 Current URL: {url}");

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[GameManager] ⚠️ Could not get URL");
            
            // ✅ FALLBACK: Try PlayerPrefs
            string savedWallet = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(savedWallet))
            {
                Debug.Log($"[GameManager] 💾 Using saved wallet: {ShortenAddress(savedWallet)}");
                OnWalletConnected(savedWallet);
            }
            yield break;
        }

        // ✅ Parse wallet parameter
        string walletParam = GetURLParameter(url, "wallet");
        
        if (string.IsNullOrEmpty(walletParam))
        {
            Debug.LogWarning("[GameManager] ⚠️ No wallet parameter in URL");
            
            // ✅ FALLBACK: Try PlayerPrefs
            string savedWallet = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(savedWallet))
            {
                Debug.Log($"[GameManager] 💾 Using saved wallet: {ShortenAddress(savedWallet)}");
                OnWalletConnected(savedWallet);
            }
            yield break;
        }

        Debug.Log($"[GameManager] 🎯 Found wallet in URL: {ShortenAddress(walletParam)}");
        OnWalletConnected(walletParam);
        
        // ✅ Wait for KulinoCoinManager to be ready
        int maxRetries = 20;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            if (KulinoCoinManager.Instance != null)
            {
                Debug.Log($"[GameManager] ✅ KulinoCoinManager found!");
                
                if (!KulinoCoinManager.Instance.IsInitialized())
                {
                    Debug.Log($"[GameManager] 🔄 Initializing KulinoCoinManager with wallet...");
                    KulinoCoinManager.Instance.Initialize(walletParam);
                }
                else
                {
                    Debug.Log("[GameManager] ℹ️ KulinoCoinManager already initialized");
                }
                
                yield break;
            }
            
            retryCount++;
            Debug.Log($"[GameManager] ⏳ Waiting for KulinoCoinManager... ({retryCount}/{maxRetries})");
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogError("[GameManager] ❌ KulinoCoinManager not found after retries!");
    }

    /// <summary>
    /// ✅ IMPROVED: Better URL getter dengan error handling
    /// </summary>
    string GetCurrentURL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string url = Application.absoluteURL;
            Debug.Log($"[GameManager] 📍 WebGL URL: {url}");
            return url;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Failed to get URL: {e.Message}");
            return "";
        }
#else
        // ✅ Editor testing
        string testUrl = "http://localhost/game?wallet=44kmkWSoRYPgft7hsmVRx7GTHyaQ5CesBDyFMLbBkjsP&game=fly-to-the-moon";
        Debug.Log($"[GameManager] 🧪 Editor test URL: {testUrl}");
        return testUrl;
#endif
    }

    /// <summary>
    /// ✅ IMPROVED: Better URL parameter parsing
    /// </summary>
    string GetURLParameter(string url, string paramName)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[GameManager] URL is empty");
                return "";
            }
            
            // ✅ Handle both ? and # parameters
            int queryStart = url.IndexOf('?');
            if (queryStart < 0)
            {
                queryStart = url.IndexOf('#');
            }
            
            if (queryStart < 0)
            {
                Debug.LogWarning("[GameManager] No query string found");
                return "";
            }
            
            string query = url.Substring(queryStart + 1);
            Debug.Log($"[GameManager] Query string: {query}");
            
            string[] pairs = query.Split('&');
            
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == paramName)
                {
                    string value = Uri.UnescapeDataString(keyValue[1]);
                    Debug.Log($"[GameManager] ✅ Found {paramName} = {ShortenAddress(value)}");
                    return value;
                }
            }
            
            Debug.LogWarning($"[GameManager] Parameter '{paramName}' not found");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] URL parse error: {e.Message}");
        }
        
        return "";
    }

    /// <summary>
    /// ✅ IMPROVED: Wallet connection dengan validation
    /// </summary>
    public void OnWalletConnected(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[GameManager] ❌ Wallet address is empty!");
            return;
        }

        // ✅ Validate Solana address format
        if (address.Length < 32 || address.Length > 44)
        {
            Debug.LogError($"[GameManager] ❌ Invalid wallet address format: {address}");
            return;
        }

        walletAddress = address;
        walletInitialized = true;
        
        Debug.Log($"[GameManager] 👛 Wallet connected: {ShortenAddress(address)}");

        // ✅ Save to PlayerPrefs
        PlayerPrefs.SetString("WalletAddress", address);
        PlayerPrefs.SetString("WalletConnectedTime", DateTime.Now.ToString());
        PlayerPrefs.Save();
        
        Debug.Log("[GameManager] 💾 Wallet saved to PlayerPrefs");

        // ✅ Initialize KulinoCoinManager
        InitializeKulinoCoinManager(address);
    }

    void InitializeKulinoCoinManager(string address)
    {
        Debug.Log("[GameManager] 🔄 Initializing KulinoCoinManager...");
        
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.Initialize(address);
            Debug.Log("[GameManager] ✓ KulinoCoinManager initialized");
            
            // ✅ Fetch balance after short delay
            StartCoroutine(FetchBalanceDelayed(2f));
        }
        else
        {
            Debug.LogError("[GameManager] ❌ KulinoCoinManager.Instance NOT FOUND!");
            
            // ✅ Retry after delay
            StartCoroutine(RetryInitializeKulinoCoin(address));
        }
    }

    IEnumerator RetryInitializeKulinoCoin(string address)
    {
        int maxRetries = 10;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            yield return new WaitForSeconds(1f);
            retryCount++;
            
            if (KulinoCoinManager.Instance != null)
            {
                Debug.Log($"[GameManager] ✅ KulinoCoinManager found on retry {retryCount}");
                KulinoCoinManager.Instance.Initialize(address);
                StartCoroutine(FetchBalanceDelayed(1f));
                yield break;
            }
            
            Debug.Log($"[GameManager] ⏳ Retry {retryCount}/{maxRetries} - KulinoCoinManager not found");
        }
        
        Debug.LogError("[GameManager] ❌ KulinoCoinManager not found after all retries!");
    }

    IEnumerator FetchBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (KulinoCoinManager.Instance != null && KulinoCoinManager.Instance.IsInitialized())
        {
            Debug.Log("[GameManager] 🔄 Triggering balance fetch...");
            KulinoCoinManager.Instance.FetchKulinoCoinBalance();
        }
    }

    public string GetWalletAddress()
    {
        return walletAddress;
    }

    public bool IsWalletInitialized()
    {
        return walletInitialized;
    }

    public void OnClaimButtonClick()
    {
        if (isRequestInProgress)
        {
            Debug.LogWarning("[GameManager] ⚠️ Request in progress!");
            return;
        }

        Debug.Log("[GameManager] 💰 Claim button clicked!");

        isRequestInProgress = true;
        requestStartTime = Time.time;

        if (claimButton != null)
            claimButton.interactable = false;

        SetStatus("Waiting for signature...");

        var payload = new ClaimPayload()
        {
            address = "",
            gameId = gameId,
            amount = claimAmount,
            nonce = Guid.NewGuid().ToString(),
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(payload);

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            Debug.Log($"[GameManager] 📤 Calling RequestClaim");
            RequestClaim(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ RequestClaim failed: {e}");
            FinishRequest(false, "js_call_failed");
        }
#else
        Debug.Log($"[GameManager] [EDITOR] Would call JS");
        Invoke(nameof(EditorSimulateResult), 1f);
#endif
    }

    public void OnClaimResult(string json)
    {
        Debug.Log($"[GameManager] 📥 OnClaimResult: {json}");

        try
        {
            var res = JsonUtility.FromJson<ClaimResult>(json);

            if (res != null && res.success)
            {
                Debug.Log($"[GameManager] ✅ SUCCESS! TX: {res.txHash}");
                FinishRequest(true, res.txHash ?? "ok");
            }
            else
            {
                string errorMsg = res != null ? (res.error ?? "unknown") : "parse_error";
                Debug.LogError($"[GameManager] ❌ FAILED: {errorMsg}");
                FinishRequest(false, errorMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Parse error: {e}");
            FinishRequest(false, "parse_exception");
        }
    }

    void FinishRequest(bool success, string info)
    {
        isRequestInProgress = false;

        if (claimButton != null)
            claimButton.interactable = true;

        string statusMsg = success ? $"✅ Success: {info}" : $"❌ Failed: {info}";
        SetStatus(statusMsg);

        if (success)
        {
            StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
        }
    }

    void SetStatus(string txt)
    {
        if (statusText != null)
            statusText.text = txt;
    }

    public void OnPhantomPaymentResult(string resultJson)
    {
        Debug.Log($"[GameManager] 💳 Payment result: {resultJson}");

        try
        {
            var result = JsonUtility.FromJson<ClaimResult>(resultJson);

            if (result.success)
            {
                Debug.Log($"[GameManager] ✅ PAYMENT SUCCESS!");
                
                var shopManager = FindFirstObjectByType<ShopManager>();
                if (shopManager != null)
                {
                    shopManager.OnPaymentConfirmed();
                }

                StartCoroutine(RefreshKulinoCoinBalanceDelayed(2f));
            }
            else
            {
                Debug.LogError($"[GameManager] ❌ PAYMENT FAILED: {result.error}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] ❌ Parse error: {ex.Message}");
        }
    }

    IEnumerator RefreshKulinoCoinBalanceDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (KulinoCoinManager.Instance != null)
        {
            Debug.Log("[GameManager] 🔄 Refreshing KC balance...");
            KulinoCoinManager.Instance.RefreshBalance();
        }
    }

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10)
            return addr;
        return $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
    }

    void EditorSimulateResult()
    {
        var fake = new ClaimResult()
        {
            success = true,
            txHash = "EDITOR_FAKE_TX"
        };

        OnClaimResult(JsonUtility.ToJson(fake));
    }

    [Serializable]
    class ClaimPayload
    {
        public string address;
        public string gameId;
        public int amount;
        public string nonce;
        public long ts;
    }

    [Serializable]
    class ClaimResult
    {
        public bool success;
        public string error;
        public string txHash;
    }

    [ContextMenu("📊 Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== GAMEMANAGER STATUS ===");
        Debug.Log($"Instance: {(Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Persistent: {isPersistent}");
        Debug.Log($"Wallet: {ShortenAddress(walletAddress)}");
        Debug.Log($"Wallet Initialized: {walletInitialized}");
        Debug.Log($"URL: {GetCurrentURL()}");
        Debug.Log("==========================");
    }

    [ContextMenu("🧪 Test: Force URL Parse")]
    void Context_TestURLParse()
    {
        StartCoroutine(ParseURLAndConnect());
    }
}