using UnityEngine;
using System.Collections;

/// <summary>
/// ✅ FIXED v2.0: LevelScrollArea dengan proper viewport validation
/// - Detection Only
/// - No JS notification
/// - Complete null safety
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LevelScrollArea : MonoBehaviour,
    UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IDragHandler,
    UnityEngine.EventSystems.IEndDragHandler,
    UnityEngine.EventSystems.IScrollHandler
{
    [Header("📜 Target ScrollRect")]
    [Tooltip("ScrollRect yang akan dikontrol. Kosongkan untuk auto-detect.")]
    public UnityEngine.UI.ScrollRect scrollRect;

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

    private UnityEngine.UI.Image overlayImage;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Coroutine autoScrollCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        overlayImage = GetComponent<UnityEngine.UI.Image>();
        if (overlayImage == null)
        {
            overlayImage = gameObject.AddComponent<UnityEngine.UI.Image>();
        }

        UpdateOverlayVisual();
        Log("LevelScrollArea initialized");
    }

    void Start()
    {
        // Auto-detect ScrollRect
        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = FindFirstObjectByType<UnityEngine.UI.ScrollRect>();
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
        
        // ✅ CRITICAL FIX: Validate ALL ScrollRect components
        if (scrollRect != null)
        {
            bool isValid = true;
            
            // Check content
            if (scrollRect.content == null)
            {
                LogWarning("⚠️ ScrollRect.content is NULL!");
                isValid = false;
            }
            
            // ✅ NEW: Check viewport
            if (scrollRect.viewport == null)
            {
                LogWarning("⚠️ ScrollRect.viewport is NULL!");
                isValid = false;
            }
            
            // Disable if invalid
            if (!isValid)
            {
                LogWarning("⚠️ ScrollRect incomplete - auto-scroll DISABLED");
                scrollRect = null; // Disable to prevent crashes
            }
            else
            {
                Log("✓ ScrollRect fully validated (content + viewport OK)");
            }
        }

        // Auto-detect level container
        if (levelItemsContainer == null && scrollRect != null && scrollRect.content != null)
        {
            levelItemsContainer = scrollRect.content;
            Log($"✓ Auto-detected container: {levelItemsContainer.name}");
        }

        CheckOrientation();
    }

    void OnEnable()
    {
        if (autoScrollOnEnable && scrollRect != null)
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

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            isDragging = true;
            Log("OnBeginDrag → forwarded");
            scrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (scrollRect != null && isDragging)
        {
            scrollRect.OnDrag(eventData);
        }
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (scrollRect != null && isDragging)
        {
            Log("OnEndDrag → forwarded");
            scrollRect.OnEndDrag(eventData);
            isDragging = false;
        }
    }

    public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
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

        // ✅ Validate before scroll
        if (scrollRect == null)
        {
            LogWarning("Cannot auto-scroll: scrollRect is null!");
            yield break;
        }

        if (scrollRect.viewport == null)
        {
            LogWarning("Cannot auto-scroll: viewport is null!");
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

    /// <summary>
    /// ✅ FIXED: Complete null safety untuk viewport
    /// </summary>
    float CalculateScrollPositionForLevel(int levelNumber)
    {
        // ✅ Check 1: Basic components
        if (levelItemsContainer == null || scrollRect == null)
        {
            LogWarning("Cannot calculate position: container or scrollRect is null");
            return 1f;
        }
        
        // ✅ Check 2: Content
        if (scrollRect.content == null)
        {
            LogWarning("Cannot calculate position: scrollRect.content is null");
            return 1f;
        }

        // ✅ Check 3: Viewport (CRITICAL FIX)
        if (scrollRect.viewport == null)
        {
            LogWarning("Cannot calculate position: scrollRect.viewport is null");
            return 1f;
        }

        // Find target level item
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

        // Calculate scroll position
        float contentHeight = contentRect.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height; // ✅ Safe now!
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
        // ✅ Validate before scroll
        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null) 
        {
            LogWarning("Cannot scroll: scrollRect, content, or viewport is null");
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
        
        if (scrollRect != null)
        {
            Debug.Log($"  - Content: {(scrollRect.content != null ? "OK" : "NULL")}");
            Debug.Log($"  - Viewport: {(scrollRect.viewport != null ? "OK" : "NULL")}");
        }
        
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

    [ContextMenu("🔧 Validate Setup")]
    void Context_ValidateSetup()
    {
        Debug.Log("=== VALIDATION CHECK ===");
        
        bool allOK = true;
        
        if (scrollRect == null)
        {
            Debug.LogError("❌ ScrollRect is NULL!");
            allOK = false;
        }
        else
        {
            Debug.Log("✓ ScrollRect assigned");
            
            if (scrollRect.content == null)
            {
                Debug.LogError("❌ ScrollRect.content is NULL!");
                allOK = false;
            }
            else
            {
                Debug.Log("✓ ScrollRect.content OK");
            }
            
            if (scrollRect.viewport == null)
            {
                Debug.LogError("❌ ScrollRect.viewport is NULL!");
                allOK = false;
            }
            else
            {
                Debug.Log("✓ ScrollRect.viewport OK");
            }
        }
        
        if (levelItemsContainer == null)
        {
            Debug.LogWarning("⚠️ levelItemsContainer is NULL (will auto-detect)");
        }
        else
        {
            Debug.Log($"✓ Level container: {levelItemsContainer.name}");
        }
        
        if (allOK)
        {
            Debug.Log("=== ✅ ALL CHECKS PASSED ===");
        }
        else
        {
            Debug.Log("=== ❌ SETUP INCOMPLETE ===");
        }
    }
}