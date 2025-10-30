using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupClaimQuest : MonoBehaviour
{
    public static PopupClaimQuest Instance { get; private set; }

    [Header("UI Components")]
    public GameObject rootPopup;
    public GameObject blurEffect;

    [Header("ContainerItems (ASSIGN INI!)")]
    [Tooltip("ContainerItems dengan HorizontalLayoutGroup - tempat spawn BorderIcon")]
    public Transform containerItems;

    [Header("BorderIcon Template (OPTIONAL - auto-find if null)")]
    [Tooltip("Template BorderIcon - jika null akan auto-find dari child pertama ContainerItems")]
    public GameObject borderIconTemplate;

    [Header("Text Displays")]
    public TMP_Text collectText;
    public TMP_Text deskItem;

    [Header("Buttons")]
    public Button confirmButton;

    [Header("Icon sizing options")]
    [Tooltip("Jika true -> panggil SetNativeSize() saat mengganti sprite. Default: false (tidak mengubah ukuran).")]
    public bool useNativeSize = false;

    // Runtime
    Action onConfirm;
    List<GameObject> spawnedBorderIcons = new List<GameObject>();

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

        // Setup blur effect click to close
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

        // PERBAIKAN: Auto-find BorderIcon template jika tidak di-assign
        if (borderIconTemplate == null && containerItems != null)
        {
            // Coba cari child pertama
            if (containerItems.childCount > 0)
            {
                borderIconTemplate = containerItems.GetChild(0).gameObject;
                Debug.Log($"[PopupClaimQuest] Auto-found BorderIcon template: {borderIconTemplate.name}");
            }
            else
            {
                Debug.LogWarning("[PopupClaimQuest] ContainerItems has no children! Please add a BorderIcon template.");
            }
        }

        // Validate setup
        if (containerItems == null)
        {
            Debug.LogError("[PopupClaimQuest] ❌ ContainerItems NOT ASSIGNED! Assign it in Inspector!");
        }
        else if (borderIconTemplate == null)
        {
            Debug.LogError("[PopupClaimQuest] ❌ BorderIcon template not found! Add a child to ContainerItems or assign manually.");
        }
        else
        {
            Debug.Log($"[PopupClaimQuest] ✓ Initialized successfully. Template: {borderIconTemplate.name}");
        }
    }

    // ========================================
    // SINGLE ITEM DISPLAY
    // ========================================

    public void Open(Sprite iconSprite, string amountText, string titleText, Action confirmCallback)
    {
        // Validation
        if (containerItems == null)
        {
            Debug.LogError("[PopupClaimQuest] Cannot open popup: ContainerItems is null!");
            return;
        }

        if (borderIconTemplate == null)
        {
            Debug.LogError("[PopupClaimQuest] Cannot open popup: borderIconTemplate is null!");
            return;
        }

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

        // Show template dengan data
        if (borderIconTemplate != null)
        {
            borderIconTemplate.SetActive(true);
            UpdateBorderIconContent(borderIconTemplate, icon, amount);
            Debug.Log($"[PopupClaimQuest] ✓ Displayed single item using template");
        }
        else
        {
            Debug.LogError("[PopupClaimQuest] ❌ borderIconTemplate is null! Cannot display item.");
        }
    }

    // ========================================
    // BUNDLE ITEMS DISPLAY
    // ========================================

    public void OpenBundle(List<BundleItemData> items, string title, string description, Action confirmCallback)
    {
        // Validation
        if (containerItems == null)
        {
            Debug.LogError("[PopupClaimQuest] Cannot open bundle popup: ContainerItems is null!");
            return;
        }

        if (borderIconTemplate == null)
        {
            Debug.LogError("[PopupClaimQuest] Cannot open bundle popup: borderIconTemplate is null!");
            return;
        }

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
                if (img.gameObject == borderIcon) continue;
                iconImage = img;
                break;
            }
        }

        if (iconImage == null)
        {
            var rootImg = borderIcon.GetComponent<Image>();
            if (rootImg != null)
            {
                Debug.LogWarning($"[PopupClaimQuest] No child Image found for {borderIcon.name}, falling back to root Image.");
                iconImage = rootImg;
            }
        }

        if (iconImage != null)
        {
            RectTransform rt = iconImage.GetComponent<RectTransform>();
            Vector2 prevSize = rt != null ? rt.sizeDelta : Vector2.zero;

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                if (useNativeSize)
                {
                    iconImage.SetNativeSize();
                }
                else
                {
                    if (rt != null) rt.sizeDelta = prevSize;
                }
                Debug.Log($"[PopupClaimQuest] ✓ Set icon '{icon.name}' on {iconImage.gameObject.name}");
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

    // ========================================
    // DEBUG HELPERS
    // ========================================

    [ContextMenu("Debug: Validate Setup")]
    void DebugValidateSetup()
    {
        Debug.Log("===== PopupClaimQuest Setup Validation =====");
        Debug.Log($"containerItems: {(containerItems != null ? containerItems.name : "NULL")}");
        Debug.Log($"borderIconTemplate: {(borderIconTemplate != null ? borderIconTemplate.name : "NULL")}");
        Debug.Log($"rootPopup: {(rootPopup != null ? rootPopup.name : "NULL")}");
        Debug.Log($"confirmButton: {(confirmButton != null ? confirmButton.name : "NULL")}");

        if (containerItems != null)
        {
            Debug.Log($"ContainerItems children count: {containerItems.childCount}");
            for (int i = 0; i < containerItems.childCount; i++)
            {
                Debug.Log($"  Child {i}: {containerItems.GetChild(i).name}");
            }
        }
    }
    [ContextMenu("Debug: Check BorderIcon Structure")]
    void DebugBorderIconStructure()
    {
        Debug.Log("===== BorderIcon Structure Check =====");

        if (containerItems == null)
        {
            Debug.LogError("ContainerItems is NULL!");
            return;
        }
        Debug.Log($"✓ ContainerItems: {containerItems.name}");

        if (borderIconTemplate == null)
        {
            Debug.LogError("BorderIconTemplate is NULL!");
            return;
        }
        Debug.Log($"✓ BorderIconTemplate: {borderIconTemplate.name}");

        // Check children
        Debug.Log($"BorderIcon children count: {borderIconTemplate.transform.childCount}");
        for (int i = 0; i < borderIconTemplate.transform.childCount; i++)
        {
            var child = borderIconTemplate.transform.GetChild(i);
            var img = child.GetComponent<Image>();
            var txt = child.GetComponent<TMP_Text>();

            Debug.Log($"  Child {i}: {child.name}");
            Debug.Log($"    - Has Image: {(img != null)}");
            Debug.Log($"    - Has TMP_Text: {(txt != null)}");

            // Check grandchildren
            if (child.childCount > 0)
            {
                for (int j = 0; j < child.childCount; j++)
                {
                    var grandchild = child.GetChild(j);
                    Debug.Log($"      Grandchild {j}: {grandchild.name}");
                }
            }
        }
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