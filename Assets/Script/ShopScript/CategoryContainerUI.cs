using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CategoryContainerUI - FIXED v4.0
/// ✅ Fixed coroutine error saat GameObject inactive
/// ✅ Auto refresh layout setelah items added
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
        // ✅ Refresh layout di LateUpdate jika perlu
        if (needsRefresh)
        {
            needsRefresh = false;
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
            
            // ✅ Mark untuk refresh di LateUpdate
            needsRefresh = true;
        }
        else
        {
            Debug.LogWarning($"[CategoryContainer] ShopItemUI not found on {itemObj.name}!");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// ✅ Refresh layout setelah items added
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
    }

    /// <summary>
    /// ✅ FIXED: Call setelah semua items added - check GameObject active sebelum StartCoroutine
    /// </summary>
    public void OnAllItemsAdded()
    {
        // ✅ FIX: Jangan start coroutine jika GameObject inactive
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RefreshLayoutDelayed());
        }
        else
        {
            // Jika inactive, langsung mark untuk refresh di LateUpdate saat active
            needsRefresh = true;
        }
    }

    IEnumerator RefreshLayoutDelayed()
    {
        // Wait 2 frames untuk Canvas selesai layout
        yield return null;
        yield return null;
        
        RefreshLayout();
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
        
        // ✅ Refresh layout saat diaktifkan
        if (active && needsRefresh)
        {
            RefreshLayout();
        }
    }
}