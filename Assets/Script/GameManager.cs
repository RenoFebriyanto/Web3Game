using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public Button claimButton;
    public TextMeshProUGUI statusText;

    [Header("Claim settings")]
    public string gameId = "unity-demo";
    public int claimAmount = 1;
    public int claimTimeoutSeconds = 60;

    bool isRequestInProgress = false;
    float requestStartTime = 0f;

    // JS binding (calls global JS function RequestClaim(message))
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestClaim(string message);
    #endif

    void Start()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimButtonClick);

        SetStatus("Ready");
    }

    void Update()
    {
        // simple timeout guard: if pending too long, reset UI
        if (isRequestInProgress)
        {
            if (Time.time - requestStartTime > claimTimeoutSeconds)
            {
                Debug.LogWarning("Claim request timeout, resetting UI.");
                FinishRequest(false, "timeout");
            }
        }
    }

    public void OnClaimButtonClick()
    {
        if (isRequestInProgress) return; // extra safety

        // disable UI
        isRequestInProgress = true;
        requestStartTime = Time.time;
        if (claimButton != null) claimButton.interactable = false;
        SetStatus("Waiting for wallet signature...");

        // prepare payload
        var payload = new ClaimPayload()
        {
            address = "", // will be filled by JS (Phantom)
            gameId = gameId,
            amount = claimAmount,
            nonce = Guid.NewGuid().ToString(),
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string json = JsonUtility.ToJson(payload);

        // call JS
        #if UNITY_WEBGL && !UNITY_EDITOR
        try {
            RequestClaim(json);
        } catch (Exception e) {
            Debug.LogError("RequestClaim JS call failed: " + e);
            FinishRequest(false, "js_call_failed");
        }
        #else
        // Editor fallback: simulate a response after 1s for testing
        Debug.Log("[Editor] Would call JS RequestClaim: " + json);
        Invoke(nameof(EditorSimulateResult), 1f);
        #endif
    }

    // Called from JS when server returns result:
    // window.unityInstance.SendMessage('GameManager','OnClaimResult', JSON.stringify(result));
    public void OnClaimResult(string json)
    {
        Debug.Log("[OnClaimResult] " + json);
        // parse minimal result
        try {
            var res = JsonUtility.FromJson<ClaimResult>(json);
            if (res != null && res.success)
            {
                FinishRequest(true, res.txHash ?? "ok");
            }
            else
            {
                FinishRequest(false, res != null ? (res.error ?? "unknown") : "parse_error");
            }
        } catch(Exception e) {
            Debug.LogError("Failed to parse OnClaimResult: " + e);
            FinishRequest(false, "parse_exception");
        }
    }

    void FinishRequest(bool ok, string info)
    {
        isRequestInProgress = false;
        if (claimButton != null) claimButton.interactable = true;
        SetStatus(ok ? ("Claim success: " + info) : ("Claim failed: " + info));
        // optionally you can handle success (update coin counter UI, play sound, etc.)
    }

    void SetStatus(string txt)
    {
        if (statusText != null) statusText.text = txt;
        Debug.Log("[GameManager] " + txt);
    }

    // Editor test helper:
    void EditorSimulateResult()
    {
        var fake = new ClaimResult(){ success = true, txHash = "EDITOR_FAKE_TX" };
        OnClaimResult(JsonUtility.ToJson(fake));
    }

    [Serializable]
    class ClaimPayload {
        public string address;
        public string gameId;
        public int amount;
        public string nonce;
        public long ts;
    }

    [Serializable]
    class ClaimResult {
        public bool success;
        public string error;
        public string txHash;
    }
}
