using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Layout manager untuk level map dengan path berkelok-kelok (seperti Candy Crush)
/// Replace LevelGridLayoutManager dengan script ini untuk layout path-based
/// </summary>
public class LevelPathLayoutManager : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Panjang horizontal per segment")]
    public float segmentLength = 300f;

    [Tooltip("Tinggi vertikal per row")]
    public float rowHeight = 250f;

    [Tooltip("Jumlah level per row (akan zigzag)")]
    public int levelsPerRow = 5;

    [Header("Wave Pattern")]
    [Tooltip("Amplitude horizontal wave (0 = lurus)")]
    public float waveAmplitude = 80f;

    [Tooltip("Frequency wave pattern")]
    public float waveFrequency = 0.3f;

    [Header("Randomization")]
    [Tooltip("Random offset per level (untuk variasi)")]
    public float randomOffsetRange = 30f;

    [Tooltip("Random seed (0 = random, >0 = consistent)")]
    public int randomSeed = 12345;

    [Header("Padding")]
    public float paddingLeft = 150f;
    public float paddingTop = 150f;
    public float paddingRight = 150f;
    public float paddingBottom = 150f;

    [Header("Auto-Setup")]
    public bool autoArrangeOnStart = true;
    public bool autoArrangeOnChildChange = true;

    [Header("Item Size")]
    public float itemWidth = 180f;
    public float itemHeight = 180f;

    private int lastChildCount = 0;
    private RectTransform rectTransform;
    private System.Random rng;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rng = randomSeed > 0 ? new System.Random(randomSeed) : new System.Random();
    }

    void Start()
    {
        if (autoArrangeOnStart)
        {
            StartCoroutine(ArrangeDelayed());
        }
        lastChildCount = transform.childCount;
    }

    System.Collections.IEnumerator ArrangeDelayed()
    {
        yield return null;
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

    [ContextMenu("Arrange Levels")]
    public void ArrangeLevels()
    {
        List<RectTransform> items = new List<RectTransform>();

        // Collect children
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                items.Add(rt);
            }
        }

        if (items.Count == 0) return;

        // Reset random seed untuk consistency
        if (randomSeed > 0)
        {
            rng = new System.Random(randomSeed);
        }

        // Calculate positions dengan path algorithm
        for (int i = 0; i < items.Count; i++)
        {
            Vector2 pos = CalculatePathPosition(i);
            items[i].anchoredPosition = pos;

            if (items[i].sizeDelta.x == 0 || items[i].sizeDelta.y == 0)
            {
                items[i].sizeDelta = new Vector2(itemWidth, itemHeight);
            }
        }

        UpdateContentSize(items.Count);
        Debug.Log($"[LevelPathLayout] ✓ Arranged {items.Count} levels in path pattern");
    }

    /// <summary>
    /// Calculate position untuk level index dengan path algorithm
    /// </summary>
    Vector2 CalculatePathPosition(int index)
    {
        // Calculate row dan position dalam row
        int row = index / levelsPerRow;
        int posInRow = index % levelsPerRow;

        // Base position
        float baseX = paddingLeft;
        float baseY = -(paddingTop + (row * rowHeight));

        // Horizontal position dengan zigzag
        bool isOddRow = row % 2 == 1;
        float xOffset;

        if (isOddRow)
        {
            // Odd row: right to left
            xOffset = (levelsPerRow - 1 - posInRow) * segmentLength;
        }
        else
        {
            // Even row: left to right
            xOffset = posInRow * segmentLength;
        }

        // Add wave pattern
        float wave = Mathf.Sin(index * waveFrequency) * waveAmplitude;

        // Add random offset
        float randomX = ((float)rng.NextDouble() - 0.5f) * 2f * randomOffsetRange;
        float randomY = ((float)rng.NextDouble() - 0.5f) * 2f * randomOffsetRange * 0.5f;

        float finalX = baseX + xOffset + wave + randomX;
        float finalY = baseY + randomY;

        return new Vector2(finalX, finalY);
    }

    void UpdateContentSize(int itemCount)
    {
        if (rectTransform == null) return;

        int totalRows = Mathf.CeilToInt((float)itemCount / levelsPerRow);

        float maxWidth = paddingLeft + ((levelsPerRow - 1) * segmentLength) + waveAmplitude * 2 + randomOffsetRange * 2 + itemWidth + paddingRight;
        float totalHeight = paddingTop + ((totalRows - 1) * rowHeight) + itemHeight + paddingBottom + randomOffsetRange;

        rectTransform.sizeDelta = new Vector2(maxWidth, totalHeight);
    }

    public void RefreshLayout()
    {
        ArrangeLevels();
    }

    public Vector2 GetPositionForIndex(int index)
    {
        return CalculatePathPosition(index);
    }
}