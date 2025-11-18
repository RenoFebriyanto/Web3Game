using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// CategoryContainerUI - Container untuk satu category dengan header + grid items
/// Version: 1.0 - Automated Layout
/// </summary>
public class CategoryContainerUI : MonoBehaviour
{
    [Header("Container Components")]
    [Tooltip("Header GameObject (CategoryHeader)")]
    public GameObject headerObject;
    
    [Tooltip("Grid container untuk items")]
    public Transform itemsGrid;
    
    [Tooltip("TMP_Text component di header")]
    public TMP_Text headerText;

    [Header("Layout Settings")]
    [Tooltip("Grid columns (default: 3)")]
    public int gridColumns = 3;
    
    [Tooltip("Cell size untuk grid items")]
    public Vector2 cellSize = new Vector2(150f, 200f);
    
    [Tooltip("Spacing antara items")]
    public Vector2 spacing = new Vector2(10f, 10f);

    private List<GameObject> spawnedItems = new List<GameObject>();
    private GridLayoutGroup gridLayout;

    void Awake()
    {
        SetupGridLayout();
    }

    /// <summary>
    /// Setup GridLayoutGroup di itemsGrid
    /// </summary>
    void SetupGridLayout()
    {
        if (itemsGrid == null)
        {
            Debug.LogError($"[CategoryContainer] itemsGrid not assigned on {gameObject.name}!");
            return;
        }

        gridLayout = itemsGrid.GetComponent<GridLayoutGroup>();
        
        if (gridLayout == null)
        {
            gridLayout = itemsGrid.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Setup grid properties
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridColumns;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        Debug.Log($"[CategoryContainer] ✓ GridLayout setup: {gridColumns} columns");
    }

    /// <summary>
    /// Set category header text
    /// </summary>
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
    /// Spawn shop item di grid
    /// </summary>
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

    /// <summary>
    /// Clear semua spawned items
    /// </summary>
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

    /// <summary>
    /// Get jumlah items di container ini
    /// </summary>
    public int GetItemCount()
    {
        return spawnedItems.Count;
    }

    /// <summary>
    /// Show/hide container
    /// </summary>
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}