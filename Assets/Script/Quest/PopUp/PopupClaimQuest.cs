using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupClaimQuest : MonoBehaviour
{
    public static PopupClaimQuest Instance { get; private set; }

    [Header("UI refs (assign in Inspector)")]
    public GameObject rootPopup;            // root panel of the popup
    public Image iconImage;                 // reward icon
    public TMP_Text rewardAmountText;       // text showing reward amount / description
    public TMP_Text deskText;               // title / description
    public Button confirmButton;            // confirm button
    public GameObject blurOverlay;          // optional background blur

    // optional: close button can call Close()

    Action onConfirm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurOverlay != null) blurOverlay.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButton);
        }
    }

    /// <summary>
    /// Open popup with shown info and a callback to execute on Confirm.
    /// </summary>
    public void Open(Sprite iconSprite, string rewardText, string titleText, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurOverlay != null) blurOverlay.SetActive(true);

        if (iconImage != null) iconImage.sprite = iconSprite;
        if (rewardAmountText != null) rewardAmountText.text = rewardText ?? "";
        if (deskText != null) deskText.text = titleText ?? "";
    }

    void OnConfirmButton()
    {
        try { onConfirm?.Invoke(); }
        catch (Exception ex) { Debug.LogError("[PopupClaimQuest] onConfirm error: " + ex); }
        Close();
    }

    public void Close()
    {
        onConfirm = null;
        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurOverlay != null) blurOverlay.SetActive(false);
    }
}
