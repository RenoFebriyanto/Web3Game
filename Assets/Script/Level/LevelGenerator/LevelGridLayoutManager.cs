using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Layout manager untuk level selection dengan snake/zig-zag pattern
/// Attach ke parent GameObject dari level items (e.g., ContentLevel)
/// Automatically positions level items in snake pattern
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

    private int lastChildCount = 0;

    void Start()
    {
        if (autoArrangeOnStart)
        {
            ArrangeLevels();
        }

        lastChildCount = transform.childCount;
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

            // Calculate position
            float xPos = paddingLeft + (col * horizontalSpacing);
            float yPos = -(paddingTop + (row * verticalSpacing));

            // Set position
            rt.anchoredPosition = new Vector2(xPos, yPos);
        }

        // Update content size for ScrollRect
        UpdateContentSize(items.Count);

        Debug.Log($"[LevelGridLayout] Arranged {items.Count} levels in {row + 1} rows (snake: {useSnakePattern})");
    }

    /// <summary>
    /// Update RectTransform size to fit all items (for ScrollRect)
    /// </summary>
    void UpdateContentSize(int itemCount)
    {
        RectTransform contentRect = GetComponent<RectTransform>();
        if (contentRect == null) return;

        // Calculate required rows
        int totalRows = Mathf.CeilToInt((float)itemCount / columnsPerRow);

        // Calculate size
        float totalWidth = paddingLeft * 2 + ((columnsPerRow - 1) * horizontalSpacing) + 200f; // +200 for item width
        float totalHeight = paddingTop * 2 + ((totalRows - 1) * verticalSpacing) + 200f; // +200 for item height

        contentRect.sizeDelta = new Vector2(totalWidth, totalHeight);

        Debug.Log($"[LevelGridLayout] Updated content size: {totalWidth}x{totalHeight} ({totalRows} rows)");
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

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw grid lines
        Gizmos.color = Color.cyan;

        int totalItems = transform.childCount;
        int totalRows = Mathf.CeilToInt((float)totalItems / columnsPerRow);

        for (int row = 0; row < totalRows; row++)
        {
            for (int col = 0; col < columnsPerRow; col++)
            {
                Vector2 pos = GetPositionForIndex(row * columnsPerRow + col);
                Vector3 worldPos = transform.TransformPoint(new Vector3(pos.x, pos.y, 0));
                Gizmos.DrawWireSphere(worldPos, 20f);
            }
        }
    }
}