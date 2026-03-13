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
/// ✅ FIXED v9.0: GameManager - Semua referensi KulinoCoinManager DIHAPUS
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("🎮 Scene Settings")]
    public string[] persistentScenes = new string[] { "Gameplay", "Game", "Level" };
    public string[] excludedScenes   = new string[] { "MainMenu", "Menu", "Lobby" };

    [Header("🔐 Wallet")]
    private string walletAddress;
    private bool walletInitialized = false;

    [Header("💰 Claim Settings")]
    public string gameId             = "unity-demo";
    public int claimAmount           = 1;
    public int claimTimeoutSeconds   = 60;

    [Header("🎨 UI References")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    private bool isRequestInProgress = false;
    private float requestStartTime   = 0f;
    private bool isPersistent        = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void RequestClaim(string message);
    [DllImport("__Internal")] private static extern string GetCurrentURL();
#endif

    // ─── Lifecycle ────────────────────────────────────────────────────────
    void Awake()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate detected - destroying");
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;

        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        isPersistent = true;
        gameObject.name = "[GameManager - PERSISTENT]";

        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log($"[GameManager] ✅ Persistent mode in scene: {currentScene}");
    }

    void OnDestroy()
    {
        if (isPersistent) SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");

        if (IsExcludedScene(scene.name))
        {
            Debug.Log($"[GameManager] ⚠️ Entered excluded scene '{scene.name}' - self-destroying");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimButtonClick);

        SetStatus("Ready");
        StartCoroutine(ParseURLAndConnect());
    }

    void Update()
    {
        if (!isRequestInProgress) return;

        float elapsed = Time.time - requestStartTime;
        if (elapsed > claimTimeoutSeconds)
        {
            Debug.LogWarning("[GameManager] ⏱️ Claim request timeout!");
            FinishRequest(false, "timeout");
        }
    }

    // ─── Wallet ───────────────────────────────────────────────────────────
    IEnumerator ParseURLAndConnect()
    {
        Debug.Log("[GameManager] 🔍 Starting URL parse...");
        yield return new WaitForSeconds(0.5f);

        string url = GetURL();
        Debug.Log($"[GameManager] 📍 URL: {url}");

        if (string.IsNullOrEmpty(url))
        {
            string saved = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(saved)) OnWalletConnected(saved);
            yield break;
        }

        string walletParam = GetURLParameter(url, "wallet");

        if (string.IsNullOrEmpty(walletParam))
        {
            string saved = PlayerPrefs.GetString("WalletAddress", "");
            if (!string.IsNullOrEmpty(saved)) OnWalletConnected(saved);
            yield break;
        }

        Debug.Log($"[GameManager] 🎯 Wallet in URL: {ShortenAddress(walletParam)}");
        OnWalletConnected(walletParam);
    }

    string GetURL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return GetCurrentURL(); }
        catch { return ""; }
#else
        return "";
#endif
    }

    string GetURLParameter(string url, string key)
    {
        try
        {
            var uri   = new Uri(url);
            string q  = uri.Query.TrimStart('?');
            foreach (string part in q.Split('&'))
            {
                string[] kv = part.Split('=');
                if (kv.Length == 2 && kv[0] == key)
                    return Uri.UnescapeDataString(kv[1]);
            }
        }
        catch { }
        return "";
    }

    public void OnWalletConnected(string address)
    {
        if (string.IsNullOrEmpty(address)) { Debug.LogError("[GameManager] ❌ Empty address!"); return; }
        if (address.Length < 32 || address.Length > 44) { Debug.LogError($"[GameManager] ❌ Invalid address: {address}"); return; }

        walletAddress     = address;
        walletInitialized = true;

        PlayerPrefs.SetString("WalletAddress", address);
        PlayerPrefs.SetString("WalletConnectedTime", DateTime.Now.ToString());
        PlayerPrefs.Save();

        Debug.Log($"[GameManager] 👛 Wallet connected: {ShortenAddress(address)}");
    }

    public string GetWalletAddress()  => walletAddress;
    public bool IsWalletInitialized() => walletInitialized;

    // ─── Claim ────────────────────────────────────────────────────────────
    public void OnClaimButtonClick()
    {
        if (isRequestInProgress) { Debug.LogWarning("[GameManager] ⚠️ Request in progress!"); return; }

        Debug.Log("[GameManager] 💰 Claim button clicked!");
        isRequestInProgress = true;
        requestStartTime    = Time.time;

        if (claimButton != null) claimButton.interactable = false;
        SetStatus("Waiting for signature...");

        var payload = new ClaimPayload
        {
            address = walletAddress ?? "",
            gameId  = gameId,
            amount  = claimAmount,
            nonce   = Guid.NewGuid().ToString(),
            ts      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(payload);

#if UNITY_WEBGL && !UNITY_EDITOR
        try   { RequestClaim(json); }
        catch (Exception e) { Debug.LogError($"[GameManager] ❌ RequestClaim failed: {e}"); FinishRequest(false, "js_call_failed"); }
#else
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
                string err = res != null ? (res.error ?? "unknown") : "parse_error";
                Debug.LogError($"[GameManager] ❌ FAILED: {err}");
                FinishRequest(false, err);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Parse error: {e}");
            FinishRequest(false, "parse_exception");
        }
    }

    public void OnPhantomPaymentResult(string resultJson)
    {
        Debug.Log($"[GameManager] 💳 Payment result: {resultJson}");
        try
        {
            var result = JsonUtility.FromJson<ClaimResult>(resultJson);
            if (result.success)
            {
                Debug.Log($"[GameManager] ✅ PAYMENT SUCCESS! TX: {result.txHash}");
                var shopManager = FindFirstObjectByType<ShopManager>();
                shopManager?.OnPaymentConfirmed();
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

    void FinishRequest(bool success, string info)
    {
        isRequestInProgress = false;
        if (claimButton != null) claimButton.interactable = true;
        SetStatus(success ? $"✅ Success: {info}" : $"❌ Failed: {info}");
    }

    void SetStatus(string txt) { if (statusText != null) statusText.text = txt; }

    // ─── Scene Helpers ────────────────────────────────────────────────────
    bool IsExcludedScene(string sceneName)
    {
        foreach (string s in excludedScenes)
            if (sceneName.Contains(s) || sceneName.Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    string ShortenAddress(string addr)
    {
        if (string.IsNullOrEmpty(addr) || addr.Length < 10) return addr;
        return $"{addr.Substring(0, 6)}...{addr.Substring(addr.Length - 4)}";
    }

    void EditorSimulateResult()
    {
        OnClaimResult(JsonUtility.ToJson(new ClaimResult { success = true, txHash = "EDITOR_FAKE_TX" }));
    }

    // ─── Serializable ─────────────────────────────────────────────────────
    [Serializable] class ClaimPayload { public string address, gameId, nonce; public int amount; public long ts; }
    [Serializable] class ClaimResult  { public bool success; public string error, txHash; }

    [ContextMenu("📊 Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log($"=== GAMEMANAGER ===\nWallet: {ShortenAddress(walletAddress)}\nInitialized: {walletInitialized}\nScene: {SceneManager.GetActiveScene().name}\n==================");
    }
}