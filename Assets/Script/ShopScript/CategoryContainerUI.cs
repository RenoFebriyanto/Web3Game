using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CategoryContainerUI - FIXED v4.2
/// ✅ Added ForceRefreshNow() method
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
        if (needsRefresh)
        {
            needsRefresh = false;
            RefreshLayout();
        }
    }

    void OnEnable()
    {
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
            needsRefresh = true;
        }
        else
        {
            Debug.LogWarning($"[CategoryContainer] ShopItemUI not found on {itemObj.name}!");
            Destroy(itemObj);
        }
    }

    public void RefreshLayout()
    {
        if (itemsGrid == null) return;

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
    }

    
public void ForceRefreshNow()
{
    if (!gameObject.activeInHierarchy)
    {
        Debug.LogWarning("[CategoryContainer] Cannot ForceRefreshNow - GameObject inactive");
        return;
    }

    // Force rebuild semua layout
    if (itemsGrid != null)
    {
        var gridRect = itemsGrid.GetComponent<RectTransform>();
        if (gridRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }
    }

    var containerRect = GetComponent<RectTransform>();
    if (containerRect != null)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
    }

    // Force canvas update
    Canvas.ForceUpdateCanvases();

    Debug.Log($"[CategoryContainer] ✓ Force refresh NOW complete (active={gameObject.activeSelf})");
}

    public void OnAllItemsAdded()
    {
        needsRefresh = true;
        Debug.Log($"[CategoryContainer] Items added, marked for refresh (active={gameObject.activeInHierarchy})");
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
}