using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// LevelScrollArea - STANDALONE VERSION
/// 
/// FITUR:
/// ✅ Scroll area luas (tidak perlu cursor pas di level item)
/// ✅ Auto-scroll ke level terbaru saat panel dibuka
/// ✅ Smooth animation
/// ✅ Manual scroll tetap bisa
/// 
/// CARA SETUP:
/// 1. Buat GameObject di Level Panel: "LevelScrollArea"
/// 2. Attach script ini
/// 3. Setup RectTransform stretch (full panel)
/// 4. Assign scrollRect & levelItemsContainer di Inspector
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LevelScrollArea : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IScrollHandler
{
    [Header("📜 Target ScrollRect")]
    [Tooltip("ScrollRect yang akan dikontrol. Kosongkan untuk auto-detect.")]
    public ScrollRect scrollRect;

    [Header("🎨 Visual Settings")]
    [Tooltip("Show debug overlay? (untuk testing)")]
    public bool showDebugOverlay = false;

    [Tooltip("Debug overlay color")]
    public Color debugColor = new Color(0, 1, 0, 0.1f);

    [Header("📍 Level Auto-Scroll")]
    [Tooltip("Auto scroll ke level terbaru saat panel dibuka?")]
    public bool autoScrollOnEnable = true;

    [Tooltip("Delay sebelum auto-scroll (detik)")]
    public float autoScrollDelay = 0.3f;

    [Tooltip("Durasi smooth scroll animation (detik)")]
    public float scrollAnimationDuration = 0.5f;

    [Tooltip("Scroll curve (ease in/out)")]
    public AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("📊 Level Detection")]
    [Tooltip("Transform container yang berisi level items")]
    public Transform levelItemsContainer;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = false;

    private Image overlayImage;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Coroutine autoScrollCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Setup overlay image
        overlayImage = GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = gameObject.AddComponent<Image>();
        }

        UpdateOverlayVisual();

        Log("LevelScrollArea initialized");
    }

    void Start()
{
    // Auto-detect ScrollRect
    if (scrollRect == null)
    {
        scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = FindFirstObjectByType<ScrollRect>();
        }

        if (scrollRect != null)
        {
            Log($"✓ Auto-detected ScrollRect: {scrollRect.name}");
        }
        else
        {
            LogWarning("⚠️ ScrollRect not found!");
        }
    }
    
    // ✅ FIX: Validate ScrollRect content
    if (scrollRect != null && scrollRect.content == null)
    {
        LogWarning("⚠️ ScrollRect.content is NULL! Auto-scroll disabled.");
        scrollRect = null; // Disable to prevent errors
    }

    // Auto-detect level container
    if (levelItemsContainer == null && scrollRect != null)
    {
        levelItemsContainer = scrollRect.content;
        Log($"✓ Auto-detected container: {levelItemsContainer.name}");
    }
}

    void OnEnable()
    {
        if (autoScrollOnEnable)
        {
            if (autoScrollCoroutine != null)
            {
                StopCoroutine(autoScrollCoroutine);
            }

            autoScrollCoroutine = StartCoroutine(AutoScrollToLatestLevel());
        }
    }

    void OnDisable()
    {
        if (autoScrollCoroutine != null)
        {
            StopCoroutine(autoScrollCoroutine);
            autoScrollCoroutine = null;
        }
    }

    void OnValidate()
    {
        if (overlayImage != null)
        {
            UpdateOverlayVisual();
        }
    }

    void UpdateOverlayVisual()
    {
        if (overlayImage == null) return;

        overlayImage.color = showDebugOverlay ? debugColor : new Color(1, 1, 1, 0);
        overlayImage.raycastTarget = true;

        Log($"Overlay updated: visible={showDebugOverlay}");
    }

    // ========================================
    // SCROLL EVENT HANDLERS
    // ========================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            isDragging = true;
            Log("OnBeginDrag → forwarded");
            scrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect != null && isDragging)
        {
            scrollRect.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect != null && isDragging)
        {
            Log("OnEndDrag → forwarded");
            scrollRect.OnEndDrag(eventData);
            isDragging = false;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            Log("OnScroll → forwarded");
            scrollRect.OnScroll(eventData);
        }
    }

    // ========================================
    // AUTO-SCROLL LOGIC
    // ========================================

    IEnumerator AutoScrollToLatestLevel()
    {
        yield return new WaitForSeconds(autoScrollDelay);

        if (scrollRect == null)
        {
            LogWarning("Cannot auto-scroll: scrollRect is null!");
            yield break;
        }

        int latestLevel = GetLatestUnlockedLevel();

        if (latestLevel <= 0)
        {
            Log("No unlocked levels, scrolling to top");
            yield return StartCoroutine(SmoothScrollTo(1f));
            yield break;
        }

        Log($"🎯 Auto-scrolling to level {latestLevel}");

        float targetPosition = CalculateScrollPositionForLevel(latestLevel);
        yield return StartCoroutine(SmoothScrollTo(targetPosition));

        Log($"✓ Auto-scroll complete to level {latestLevel}");
    }

    int GetLatestUnlockedLevel()
    {
        if (LevelProgressManager.Instance != null)
        {
            int highest = LevelProgressManager.Instance.GetHighestUnlocked();
            Log($"Latest unlocked level: {highest}");
            return highest;
        }

        LogWarning("LevelProgressManager not found, defaulting to level 1");
        return 1;
    }

    float CalculateScrollPositionForLevel(int levelNumber)
{
    // ✅ FIX: Add null checks
    if (levelItemsContainer == null || scrollRect == null)
    {
        LogWarning("Cannot calculate position: container or scrollRect is null");
        return 1f;
    }
    
    if (scrollRect.content == null)
    {
        LogWarning("Cannot calculate position: scrollRect.content is null");
        return 1f;
    }

    Transform targetItem = FindLevelItemByNumber(levelNumber);

    if (targetItem == null)
    {
        LogWarning($"Level item {levelNumber} not found");
        
        // Fallback: estimate position
        int totalLevels = levelItemsContainer.childCount;
        if (totalLevels > 0)
        {
            float estimated = 1f - ((float)(levelNumber - 1) / totalLevels);
            Log($"Using estimated position: {estimated:F2}");
            return Mathf.Clamp01(estimated);
        }
        return 1f;
    }

    RectTransform contentRect = scrollRect.content;
    RectTransform targetRect = targetItem.GetComponent<RectTransform>();

    if (contentRect == null || targetRect == null)
    {
        return 1f;
    }

    float contentHeight = contentRect.rect.height;
    float viewportHeight = scrollRect.viewport.rect.height;
    float targetY = Mathf.Abs(targetRect.anchoredPosition.y);
    float scrollableHeight = contentHeight - viewportHeight;

    if (scrollableHeight <= 0)
    {
        return 1f; // Content smaller than viewport
    }

    float calculatedPosition = 1f - (targetY / scrollableHeight);
    
    // Adjust to center target in viewport
    float adjustment = (viewportHeight * 0.3f) / scrollableHeight;
    calculatedPosition = Mathf.Clamp01(calculatedPosition + adjustment);

    Log($"📐 Calculated position for level {levelNumber}: {calculatedPosition:F2}");

    return calculatedPosition;
}

    Transform FindLevelItemByNumber(int levelNumber)
    {
        if (levelItemsContainer == null) return null;

        for (int i = 0; i < levelItemsContainer.childCount; i++)
        {
            var child = levelItemsContainer.GetChild(i);
            var levelItem = child.GetComponent<LevelSelectionItem>();

            if (levelItem != null && levelItem.levelConfig != null)
            {
                if (levelItem.levelConfig.number == levelNumber)
                {
                    Log($"✓ Found level item: {child.name}");
                    return child;
                }
            }
        }

        return null;
    }

    IEnumerator SmoothScrollTo(float targetPosition)
{
    // ✅ FIX: Null check before accessing
    if (scrollRect == null || scrollRect.content == null) 
    {
        LogWarning("Cannot scroll: scrollRect or content is null");
        yield break;
    }

    float startPosition = scrollRect.verticalNormalizedPosition;
    float elapsed = 0f;

    Log($"🔄 Smooth scroll: {startPosition:F2} → {targetPosition:F2}");

    while (elapsed < scrollAnimationDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / scrollAnimationDuration;
        float curveValue = scrollCurve.Evaluate(t);

        scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, curveValue);

        yield return null;
    }

    scrollRect.verticalNormalizedPosition = targetPosition;
    Log($"✓ Scroll complete");
}

    // ========================================
    // PUBLIC API
    // ========================================

    public void ScrollToLevel(int levelNumber)
    {
        if (autoScrollCoroutine != null)
        {
            StopCoroutine(autoScrollCoroutine);
        }

        autoScrollCoroutine = StartCoroutine(ScrollToLevelCoroutine(levelNumber));
    }

    IEnumerator ScrollToLevelCoroutine(int levelNumber)
    {
        float targetPosition = CalculateScrollPositionForLevel(levelNumber);
        yield return StartCoroutine(SmoothScrollTo(targetPosition));
    }

    public void ScrollToTop()
    {
        if (autoScrollCoroutine != null)
        {
            StopCoroutine(autoScrollCoroutine);
        }

        autoScrollCoroutine = StartCoroutine(SmoothScrollTo(1f));
    }

    public void ScrollToBottom()
    {
        if (autoScrollCoroutine != null)
        {
            StopCoroutine(autoScrollCoroutine);
        }

        autoScrollCoroutine = StartCoroutine(SmoothScrollTo(0f));
    }

    public void RefreshAutoScroll()
    {
        if (autoScrollCoroutine != null)
        {
            StopCoroutine(autoScrollCoroutine);
        }

        autoScrollCoroutine = StartCoroutine(AutoScrollToLatestLevel());
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[LevelScrollArea] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[LevelScrollArea] {message}");
    }

    // ========================================
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("🎯 Test: Auto Scroll to Latest")]
    void Context_TestAutoScroll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode");
            return;
        }

        RefreshAutoScroll();
    }

    [ContextMenu("🔝 Test: Scroll to Top")]
    void Context_TestScrollTop()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode");
            return;
        }

        ScrollToTop();
    }

    [ContextMenu("🔽 Test: Scroll to Bottom")]
    void Context_TestScrollBottom()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode");
            return;
        }

        ScrollToBottom();
    }

    [ContextMenu("📊 Debug: Print Info")]
    void Context_PrintInfo()
    {
        Debug.Log("=== LEVEL SCROLL AREA INFO ===");
        Debug.Log($"ScrollRect: {(scrollRect != null ? scrollRect.name : "NULL")}");
        Debug.Log($"Container: {(levelItemsContainer != null ? levelItemsContainer.name : "NULL")}");
        
        if (LevelProgressManager.Instance != null)
        {
            Debug.Log($"Latest Level: {LevelProgressManager.Instance.GetHighestUnlocked()}");
        }

        if (levelItemsContainer != null)
        {
            Debug.Log($"Total Items: {levelItemsContainer.childCount}");
        }

        Debug.Log($"Auto Scroll: {autoScrollOnEnable}");
        Debug.Log("==============================");
    }
}