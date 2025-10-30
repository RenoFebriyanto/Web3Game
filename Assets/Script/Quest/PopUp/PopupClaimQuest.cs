using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// REBUILT: PopupClaimQuest dengan structure detection yang lebih robust
/// Support untuk single item dan bundle items
/// Integrated dengan SoundManager untuk audio feedback
/// </summary>
public class PopupClaimQuest : MonoBehaviour
{
    public static PopupClaimQuest Instance { get; private set; }

    [Header("=== ROOT COMPONENTS ===")]
    [Tooltip("Root popup GameObject (ContainerPopUp)")]
    public GameObject rootPopup;

    [Tooltip("Blur overlay background")]
    public GameObject blurEffect;

    [Header("=== CONTAINER ITEMS (CRITICAL!) ===")]
    [Tooltip("ContainerItems - parent untuk BorderIcon items")]
    public Transform containerItems;

    [Header("=== TEXT DISPLAYS ===")]
    public TMP_Text collectText;  // "Memperoleh Hadiah"
    public TMP_Text deskItem;     // Item description/title

    [Header("=== BUTTONS ===")]
    public Button confirmButton;
    public Button closeButton;

    [Header("=== PREFAB TEMPLATE ===")]
    [Tooltip("BorderIcon prefab untuk spawn items (optional - will auto-find if null)")]
    public GameObject borderIconPrefab;

    [Header("=== LAYOUT SETTINGS ===")]
    [Tooltip("Apakah menggunakan native size untuk icon sprites")]
    public bool useNativeSize = false;

    [Tooltip("Max items yang ditampilkan (untuk scroll detection)")]
    public int maxVisibleItems = 5;

    [Header("=== DEBUG ===")]
    public bool enableDebugLogs = true;

    // Runtime
    private Action onConfirmCallback;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private GameObject templateItem;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Log("=== POPUP CLAIM QUEST AWAKE ===");

        // Hide popup at start
        if (rootPopup != null)
        {
            rootPopup.SetActive(false);
        }
        if (blurEffect != null)
        {
            blurEffect.SetActive(false);
        }

        // Setup buttons
        SetupButtons();

        // Initialize template
        InitializeTemplate();

        // Validate setup
        ValidateSetup();
    }

    void SetupButtons()
    {
        // Confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        // Close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        // Blur effect click to close
        if (blurEffect != null)
        {
            Button blurBtn = blurEffect.GetComponent<Button>();
            if (blurBtn == null)
            {
                blurBtn = blurEffect.AddComponent<Button>();
                blurBtn.transition = Selectable.Transition.None;
            }
            blurBtn.onClick.RemoveAllListeners();
            blurBtn.onClick.AddListener(OnCloseClicked);
        }
    }

    void InitializeTemplate()
    {
        Log("--- Initializing Template ---");

        if (containerItems == null)
        {
            LogError("containerItems is NULL! Cannot initialize template.");
            return;
        }

        // Try to find template from containerItems children
        if (borderIconPrefab == null)
        {
            if (containerItems.childCount > 0)
            {
                templateItem = containerItems.GetChild(0).gameObject;
                Log($"Auto-found template from containerItems: {templateItem.name}");
            }
            else
            {
                LogError("containerItems has no children! Add a BorderIcon template as child.");
                return;
            }
        }
        else
        {
            templateItem = borderIconPrefab;
            Log($"Using assigned borderIconPrefab: {templateItem.name}");
        }

        // Validate template structure
        ValidateTemplateStructure(templateItem);

        // Hide template (will be cloned later)
        if (templateItem != null)
        {
            templateItem.SetActive(false);
            Log($"Template '{templateItem.name}' hidden and ready for cloning");
        }
    }

    void ValidateTemplateStructure(GameObject template)
    {
        if (template == null)
        {
            LogError("Template is NULL!");
            return;
        }

        Log($"=== Validating Template Structure: {template.name} ===");

        // Check for Image components
        Image[] images = template.GetComponentsInChildren<Image>(true);
        Log($"Found {images.Length} Image components in template");

        foreach (var img in images)
        {
            Log($"  - {img.gameObject.name}: sprite={(img.sprite != null ? img.sprite.name : "NULL")}");
        }

        // Check for TMP_Text components
        TMP_Text[] texts = template.GetComponentsInChildren<TMP_Text>(true);
        Log($"Found {texts.Length} TMP_Text components in template");

        foreach (var txt in texts)
        {
            Log($"  - {txt.gameObject.name}: text='{txt.text}'");
        }

        // Check for specific named children
        Transform iconChild = template.transform.Find("Icons");
        Transform amountChild = template.transform.Find("Amount");

        Log($"Icons child: {(iconChild != null ? "FOUND" : "NOT FOUND")}");
        Log($"Amount child: {(amountChild != null ? "FOUND" : "NOT FOUND")}");
    }

    bool ValidateSetup()
    {
        bool valid = true;

        if (rootPopup == null)
        {
            LogError("rootPopup is NULL!");
            valid = false;
        }

        if (containerItems == null)
        {
            LogError("containerItems is NULL!");
            valid = false;
        }

        if (templateItem == null)
        {
            LogError("templateItem is NULL!");
            valid = false;
        }

        if (confirmButton == null)
        {
            LogError("confirmButton is NULL!");
            valid = false;
        }

        if (valid)
        {
            Log("✓ Setup validation PASSED");
        }
        else
        {
            LogError("✗ Setup validation FAILED! Check Inspector assignments.");
        }

        return valid;
    }

    // ========================================
    // PUBLIC API: OPEN POPUP
    // ========================================

    /// <summary>
    /// Open popup dengan SINGLE ITEM
    /// </summary>
    public void Open(Sprite icon, string amountText, string title, Action onConfirm)
    {
        if (!ValidateSetup())
        {
            LogError("Cannot open popup: Setup validation failed!");
            return;
        }

        Log($"=== OPEN SINGLE ITEM ===");
        Log($"Icon: {(icon != null ? icon.name : "NULL")}");
        Log($"Amount: {amountText}");
        Log($"Title: {title}");

        onConfirmCallback = onConfirm;

        // Show popup
        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        // Update texts
        if (collectText != null) collectText.text = "Memperoleh Hadiah";
        if (deskItem != null) deskItem.text = title ?? "";

        // Display single item
        DisplaySingleItem(icon, amountText);

        // Play sound
        PlayPopupOpenSound();
    }

    /// <summary>
    /// Open popup dengan BUNDLE ITEMS (multiple items)
    /// </summary>
    public void OpenBundle(List<BundleItemData> items, string title, string description, Action onConfirm)
    {
        if (!ValidateSetup())
        {
            LogError("Cannot open bundle popup: Setup validation failed!");
            return;
        }

        Log($"=== OPEN BUNDLE ===");
        Log($"Items count: {items?.Count ?? 0}");
        Log($"Title: {title}");

        onConfirmCallback = onConfirm;

        // Show popup
        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurEffect != null) blurEffect.SetActive(true);

        // Update texts
        if (collectText != null) collectText.text = "Memperoleh Hadiah";
        if (deskItem != null) deskItem.text = title ?? "";

        // Display bundle items
        DisplayBundleItems(items);

        // Play sound
        PlayPopupOpenSound();
    }

    // ========================================
    // DISPLAY LOGIC
    // ========================================

    void DisplaySingleItem(Sprite icon, string amountText)
    {
        Log("--- Display Single Item ---");

        // Clear previous spawned items
        ClearSpawnedItems();

        if (templateItem == null)
        {
            LogError("templateItem is NULL! Cannot display item.");
            return;
        }

        // Show template directly (not cloning for single item)
        templateItem.SetActive(true);
        UpdateItemContent(templateItem, icon, amountText);

        Log("✓ Single item displayed using template");
    }

    void DisplayBundleItems(List<BundleItemData> items)
    {
        Log("--- Display Bundle Items ---");

        // Clear previous spawned items
        ClearSpawnedItems();

        if (templateItem == null)
        {
            LogError("templateItem is NULL! Cannot display bundle.");
            return;
        }

        if (items == null || items.Count == 0)
        {
            LogError("Bundle items list is empty!");
            return;
        }

        // Hide template
        templateItem.SetActive(false);

        // Spawn items (clone template for each item)
        int spawnCount = 0;
        foreach (var item in items)
        {
            if (item == null)
            {
                Log("Skipping null bundle item");
                continue;
            }

            if (item.icon == null)
            {
                Log($"Skipping item with null icon: {item.displayName}");
                continue;
            }

            // Clone template
            GameObject clonedItem = Instantiate(templateItem, containerItems);
            clonedItem.SetActive(true);
            clonedItem.name = $"BundleItem_{spawnCount}_{item.displayName}";

            // Update content
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
            LogError("itemObject is NULL in UpdateItemContent!");
            return;
        }

        Log($"Updating content for: {itemObject.name}");

        // === UPDATE ICON ===
        Image iconImage = FindIconImage(itemObject);

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;

            if (useNativeSize)
            {
                iconImage.SetNativeSize();
            }

            Log($"✓ Icon set: {icon.name} on {iconImage.gameObject.name}");
        }
        else
        {
            LogError($"Cannot set icon! iconImage={(iconImage != null ? iconImage.gameObject.name : "NULL")}, icon={(icon != null ? icon.name : "NULL")}");
        }

        // === UPDATE AMOUNT TEXT ===
        TMP_Text amountTMP = FindAmountText(itemObject);

        if (amountTMP != null)
        {
            if (!string.IsNullOrEmpty(amountText))
            {
                amountTMP.text = amountText;
                amountTMP.gameObject.SetActive(true);
                Log($"✓ Amount set: {amountText} on {amountTMP.gameObject.name}");
            }
            else
            {
                amountTMP.gameObject.SetActive(false);
            }
        }
        else
        {
            LogError("Amount TMP_Text not found!");
        }
    }

    // ========================================
    // HELPER: FIND COMPONENTS
    // ========================================

    Image FindIconImage(GameObject parent)
    {
        // Try direct child named "Icons" or "Icon"
        string[] iconNames = new string[] { "Icons", "Icon", "IconImage", "Image" };

        foreach (var name in iconNames)
        {
            Transform child = parent.transform.Find(name);
            if (child != null)
            {
                Image img = child.GetComponent<Image>();
                if (img != null)
                {
                    Log($"Found icon via Find('{name}'): {child.name}");
                    return img;
                }
            }
        }

        // Fallback: find any child Image (skip if it's the parent/background)
        Image[] images = parent.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject == parent) continue; // Skip parent background image

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

        // Fallback: find any child TMP_Text
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

        // Play confirm sound
        PlayConfirmSound();

        // Execute callback
        try
        {
            onConfirmCallback?.Invoke();
        }
        catch (Exception ex)
        {
            LogError($"Confirm callback error: {ex.Message}");
        }

        // Close popup
        Close();
    }

    void OnCloseClicked()
    {
        Log("Close button clicked");

        // Play close sound
        PlayCloseSound();

        // Close popup (no callback)
        Close();
    }

    public void Close()
    {
        Log("=== CLOSE POPUP ===");

        // Clear callback
        onConfirmCallback = null;

        // Clear spawned items
        ClearSpawnedItems();

        // Show template again (for next use)
        if (templateItem != null)
        {
            templateItem.SetActive(true);
        }

        // Hide popup
        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurEffect != null) blurEffect.SetActive(false);
    }

    // ========================================
    // AUDIO INTEGRATION
    // ========================================

    void PlayPopupOpenSound()
    {
        // TODO: Add popup open sound (optional)
        // SoundManager.Instance?.PlayPopupOpen();
    }

    void PlayConfirmSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPurchaseSuccess();
        }
    }

    void PlayCloseSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
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

    // ========================================
    // DEBUG HELPERS
    // ========================================

    [ContextMenu("Debug: Validate Setup")]
    void Debug_ValidateSetup()
    {
        ValidateSetup();
    }

    [ContextMenu("Debug: Print Hierarchy")]
    void Debug_PrintHierarchy()
    {
        if (containerItems == null)
        {
            LogError("containerItems is NULL!");
            return;
        }

        Log("=== ContainerItems Hierarchy ===");
        Log($"Children count: {containerItems.childCount}");

        for (int i = 0; i < containerItems.childCount; i++)
        {
            Transform child = containerItems.GetChild(i);
            Log($"Child {i}: {child.name} (active: {child.gameObject.activeSelf})");

            // Print child's children
            for (int j = 0; j < child.childCount; j++)
            {
                Transform grandchild = child.GetChild(j);
                var img = grandchild.GetComponent<Image>();
                var txt = grandchild.GetComponent<TMP_Text>();

                Log($"  - {grandchild.name}: Image={img != null}, Text={txt != null}");
            }
        }
    }

    [ContextMenu("Test: Open Single Item")]
    void Test_OpenSingleItem()
    {
        // Create test sprite (white square)
        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        Sprite testSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        Open(testSprite, "x99", "Test Item", () => {
            Log("Test confirm callback executed!");
        });
    }
}

/// <summary>
/// Bundle item data structure
/// </summary>
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