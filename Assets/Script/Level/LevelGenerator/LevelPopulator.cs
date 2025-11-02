using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FIXED: Auto-populate level selection UI from LevelDatabase
/// Properly initializes RectTransform for UI elements
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
            // ✅ CRITICAL: Wait frames untuk UI setup
            StartCoroutine(PopulateLevelsDelayed());
        }
    }

    IEnumerator PopulateLevelsDelayed()
    {
        yield return null; // Wait 1 frame for UI setup
        PopulateLevels();
        yield return new WaitForEndOfFrame(); // Wait for layout

        // ✅ Force refresh layout after populate
        if (layoutManager != null)
        {
            layoutManager.RefreshLayout();
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

        // ✅ Spawn level items dan AUTO-ASSIGN LevelConfig
        int successCount = 0;
        foreach (var levelConfig in levelDatabase.levels)
        {
            if (levelConfig == null)
            {
                Debug.LogWarning("[LevelPopulator] Null LevelConfig found in database, skipping...");
                continue;
            }

            // ✅ Instantiate prefab (make sure parent is set)
            GameObject levelItem = Instantiate(levelItemPrefab, transform);
            levelItem.name = $"Level_{levelConfig.number}";

            // ✅ Setup RectTransform properly
            RectTransform rt = levelItem.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Set anchors to top-left
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);

                // Initial position (will be fixed by layout manager)
                rt.anchoredPosition = Vector2.zero;

                // Ensure scale is correct
                rt.localScale = Vector3.one;
            }

            // ✅ CRITICAL: Setup LevelSelectionItem dengan LevelConfig
            LevelSelectionItem selectionItem = levelItem.GetComponent<LevelSelectionItem>();
            if (selectionItem != null)
            {
                selectionItem.levelConfig = levelConfig; // ✅ AUTO-ASSIGN
                selectionItem.Refresh(); // ✅ Refresh UI
                successCount++;
            }
            else
            {
                Debug.LogError($"[LevelPopulator] Level item prefab missing LevelSelectionItem component!");
                Destroy(levelItem);
                continue;
            }

            spawnedItems.Add(levelItem);
        }

        Debug.Log($"[LevelPopulator] ✓ Successfully spawned {successCount} level items");
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
                if (Application.isPlaying)
                    Destroy(item);
                else
                    DestroyImmediate(item);
            }
        }
        spawnedItems.Clear();

        // ✅ Also destroy any remaining children
        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            toDestroy.Add(transform.GetChild(i).gameObject);
        }

        foreach (var obj in toDestroy)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        Debug.Log("[LevelPopulator] ✓ Cleared all level items");
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

        Debug.Log("[LevelPopulator] ✓ Refreshed all level items");
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

    // Validation
    void OnValidate()
    {
        if (levelItemPrefab != null)
        {
            var selectionItem = levelItemPrefab.GetComponent<LevelSelectionItem>();
            if (selectionItem == null)
            {
                Debug.LogWarning("[LevelPopulator] ⚠️ Level item prefab missing LevelSelectionItem component!");
            }
        }

        if (levelDatabase == null)
        {
            Debug.LogWarning("[LevelPopulator] ⚠️ LevelDatabase not assigned!");
        }
    }
}