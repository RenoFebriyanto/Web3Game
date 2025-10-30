using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UPDATED: Support bundle display dengan multiple BorderIcon clones
/// STRUCTURE HIERARCHY:
///   PopupClaimQuest (root)
///     ├─ CollectText (TMP_Text) - title "Memperoleh Hadiah"
///     ├─ BlurEffect (Image) - background blur
///     ├─ ContainerItems (Transform + HorizontalLayoutGroup) - ASSIGN INI!
///     │   └─ BorderIcon (template - akan di-clone untuk bundle)
///     │       ├─ Icons (Image) - icon item
///     │       └─ Amount (TMP_Text) - jumlah item
///     ├─ DeskItem (TMP_Text) - description
///     └─ ConfirmBTN (Button) - confirm button
/// </summary>
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
    // SINGLE ITEM DISPLAY (Quest reward, single shop item)
    // ========================================

    /// <summary>
    /// Open popup dengan single item display
    /// </summary>
    public void Open(Sprite iconSprite, string amountText, string titleText, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        // Show single item display
        ShowSingleItem(iconSprite, amountText, titleText);
    }

    void ShowSingleItem(Sprite icon, string amount, string title)
    {
        Debug.Log($"[PopupClaimQuest] ShowSingleItem: icon={icon?.name}, amount={amount}, title={title}");

        // Clear spawned icons
        ClearSpawnedIcons();

        // Update text displays
        if (collectText != null) collectText.text = "Memperoleh Hadiah"; // Fixed title
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
    // BUNDLE ITEMS DISPLAY (Shop bundle)
    // ========================================

    /// <summary>
    /// Open popup dengan bundle items display (multiple items)
    /// </summary>
    public void OpenBundle(List<BundleItemData> items, string title, string description, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        // Show bundle display
        ShowBundleItems(items, title, description);
    }

    void ShowBundleItems(List<BundleItemData> items, string title, string description)
    {
        Debug.Log($"[PopupClaimQuest] ShowBundleItems: {items?.Count ?? 0} items, title={title}");

        // Clear previous spawned icons
        ClearSpawnedIcons();

        // Update text displays
        if (collectText != null) collectText.text = "Memperoleh Hadiah"; // Fixed title
        if (deskItem != null) deskItem.text = title ?? "";

        // Validate
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

        // Spawn bundle items - clone BorderIcon untuk setiap item
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

            // Clone BorderIcon template
            GameObject clonedIcon = Instantiate(borderIconTemplate, containerItems);
            clonedIcon.SetActive(true);

            // Update content
            UpdateBorderIconContent(clonedIcon, item.icon, item.amount.ToString());

            spawnedBorderIcons.Add(clonedIcon);
            spawnedCount++;

            Debug.Log($"[PopupClaimQuest] Spawned item {spawnedCount}: {item.displayName} x{item.amount}");
        }

        Debug.Log($"[PopupClaimQuest] Total spawned: {spawnedCount} items. HorizontalLayoutGroup akan auto-arrange.");
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    /// <summary>
    /// Update content BorderIcon (icon + amount text)
    /// Structure: BorderIcon -> Icons (Image) -> Amount (TMP_Text)
    /// </summary>
    void UpdateBorderIconContent(GameObject borderIcon, Sprite icon, string amountText)
    {
        if (borderIcon == null) return;

        // Find Icons child (Image untuk icon item)
        Transform iconsTransform = borderIcon.transform.Find("Icons");
        if (iconsTransform != null)
        {
            Image iconsImage = iconsTransform.GetComponent<Image>();
            if (iconsImage != null && icon != null)
            {
                iconsImage.sprite = icon;
                iconsImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[PopupClaimQuest] Icons Image not found or icon is null");
            }
        }
        else
        {
            Debug.LogWarning($"[PopupClaimQuest] Icons child not found in {borderIcon.name}");
        }

        // Find Amount child (TMP_Text)
        Transform amountTransform = borderIcon.transform.Find("Amount");
        if (amountTransform != null)
        {
            TMP_Text amountTMP = amountTransform.GetComponent<TMP_Text>();
            if (amountTMP != null)
            {
                if (!string.IsNullOrEmpty(amountText))
                {
                    amountTMP.text = amountText;
                    amountTMP.gameObject.SetActive(true);
                }
                else
                {
                    amountTMP.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[PopupClaimQuest] Amount TMP_Text not found");
            }
        }
        else
        {
            Debug.LogWarning($"[PopupClaimQuest] Amount child not found in {borderIcon.name}");
        }
    }

    /// <summary>
    /// Clear all spawned BorderIcon clones (keep template)
    /// </summary>
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
        // Play success sound
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

        // Clear spawned icons
        ClearSpawnedIcons();

        // Restore template visibility
        if (borderIconTemplate != null)
        {
            borderIconTemplate.SetActive(true);
        }

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurEffect != null) blurEffect.SetActive(false);
    }
}

/// <summary>
/// Data class untuk bundle item display di popup
/// </summary>
[System.Serializable]
public class BundleItemData
{
    public Sprite icon;
    public int amount;
    public string displayName; // Optional

    public BundleItemData(Sprite icon, int amount, string displayName = "")
    {
        this.icon = icon;
        this.amount = amount;
        this.displayName = displayName;
    }
}