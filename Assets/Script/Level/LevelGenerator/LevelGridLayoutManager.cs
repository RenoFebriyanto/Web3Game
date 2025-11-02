using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FIXED: Layout manager untuk level selection dengan snake/zig-zag pattern
/// Properly handles UI RectTransform positioning
/// </summary>
public class LevelGridLayoutManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Number of columns per row")]
    public int columnsPerRow = 4;

    [Tooltip("Horizontal spacing between items")]
    public float horizontalSpacing = 220f;

    [Tooltip("Vertical spacing between rows")]
    public float verticalSpacing = 220f;

    [Header("Snake Pattern")]
    [Tooltip("Enable snake/zig-zag pattern (reverse direction each row)")]
    public bool useSnakePattern = true;

    [Header("Padding")]
    public float paddingLeft = 100f;
    public float paddingTop = 100f;

    [Header("Auto-Setup")]
    [Tooltip("Auto-arrange on Start")]
    public bool autoArrangeOnStart = true;

    [Tooltip("Auto-arrange when children change")]
    public bool autoArrangeOnChildChange = true;

    [Header("Item Size (for content size calculation)")]
    public float itemWidth = 200f;
    public float itemHeight = 200f;

    private int lastChildCount = 0;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[LevelGridLayout] RectTransform not found! This must be on a UI element.");
        }
    }

    void Start()
    {
        if (autoArrangeOnStart)
        {
            // ✅ Wait 1 frame untuk semua children ready
            StartCoroutine(ArrangeDelayed());
        }

        lastChildCount = transform.childCount;
    }

    System.Collections.IEnumerator ArrangeDelayed()
    {
        yield return null; // Wait 1 frame
        ArrangeLevels();
    }

    void Update()
    {
        if (autoArrangeOnChildChange && transform.childCount != lastChildCount)
        {
            ArrangeLevels();
            lastChildCount = transform.childCount;
        }
    }

    /// <summary>
    /// Arrange all child level items in snake pattern
    /// </summary>
    [ContextMenu("Arrange Levels")]
    public void ArrangeLevels()
    {
        List<RectTransform> items = new List<RectTransform>();

        // Collect all active children with RectTransform
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (!child.gameObject.activeSelf) continue;

            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null)
            {
                // ✅ CRITICAL: Setup RectTransform anchors untuk UI
                rt.anchorMin = new Vector2(0, 1); // Top-left anchor
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f); // Center pivot

                items.Add(rt);
            }
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("[LevelGridLayout] No items to arrange");
            return;
        }

        // Calculate positions
        int row = 0;
        int col = 0;

        for (int i = 0; i < items.Count; i++)
        {
            RectTransform rt = items[i];

            // Calculate row and column
            row = i / columnsPerRow;
            col = i % columnsPerRow;

            // Snake pattern: reverse direction on odd rows
            if (useSnakePattern && row % 2 == 1)
            {
                col = (columnsPerRow - 1) - col;
            }

            // ✅ FIXED: Calculate position (top-left origin, downward)
            float xPos = paddingLeft + (col * horizontalSpacing);
            float yPos = -(paddingTop + (row * verticalSpacing));

            // Set anchored position
            rt.anchoredPosition = new Vector2(xPos, yPos);

            // ✅ Set size (optional, if prefab size not set)
            if (rt.sizeDelta.x == 0 || rt.sizeDelta.y == 0)
            {
                rt.sizeDelta = new Vector2(itemWidth, itemHeight);
            }
        }

        // Update content size for ScrollRect
        UpdateContentSize(items.Count);

        Debug.Log($"[LevelGridLayout] ✓ Arranged {items.Count} levels in {row + 1} rows (snake: {useSnakePattern})");
    }

    /// <summary>
    /// Update RectTransform size to fit all items (for ScrollRect)
    /// </summary>
    void UpdateContentSize(int itemCount)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
        }

        // Calculate required rows
        int totalRows = Mathf.CeilToInt((float)itemCount / columnsPerRow);

        // ✅ FIXED: Calculate size properly
        float totalWidth = paddingLeft + ((columnsPerRow - 1) * horizontalSpacing) + itemWidth + paddingLeft;
        float totalHeight = paddingTop + ((totalRows - 1) * verticalSpacing) + itemHeight + paddingTop;

        // Set size delta
        rectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);

        Debug.Log($"[LevelGridLayout] ✓ Content size: {totalWidth}x{totalHeight} ({totalRows} rows)");
    }

    /// <summary>
    /// Get position for a specific level index
    /// </summary>
    public Vector2 GetPositionForIndex(int index)
    {
        int row = index / columnsPerRow;
        int col = index % columnsPerRow;

        if (useSnakePattern && row % 2 == 1)
        {
            col = (columnsPerRow - 1) - col;
        }

        float xPos = paddingLeft + (col * horizontalSpacing);
        float yPos = -(paddingTop + (row * verticalSpacing));

        return new Vector2(xPos, yPos);
    }

    /// <summary>
    /// Public method untuk manual refresh
    /// </summary>
    public void RefreshLayout()
    {
        ArrangeLevels();
    }

    // ✅ Validation untuk auto-setup
    void OnValidate()
    {
        // Auto-clamp values
        columnsPerRow = Mathf.Max(1, columnsPerRow);
        horizontalSpacing = Mathf.Max(50f, horizontalSpacing);
        verticalSpacing = Mathf.Max(50f, verticalSpacing);
        itemWidth = Mathf.Max(50f, itemWidth);
        itemHeight = Mathf.Max(50f, itemHeight);
    }
}