using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// CategoryContainerUI - COMPLETE FIX
/// Version: 1.2 - All Issues Fixed
/// </summary>
public class CategoryContainerUI : MonoBehaviour
{
    [Header("Container Components")]
    public GameObject headerObject;
    public Transform itemsGrid;
    public TMP_Text headerText;

    [Header("Layout Settings")]
    public int gridColumns = 3;
    public Vector2 cellSize = new Vector2(200f, 200f);
    public Vector2 spacing = new Vector2(10f, 10f);

    private List<GameObject> spawnedItems = new List<GameObject>();
    private GridLayoutGroup gridLayout;

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

    /// <summary>
    /// ✅ NEW: Clear dummy items from prefab
    /// </summary>
    public void ClearDummyItems()
    {
        if (itemsGrid == null) return;

        // Destroy all existing children (dummy items from prefab)
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

    /// <summary>
    /// ✅ Setup grid layout - called AFTER ClearDummyItems
    /// </summary>
    public void SetupGridLayout()
    {
        if (itemsGrid == null)
        {
            Debug.LogError($"[CategoryContainer] itemsGrid not assigned!");
            return;
        }

        gridLayout = itemsGrid.GetComponent<GridLayoutGroup>();
        
        if (gridLayout == null)
        {
            gridLayout = itemsGrid.gameObject.AddComponent<GridLayoutGroup>();
        }

        // ✅ ALWAYS update settings (don't trust prefab values)
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridColumns;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        Debug.Log($"[CategoryContainer] GridLayout: {gridColumns}cols, cell={cellSize}, spacing={spacing}");
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
        }
        else
        {
            Debug.LogWarning($"[CategoryContainer] ShopItemUI not found on {itemObj.name}!");
            Destroy(itemObj);
        }
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
    }
}