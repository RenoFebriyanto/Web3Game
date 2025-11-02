using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-populate level selection UI from LevelDatabase
/// Attach ke ContentLevel GameObject
/// Works with LevelGridLayoutManager for automatic positioning
/// </summary>
public class LevelPopulator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your LevelDatabase here")]
    public LevelDatabase levelDatabase;

    [Tooltip("Prefab for level selection item (must have LevelSelectionItem component)")]
    public GameObject levelItemPrefab;

    [Header("Settings")]
    [Tooltip("Auto-populate on Start")]
    public bool autoPopulateOnStart = true;

    [Tooltip("Clear existing items before populate")]
    public bool clearExistingItems = true;

    [Header("Optional: Layout Manager")]
    [Tooltip("Optional: Layout manager for snake pattern (will auto-find if null)")]
    public LevelGridLayoutManager layoutManager;

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
        // Auto-find layout manager
        if (layoutManager == null)
        {
            layoutManager = GetComponent<LevelGridLayoutManager>();
        }
    }

    void Start()
    {
        if (autoPopulateOnStart)
        {
            PopulateLevels();
        }
    }

    /// <summary>
    /// Populate level UI from database
    /// </summary>
    [ContextMenu("Populate Levels")]
    public void PopulateLevels()
    {
        if (levelDatabase == null)
        {
            Debug.LogError("[LevelPopulator] LevelDatabase not assigned!");
            return;
        }

        if (levelItemPrefab == null)
        {
            Debug.LogError("[LevelPopulator] Level item prefab not assigned!");
            return;
        }

        if (levelDatabase.levels == null || levelDatabase.levels.Count == 0)
        {
            Debug.LogWarning("[LevelPopulator] LevelDatabase has no levels!");
            return;
        }

        // Clear existing
        if (clearExistingItems)
        {
            ClearLevels();
        }

        Debug.Log($"[LevelPopulator] Populating {levelDatabase.levels.Count} levels...");

        // Spawn level items
        foreach (var levelConfig in levelDatabase.levels)
        {
            if (levelConfig == null) continue;

            GameObject levelItem = Instantiate(levelItemPrefab, transform);
            levelItem.name = $"Level_{levelConfig.number}";

            // Setup LevelSelectionItem
            LevelSelectionItem selectionItem = levelItem.GetComponent<LevelSelectionItem>();
            if (selectionItem != null)
            {
                selectionItem.levelConfig = levelConfig;
                selectionItem.Refresh();
            }
            else
            {
                Debug.LogWarning($"[LevelPopulator] Level item prefab missing LevelSelectionItem component!");
            }

            spawnedItems.Add(levelItem);
        }

        Debug.Log($"[LevelPopulator] ✓ Spawned {spawnedItems.Count} level items");

        // Trigger layout refresh
        if (layoutManager != null)
        {
            // Wait one frame for RectTransforms to be ready
            StartCoroutine(RefreshLayoutDelayed());
        }
    }

    System.Collections.IEnumerator RefreshLayoutDelayed()
    {
        yield return new WaitForEndOfFrame();

        if (layoutManager != null)
        {
            layoutManager.RefreshLayout();
            Debug.Log("[LevelPopulator] Layout refreshed");
        }
    }

    /// <summary>
    /// Clear all spawned level items
    /// </summary>
    [ContextMenu("Clear Levels")]
    public void ClearLevels()
    {
        // Destroy spawned items
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();

        // Also destroy any remaining children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        Debug.Log("[LevelPopulator] Cleared all level items");
    }

    /// <summary>
    /// Refresh all level items (e.g., after unlock/progress change)
    /// </summary>
    public void RefreshAllLevels()
    {
        foreach (var item in spawnedItems)
        {
            if (item == null) continue;

            LevelSelectionItem selectionItem = item.GetComponent<LevelSelectionItem>();
            if (selectionItem != null)
            {
                selectionItem.Refresh();
            }
        }

        Debug.Log("[LevelPopulator] Refreshed all level items");
    }

    /// <summary>
    /// Get spawned item for specific level number
    /// </summary>
    public GameObject GetLevelItem(int levelNumber)
    {
        foreach (var item in spawnedItems)
        {
            if (item == null) continue;

            LevelSelectionItem selectionItem = item.GetComponent<LevelSelectionItem>();
            if (selectionItem != null && selectionItem.levelConfig != null)
            {
                if (selectionItem.levelConfig.number == levelNumber)
                {
                    return item;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Scroll to specific level
    /// </summary>
    public void ScrollToLevel(int levelNumber)
    {
        GameObject levelItem = GetLevelItem(levelNumber);
        if (levelItem == null)
        {
            Debug.LogWarning($"[LevelPopulator] Level {levelNumber} not found");
            return;
        }

        // Get ScrollRect from parent
        var scrollRect = GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogWarning("[LevelPopulator] No ScrollRect found in parent");
            return;
        }

        // Calculate normalized position
        RectTransform contentRect = GetComponent<RectTransform>();
        RectTransform itemRect = levelItem.GetComponent<RectTransform>();

        if (contentRect != null && itemRect != null)
        {
            Canvas.ForceUpdateCanvases();

            // Calculate vertical position (0 = top, 1 = bottom)
            float contentHeight = contentRect.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            float itemY = Mathf.Abs(itemRect.anchoredPosition.y);

            float normalizedPosition = Mathf.Clamp01(itemY / (contentHeight - viewportHeight));

            scrollRect.verticalNormalizedPosition = 1f - normalizedPosition;

            Debug.Log($"[LevelPopulator] Scrolled to level {levelNumber}");
        }
    }

    // Validation
    void OnValidate()
    {
        if (levelItemPrefab != null)
        {
            var selectionItem = levelItemPrefab.GetComponent<LevelSelectionItem>();
            if (selectionItem == null)
            {
                Debug.LogWarning("[LevelPopulator] Level item prefab missing LevelSelectionItem component!");
            }
        }
    }
}