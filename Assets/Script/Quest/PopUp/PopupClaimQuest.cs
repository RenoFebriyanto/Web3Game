using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FIXED: PopupClaimQuest dengan icon detection yang benar
/// Support untuk single item dan bundle items
/// </summary>
public class PopupClaimQuest : MonoBehaviour
{
    public static PopupClaimQuest Instance { get; private set; }

    [Header("=== ROOT COMPONENTS ===")]
    public GameObject rootPopup;
    public GameObject blurEffect;

    [Header("=== CONTAINER ITEMS ===")]
    public Transform containerItems;

    [Header("=== TEXT DISPLAYS ===")]
    public TMP_Text collectText;
    public TMP_Text deskItem;

    [Header("=== BUTTONS ===")]
    public Button confirmButton;

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = false;

    private Action onConfirmCallback;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private GameObject templateItem;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurEffect != null) blurEffect.SetActive(false);

        SetupButtons();
        InitializeTemplate();
    }

    void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (blurEffect != null)
        {
            Button blurBtn = blurEffect.GetComponent<Button>();
            if (blurBtn == null)
            {
                blurBtn = blurEffect.AddComponent<Button>();
                blurBtn.transition = Selectable.Transition.None;
            }
            blurBtn.onClick.RemoveAllListeners();
            blurBtn.onClick.AddListener(OnConfirmClicked);
        }
    }

    void InitializeTemplate()
    {
        if (containerItems == null)
        {
            LogError("containerItems is NULL!");
            return;
        }

        if (containerItems.childCount > 0)
        {
            templateItem = containerItems.GetChild(0).gameObject;
            templateItem.SetActive(false);
            Log($"Template initialized: {templateItem.name}");
        }
        else
        {
            LogError("containerItems has no children!");
        }
    }

    // ========================================
    // PUBLIC API: OPEN POPUP
    // ========================================

    public void Open(Sprite icon, string amountText, string title, Action onConfirm)
    {
        Log($"=== OPEN SINGLE ITEM ===");
        Log($"Icon: {(icon != null ? icon.name : "NULL")}");
        Log($"Amount: {amountText}");
        Log($"Title: {title}");

        onConfirmCallback = onConfirm;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        if (collectText != null) collectText.text = "Memperoleh Hadiah";
        if (deskItem != null) deskItem.text = title ?? "";

        DisplaySingleItem(icon, amountText);
        PlayPopupOpenSound();
    }

    public void OpenBundle(List<BundleItemData> items, string title, string description, Action onConfirm)
    {
        Log($"=== OPEN BUNDLE ===");
        Log($"Items count: {items?.Count ?? 0}");

        onConfirmCallback = onConfirm;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        if (collectText != null) collectText.text = "Memperoleh Hadiah";
        if (deskItem != null) deskItem.text = title ?? "";

        DisplayBundleItems(items);
        PlayPopupOpenSound();
    }

    // ========================================
    // DISPLAY LOGIC
    // ========================================

    void DisplaySingleItem(Sprite icon, string amountText)
    {
        ClearSpawnedItems();

        if (templateItem == null)
        {
            LogError("templateItem is NULL!");
            return;
        }

        templateItem.SetActive(true);
        UpdateItemContent(templateItem, icon, amountText);
        Log("✓ Single item displayed");
    }

    void DisplayBundleItems(List<BundleItemData> items)
    {
        ClearSpawnedItems();

        if (templateItem == null)
        {
            LogError("templateItem is NULL!");
            return;
        }

        if (items == null || items.Count == 0)
        {
            LogError("Bundle items list is empty!");
            return;
        }

        templateItem.SetActive(false);

        int spawnCount = 0;
        foreach (var item in items)
        {
            if (item == null || item.icon == null)
            {
                Log("Skipping null bundle item");
                continue;
            }

            GameObject clonedItem = Instantiate(templateItem, containerItems);
            clonedItem.SetActive(true);
            clonedItem.name = $"BundleItem_{spawnCount}_{item.displayName}";

            UpdateItemContent(clonedItem, item.icon, item.amount.ToString());

            spawnedItems.Add(clonedItem);
            spawnCount++;
            Log($"Spawned bundle item {spawnCount}: {item.displayName} x{item.amount}");
        }

        Log($"✓ Bundle displayed with {spawnCount} items");
    }

    void UpdateItemContent(GameObject itemObject, Sprite icon, string amountText)
    {
        if (itemObject == null)
        {
            LogError("itemObject is NULL!");
            return;
        }

        // === UPDATE ICON ===
        Image iconImage = FindIconImage(itemObject);

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
            Log($"✓ Icon set: {icon.name}");
        }
        else
        {
            LogError($"Cannot set icon! iconImage={(iconImage != null)}, icon={(icon != null)}");
        }

        // === UPDATE AMOUNT TEXT ===
        TMP_Text amountTMP = FindAmountText(itemObject);

        if (amountTMP != null)
        {
            if (!string.IsNullOrEmpty(amountText))
            {
                amountTMP.text = amountText;
                amountTMP.gameObject.SetActive(true);
                Log($"✓ Amount set: {amountText}");
            }
            else
            {
                amountTMP.gameObject.SetActive(false);
            }
        }
    }

    // ========================================
    // HELPER: FIND COMPONENTS (IMPROVED)
    // ========================================

    Image FindIconImage(GameObject parent)
    {
        // Try direct child named "Icons"
        Transform iconsChild = parent.transform.Find("Icons");
        if (iconsChild != null)
        {
            Image img = iconsChild.GetComponent<Image>();
            if (img != null)
            {
                Log($"Found icon via Find('Icons'): {iconsChild.name}");
                return img;
            }
        }

        // Fallback: find any Image component (skip background)
        Image[] images = parent.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject == parent) continue; // Skip parent background

            // Skip if name contains "background", "bg", "border"
            string lowerName = img.gameObject.name.ToLower();
            if (lowerName.Contains("background") ||
                lowerName.Contains("bg") ||
                lowerName.Contains("border"))
            {
                continue;
            }

            Log($"Found icon via GetComponentsInChildren: {img.gameObject.name}");
            return img;
        }

        LogError($"Icon Image not found in {parent.name}");
        return null;
    }

    TMP_Text FindAmountText(GameObject parent)
    {
        // Try direct child named "Amount"
        Transform child = parent.transform.Find("Amount");
        if (child != null)
        {
            TMP_Text txt = child.GetComponent<TMP_Text>();
            if (txt != null)
            {
                Log($"Found amount text via Find('Amount'): {child.name}");
                return txt;
            }
        }

        // Fallback: find any TMP_Text
        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0)
        {
            Log($"Found amount text via GetComponentsInChildren: {texts[0].gameObject.name}");
            return texts[0];
        }

        LogError($"Amount TMP_Text not found in {parent.name}");
        return null;
    }

    // ========================================
    // CLEANUP
    // ========================================

    void ClearSpawnedItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
        Log("Cleared spawned items");
    }

    // ========================================
    // BUTTON HANDLERS
    // ========================================

    void OnConfirmClicked()
    {
        Log("Confirm button clicked");
        PlayConfirmSound();

        try
        {
            onConfirmCallback?.Invoke();
        }
        catch (Exception ex)
        {
            LogError($"Confirm callback error: {ex.Message}");
        }

        Close();
    }

    public void Close()
    {
        Log("=== CLOSE POPUP ===");

        onConfirmCallback = null;
        ClearSpawnedItems();

        if (templateItem != null)
        {
            templateItem.SetActive(true);
        }

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurEffect != null) blurEffect.SetActive(false);
    }

    // ========================================
    // AUDIO
    // ========================================

    void PlayPopupOpenSound()
    {
        // Optional: Add popup open sound
    }

    void PlayConfirmSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }
    }

    // ========================================
    // LOGGING
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PopupClaimQuest] {message}");
        }
    }

    void LogError(string message)
    {
        Debug.LogError($"[PopupClaimQuest] ❌ {message}");
    }
}