using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI Elements")]
    public Button connectButton;   // Drag & drop tombol Connect Wallet di Inspector
    public Button claimButton;     // Drag & drop tombol Claim Reward di Inspector

    private string playerPubKey = "";

    void Start()
    {
        // Awal game: sembunyikan tombol Claim dan pastikan Connect aktif
        claimButton.gameObject.SetActive(false);
        connectButton.gameObject.SetActive(true);

        // Pasang listener untuk tombol
        connectButton.onClick.AddListener(OnClickConnect);
        claimButton.onClick.AddListener(OnClickClaim);
    }

    // 1) Dipanggil saat klik Connect Wallet
    public void OnClickConnect()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalCall("unityConnect");
        #else
            Debug.Log("Wallet connect hanya tersedia di WebGL build.");
        #endif
    }

    // 2) Callback dari JS setelah Phantom berhasil connect
    //    Di JS wrapper: SendMessage("GameController", "OnWalletConnected", publicKey);
    public void OnWalletConnected(string publicKey)
    {
        Debug.Log("Wallet terhubung: " + publicKey);
        playerPubKey = publicKey;

        // Setelah connect, sembunyikan tombol Connect
        connectButton.gameObject.SetActive(false);
    }

    // 3) Dipanggil dari logic game Anda ketika player menang
    public void OnPlayerWins()
    {
        // Tampilkan tombol Claim Reward
        claimButton.gameObject.SetActive(true);
    }

    // 4) Dipanggil saat klik Claim Reward
    public void OnClickClaim()
    {
        if (string.IsNullOrEmpty(playerPubKey))
        {
            Debug.LogWarning("Wallet belum terhubung!");
            return;
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
            // Panggil fungsi JS unityReward(publicKey)
            Application.ExternalCall("unityReward", playerPubKey);
        #else
            Debug.Log("Claim reward hanya di WebGL build.");
        #endif
    }

    // 5) Callback dari JS setelah transaksi berhasil
    //    Di JS wrapper: SendMessage("GameController", "OnRewardSent", txSignature);
    public void OnRewardSent(string txSignature)
    {
        Debug.Log("Reward terkirim! Tx Sig: " + txSignature);

        // Nonaktifkan tombol setelah claim
        claimButton.interactable = false;
        Text btnText = claimButton.GetComponentInChildren<Text>();
        if (btnText != null)
            btnText.text = "Claimed";
    }
}
