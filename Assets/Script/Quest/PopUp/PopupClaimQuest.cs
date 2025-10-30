using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupClaimQuest : MonoBehaviour
{
    public static PopupClaimQuest Instance { get; private set; }

    [Header("UI Components")]
    public GameObject rootPopup;             // Root PopupClaimQuest
    public GameObject blurEffect;            // BlurEffect (Image background)

    [Header("ContainerItems (ASSIGN INI!)")]
    [Tooltip("ContainerItems dengan HorizontalLayoutGroup - tempat spawn BorderIcon")]
    public Transform containerItems;         // ContainerItems dari hierarchy

    [Header("Text Displays")]
    public TMP_Text collectText;             // CollectText - title "Memperoleh Hadiah"
    public TMP_Text deskItem;                // DeskItem - description

    [Header("Buttons")]
    public Button confirmButton;             // ConfirmBTN

    [Header("Icon sizing options")]
    [Tooltip("Jika true -> panggil SetNativeSize() saat mengganti sprite. Default: false (tidak mengubah ukuran).")]
    public bool useNativeSize = false;

    // Runtime
    Action onConfirm;
    List<GameObject> spawnedBorderIcons = new List<GameObject>();
    GameObject borderIconTemplate;           // Template BorderIcon dari hierarchy

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Hide popup di start
        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurEffect != null) blurEffect.SetActive(false);

        // Setup confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButton);
        }

        // Setup blur effect click to close (optional)
        if (blurEffect != null)
        {
            var btn = blurEffect.GetComponent<Button>();
            if (btn == null)
            {
                btn = blurEffect.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(Close);
        }

        // Get BorderIcon template dari ContainerItems
        if (containerItems != null && containerItems.childCount > 0)
        {
            borderIconTemplate = containerItems.GetChild(0).gameObject;
            Debug.Log($"[PopupClaimQuest] Found BorderIcon template: {borderIconTemplate.name}");
        }
        else
        {
            Debug.LogError("[PopupClaimQuest] ContainerItems not assigned or has no children! Need BorderIcon template.");
        }

        Debug.Log($"[PopupClaimQuest] Initialized. ContainerItems assigned: {(containerItems != null)}");
    }

    // ========================================
    // SINGLE ITEM DISPLAY
    // ========================================

    public void Open(Sprite iconSprite, string amountText, string titleText, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        ShowSingleItem(iconSprite, amountText, titleText);
    }

    void ShowSingleItem(Sprite icon, string amount, string title)
    {
        Debug.Log($"[PopupClaimQuest] ShowSingleItem: icon={icon?.name}, amount={amount}, title={title}");

        // Clear spawned icons
        ClearSpawnedIcons();

        // Update text displays
        if (collectText != null) collectText.text = "Memperoleh Hadiah";
        if (deskItem != null) deskItem.text = title ?? "";

        // Show ONLY 1 BorderIcon (template) dengan data
        if (borderIconTemplate != null)
        {
            borderIconTemplate.SetActive(true);
            UpdateBorderIconContent(borderIconTemplate, icon, amount);
            Debug.Log($"[PopupClaimQuest] Displayed single item using template");
        }
        else
        {
            Debug.LogError("[PopupClaimQuest] borderIconTemplate is null!");
        }
    }

    // ========================================
    // BUNDLE ITEMS DISPLAY
    // ========================================

    public void OpenBundle(List<BundleItemData> items, string title, string description, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        ShowBundleItems(items, title, description);
    }

    void ShowBundleItems(List<BundleItemData> items, string title, string description)
    {
        Debug.Log($"[PopupClaimQuest] ShowBundleItems: {items?.Count ?? 0} items, title={title}");

        ClearSpawnedIcons();

        if (collectText != null) collectText.text = "Memperoleh Hadiah";
        if (deskItem != null) deskItem.text = title ?? "";

        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[PopupClaimQuest] No items in bundle!");
            return;
        }

        if (containerItems == null)
        {
            Debug.LogError("[PopupClaimQuest] ContainerItems not assigned!");
            return;
        }

        if (borderIconTemplate == null)
        {
            Debug.LogError("[PopupClaimQuest] borderIconTemplate not found!");
            return;
        }

        // Hide template (akan di-clone)
        borderIconTemplate.SetActive(false);

        int spawnedCount = 0;
        foreach (var item in items)
        {
            if (item == null)
            {
                Debug.LogWarning("[PopupClaimQuest] Bundle item is null, skipping");
                continue;
            }

            if (item.icon == null)
            {
                Debug.LogWarning($"[PopupClaimQuest] Item '{item.displayName}' has null icon, skipping");
                continue;
            }

            GameObject clonedIcon = Instantiate(borderIconTemplate, containerItems);
            clonedIcon.SetActive(true);

            UpdateBorderIconContent(clonedIcon, item.icon, item.amount.ToString());

            spawnedBorderIcons.Add(clonedIcon);
            spawnedCount++;

            Debug.Log($"[PopupClaimQuest] Spawned item {spawnedCount}: {item.displayName} x{item.amount}");
        }

        Debug.Log($"[PopupClaimQuest] Total spawned: {spawnedCount} items.");
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    void UpdateBorderIconContent(GameObject borderIcon, Sprite icon, string amountText)
    {
        if (borderIcon == null)
        {
            Debug.LogError("[PopupClaimQuest] borderIcon is null!");
            return;
        }

        // === UPDATE ICON ===
        Image iconImage = null;

        string[] candidateNames = new string[] { "Image", "Icons", "Icon", "IconImage", "ItemIcon", "Icon_Item" };
        foreach (var name in candidateNames)
        {
            var t = borderIcon.transform.Find(name);
            if (t != null)
            {
                iconImage = t.GetComponent<Image>();
                if (iconImage != null) break;
            }
        }

        if (iconImage == null)
        {
            var images = borderIcon.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img == null) continue;
                if (img.gameObject == borderIcon) continue; // skip root border image
                iconImage = img;
                break;
            }
        }

        if (iconImage == null)
        {
            var rootImg = borderIcon.GetComponent<Image>();
            if (rootImg != null)
            {
                Debug.LogWarning($"[PopupClaimQuest] No child Image found for {borderIcon.name}, falling back to root Image (this may replace border).");
                iconImage = rootImg;
            }
        }

        if (iconImage != null)
        {
            // Preserve rect size if requested (default behavior: preserve)
            RectTransform rt = iconImage.GetComponent<RectTransform>();
            Vector2 prevSize = rt != null ? rt.sizeDelta : Vector2.zero;

            // Assign sprite
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                // DO NOT call SetNativeSize() by default (keeps inspector/template size)
                if (useNativeSize)
                {
                    iconImage.SetNativeSize();
                }
                else
                {
                    // reapply previous size to be safe
                    if (rt != null) rt.sizeDelta = prevSize;
                }
                Debug.Log($"[PopupClaimQuest] ✓ Set icon '{icon.name}' on {iconImage.gameObject.name} (preserveSize={(!useNativeSize)})");
            }
            else
            {
                Debug.LogWarning($"[PopupClaimQuest] icon is NULL for {borderIcon.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[PopupClaimQuest] ✗ Failed to find Image component in {borderIcon.name} hierarchy.");
        }

        // === UPDATE AMOUNT TEXT ===
        TMP_Text amountTMP = null;

        Transform amountTransform = borderIcon.transform.Find("Amount");
        if (amountTransform != null)
        {
            amountTMP = amountTransform.GetComponent<TMP_Text>();
        }

        if (amountTMP == null)
        {
            amountTMP = borderIcon.GetComponentInChildren<TMP_Text>(true);
            if (amountTMP != null && amountTMP.gameObject.name != "Amount")
            {
                var at = borderIcon.transform.Find("Amount");
                if (at != null) amountTMP = at.GetComponent<TMP_Text>();
            }
        }

        if (amountTMP != null)
        {
            if (!string.IsNullOrEmpty(amountText))
            {
                amountTMP.text = amountText;
                amountTMP.gameObject.SetActive(true);
                Debug.Log($"[PopupClaimQuest] ✓ Set amount text '{amountText}' on {amountTMP.gameObject.name}");
            }
            else
            {
                amountTMP.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"[PopupClaimQuest] ✗ Amount TMP_Text not found in {borderIcon.name}");
        }
    }

    void ClearSpawnedIcons()
    {
        foreach (var go in spawnedBorderIcons)
        {
            if (go != null) Destroy(go);
        }
        spawnedBorderIcons.Clear();

        Debug.Log("[PopupClaimQuest] Cleared spawned icons");
    }

    // ========================================
    // BUTTON HANDLERS
    // ========================================

    void OnConfirmButton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }

        try { onConfirm?.Invoke(); }
        catch (Exception ex) { Debug.LogError("[PopupClaimQuest] onConfirm error: " + ex); }

        Close();
    }

    public void Close()
    {
        onConfirm = null;

        ClearSpawnedIcons();

        if (borderIconTemplate != null)
        {
            borderIconTemplate.SetActive(true);
        }

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurEffect != null) blurEffect.SetActive(false);
    }
}

[System.Serializable]
public class BundleItemData
{
    public Sprite icon;
    public int amount;
    public string displayName;

    public BundleItemData(Sprite icon, int amount, string displayName = "")
    {
        this.icon = icon;
        this.amount = amount;
        this.displayName = displayName;
    }
}
