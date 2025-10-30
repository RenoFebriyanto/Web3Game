using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UPDATED: Support multiple items display untuk bundle purchases.
/// Structure:
///   - Single item: show icon + amount text
///   - Multiple items (bundle): spawn IconsItem prefabs di grid
/// </summary>
public class PopupClaimQuest : MonoBehaviour
{
    public static PopupClaimQuest Instance { get; private set; }

    [Header("UI refs (assign in Inspector)")]
    public GameObject rootPopup;
    public GameObject blurOverlay;

    [Header("Single Item Display")]
    public Image iconImage;              // Icon untuk single item
    public TMP_Text rewardAmountText;    // Amount text untuk single item
    public GameObject singleItemRoot;    // Root untuk single item display (hide untuk bundle)

    [Header("Bundle Items Display")]
    public Transform bundleItemsContainer;  // Container untuk spawn bundle items
    public GameObject bundleItemPrefab;     // Prefab IconsItem untuk bundle
    public int maxVisibleBundleItems = 5;   // Max items visible (enable scroll if more)
    public ScrollRect bundleScrollRect;     // Optional: scroll untuk bundle items

    [Header("Info Display")]
    public TMP_Text titleText;           // Title/description
    public TMP_Text deskText;            // Optional: extra description

    [Header("Buttons")]
    public Button confirmButton;
    public Button closeButton;           // Optional: X button

    // Runtime
    Action onConfirm;
    List<GameObject> spawnedBundleItems = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurOverlay != null) blurOverlay.SetActive(false);

        // Setup buttons
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButton);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        // Blur overlay click to close
        if (blurOverlay != null)
        {
            var btn = blurOverlay.GetComponent<Button>();
            if (btn == null)
            {
                btn = blurOverlay.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(Close);
        }

        // Auto-find scroll rect
        if (bundleScrollRect == null && bundleItemsContainer != null)
        {
            bundleScrollRect = bundleItemsContainer.GetComponentInParent<ScrollRect>();
        }
    }

    // ========================================
    // SINGLE ITEM DISPLAY (Quest reward, single shop item)
    // ========================================

    /// <summary>
    /// Open popup dengan single item display (quest/single shop item)
    /// </summary>
    public void Open(Sprite iconSprite, string rewardText, string titleText, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurOverlay != null) blurOverlay.SetActive(true);

        // Show single item display
        ShowSingleItem(iconSprite, rewardText, titleText);
    }

    void ShowSingleItem(Sprite icon, string amountText, string title)
    {
        // Show single item root, hide bundle
        if (singleItemRoot != null) singleItemRoot.SetActive(true);
        if (bundleItemsContainer != null) bundleItemsContainer.gameObject.SetActive(false);

        // Clear any spawned bundle items
        ClearBundleItems();

        // Update single item display
        if (iconImage != null) iconImage.sprite = icon;
        if (rewardAmountText != null) rewardAmountText.text = amountText ?? "";
        if (titleText != null) this.titleText.text = title ?? "";
        if (deskText != null) deskText.text = "";

        // Disable scroll (not used for single item)
        if (bundleScrollRect != null) bundleScrollRect.enabled = false;
    }

    // ========================================
    // BUNDLE ITEMS DISPLAY (Shop bundle)
    // ========================================

    /// <summary>
    /// Open popup dengan bundle items display (untuk shop bundle)
    /// </summary>
    public void OpenBundle(List<BundleItemData> items, string title, string description, Action confirmCallback)
    {
        onConfirm = confirmCallback;

        if (rootPopup != null) rootPopup.SetActive(true);
        if (blurOverlay != null) blurOverlay.SetActive(true);

        // Show bundle display
        ShowBundleItems(items, title, description);
    }

    void ShowBundleItems(List<BundleItemData> items, string title, string description)
    {
        // Hide single item display, show bundle container
        if (singleItemRoot != null) singleItemRoot.SetActive(false);
        if (bundleItemsContainer != null) bundleItemsContainer.gameObject.SetActive(true);

        // Clear previous bundle items
        ClearBundleItems();

        // Update title/description
        if (titleText != null) this.titleText.text = title ?? "";
        if (deskText != null) deskText.text = description ?? "";

        // Spawn bundle items
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[PopupClaimQuest] No items in bundle!");
            return;
        }

        if (bundleItemPrefab == null)
        {
            Debug.LogError("[PopupClaimQuest] bundleItemPrefab not assigned!");
            return;
        }

        int spawnedCount = 0;
        foreach (var item in items)
        {
            if (item == null || item.icon == null) continue;

            var go = Instantiate(bundleItemPrefab, bundleItemsContainer);
            go.SetActive(true);

            var display = go.GetComponent<BundleItemDisplay>();
            if (display != null)
            {
                display.Setup(item.icon, item.amount);
                spawnedBundleItems.Add(go);
                spawnedCount++;
            }
            else
            {
                Debug.LogWarning("[PopupClaimQuest] bundleItemPrefab doesn't have BundleItemDisplay!");
                Destroy(go);
            }
        }

        // Enable scroll if items > maxVisibleBundleItems
        if (bundleScrollRect != null)
        {
            bundleScrollRect.enabled = spawnedCount > maxVisibleBundleItems;

            if (bundleScrollRect.enabled)
            {
                Canvas.ForceUpdateCanvases();
                bundleScrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        Debug.Log($"[PopupClaimQuest] Displayed {spawnedCount} bundle items. Scroll: {(bundleScrollRect != null && bundleScrollRect.enabled)}");
    }

    void ClearBundleItems()
    {
        foreach (var go in spawnedBundleItems)
        {
            if (go != null) Destroy(go);
        }
        spawnedBundleItems.Clear();
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

        // Clear bundle items
        ClearBundleItems();

        // Show single item root (default state)
        if (singleItemRoot != null) singleItemRoot.SetActive(true);
        if (bundleItemsContainer != null) bundleItemsContainer.gameObject.SetActive(false);

        // Reset scroll
        if (bundleScrollRect != null)
        {
            bundleScrollRect.horizontalNormalizedPosition = 0f;
            bundleScrollRect.enabled = false;
        }

        if (rootPopup != null) rootPopup.SetActive(false);
        if (blurOverlay != null) blurOverlay.SetActive(false);
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