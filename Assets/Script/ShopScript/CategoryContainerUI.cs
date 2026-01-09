using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// CategoryContainerUI - FIXED v5.0
/// </summary>
public class CategoryContainerUI : MonoBehaviour
{
    [Header("Container Components")]
    public GameObject headerObject;
    public Transform itemsGrid;
    public TMP_Text headerText;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private GridLayoutGroup gridLayout;
    private bool needsRefresh = false;

    void Awake()
    {
        Log($"Awake: {gameObject.name}");

        if (itemsGrid != null)
        {
            gridLayout = itemsGrid.GetComponent<GridLayoutGroup>();
            
            if (gridLayout != null)
            {
                Log("✓ GridLayout found");
            }
            else
            {
                LogWarning("✗ GridLayout NOT found!");
            }
        }
        else
        {
            LogWarning("✗ itemsGrid is NULL!");
        }
    }

    void Start()
    {
        Log($"Start: {gameObject.name} (active={gameObject.activeSelf})");
    }

    void LateUpdate()
    {
        if (needsRefresh)
        {
            needsRefresh = false;
            RefreshLayout();
        }
    }

    void OnEnable()
    {
        Log($"OnEnable: {gameObject.name}");

        if (needsRefresh)
        {
            RefreshLayout();
        }
    }

    public void SetHeaderText(string text)
    {
        if (headerText != null)
        {
            headerText.text = text;
        }
        
        if (headerObject != null)
        {
            headerObject.SetActive(!string.IsNullOrEmpty(text));
        }

        Log($"Header set: '{text}'");
    }

    public void ClearDummyItems()
    {
        if (itemsGrid == null) return;

        int childCount = itemsGrid.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            var child = itemsGrid.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        Log($"Cleared {childCount} dummy items");
    }

    public void AddItem(GameObject itemPrefab, ShopItemData data, ShopManager manager)
    {
        if (itemsGrid == null || itemPrefab == null)
        {
            LogWarning("Cannot add item - missing references!");
            return;
        }

        GameObject itemObj = Instantiate(itemPrefab, itemsGrid);
        itemObj.name = $"ShopItem_{data.itemId}";

        var ui = itemObj.GetComponent<ShopItemUI>();
        if (ui != null)
        {
            ui.Setup(data, manager);
            spawnedItems.Add(itemObj);
            needsRefresh = true;
        }
        else
        {
            LogWarning($"ShopItemUI not found on {itemObj.name}!");
            Destroy(itemObj);
        }
    }

    public void RefreshLayout()
    {
        if (itemsGrid == null)
        {
            LogWarning("Cannot refresh - itemsGrid is null!");
            return;
        }

        Log("RefreshLayout called");

        var gridRect = itemsGrid.GetComponent<RectTransform>();
        if (gridRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }

        var containerRect = GetComponent<RectTransform>();
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        Log($"Layout refreshed (pos={containerRect?.anchoredPosition})");
    }

    public void ForceRefreshNow()
    {
        Log($"=== ForceRefreshNow START ===");
        Log($"  Active: {gameObject.activeInHierarchy}");
        Log($"  Items: {spawnedItems.Count}");

        if (!gameObject.activeInHierarchy)
        {
            LogWarning("Cannot ForceRefreshNow - GameObject inactive!");
            return;
        }

        // Step 1: Force itemsGrid layout
        if (itemsGrid != null)
        {
            var gridRect = itemsGrid.GetComponent<RectTransform>();
            if (gridRect != null)
            {
                Log("  Rebuilding itemsGrid layout...");
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                Log($"    Grid pos: {gridRect.anchoredPosition}");
            }
        }

        // Step 2: Force container layout
        var containerRect = GetComponent<RectTransform>();
        if (containerRect != null)
        {
            Log("  Rebuilding container layout...");
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            Log($"    Container pos: {containerRect.anchoredPosition}");
        }

        // Step 3: Force canvas update
        Canvas.ForceUpdateCanvases();

        // Step 4: Check positions
        if (containerRect != null)
        {
            Log($"  Final container pos: {containerRect.anchoredPosition}");
            
            // If position is crazy, reset it
            if (Mathf.Abs(containerRect.anchoredPosition.y) > 10000)
            {
                LogWarning($"  ⚠️ Detected invalid position! Resetting...");
                containerRect.anchoredPosition = Vector2.zero;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }
        }

        Log("=== ForceRefreshNow COMPLETE ===");
    }

    public void OnAllItemsAdded()
    {
        needsRefresh = true;
        Log($"Items added, marked for refresh (active={gameObject.activeInHierarchy})");
    }

    public void ClearItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        
        spawnedItems.Clear();
    }

    public int GetItemCount()
    {
        return spawnedItems.Count;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        
        if (active && needsRefresh)
        {
            RefreshLayout();
        }
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CategoryContainer:{gameObject.name}] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[CategoryContainer:{gameObject.name}] {message}");
    }
}