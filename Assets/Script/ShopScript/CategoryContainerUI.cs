using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CategoryContainerUI - FIXED v5.0
/// ✅ Better layout refresh timing
/// ✅ No coroutine errors when inactive
/// </summary>
public class CategoryContainerUI : MonoBehaviour
{
    [Header("Container Components")]
    public GameObject headerObject;
    public Transform itemsGrid;
    public TMP_Text headerText;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private GridLayoutGroup gridLayout;
    private bool needsRefresh = false;
    private int refreshFrameCount = 0; // ✅ NEW: Track frames

    void Awake()
    {
        if (itemsGrid != null)
        {
            gridLayout = itemsGrid.GetComponent<GridLayoutGroup>();
            
            if (gridLayout != null)
            {
                Debug.Log($"[CategoryContainer] ✓ GridLayout found");
            }
        }
    }

    void LateUpdate()
    {
        // ✅ IMPROVED: Multi-frame refresh untuk ensure layout settled
        if (needsRefresh)
        {
            refreshFrameCount++;
            
            // Refresh di frame 1, 2, dan 3
            if (refreshFrameCount <= 3)
            {
                RefreshLayout();
            }
            else
            {
                needsRefresh = false;
                refreshFrameCount = 0;
            }
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

        Debug.Log($"[CategoryContainer] Cleared {childCount} dummy items");
    }

    public void AddItem(GameObject itemPrefab, ShopItemData data, ShopManager manager)
    {
        if (itemsGrid == null || itemPrefab == null)
        {
            Debug.LogError("[CategoryContainer] Cannot add item - missing references!");
            return;
        }

        GameObject itemObj = Instantiate(itemPrefab, itemsGrid);
        itemObj.name = $"ShopItem_{data.itemId}";

        var ui = itemObj.GetComponent<ShopItemUI>();
        if (ui != null)
        {
            ui.Setup(data, manager);
            spawnedItems.Add(itemObj);
            
            // ✅ Mark untuk refresh
            needsRefresh = true;
            refreshFrameCount = 0;
        }
        else
        {
            Debug.LogWarning($"[CategoryContainer] ShopItemUI not found on {itemObj.name}!");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// ✅ IMPROVED: Robust layout refresh
    /// </summary>
    public void RefreshLayout()
    {
        if (itemsGrid == null) return;

        // Force rebuild grid layout
        var gridRect = itemsGrid.GetComponent<RectTransform>();
        if (gridRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }

        // Force rebuild container
        var containerRect = GetComponent<RectTransform>();
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
        
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// ✅ SIMPLIFIED: No coroutine, just mark for refresh
    /// </summary>
    public void OnAllItemsAdded()
    {
        needsRefresh = true;
        refreshFrameCount = 0;
        
        Debug.Log($"[CategoryContainer] OnAllItemsAdded - marked for refresh");
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
        
        // ✅ Force refresh saat diaktifkan
        if (active)
        {
            needsRefresh = true;
            refreshFrameCount = 0;
        }
    }
}