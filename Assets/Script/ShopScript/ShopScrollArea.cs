using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ShopScrollArea - Component untuk memperluas area scroll di Shop
/// 
/// CARA SETUP:
/// 1. Buat GameObject baru di Canvas Shop (misal: "ShopScrollArea")
/// 2. Attach script ini
/// 3. Setup RectTransform untuk cover seluruh area shop yang ingin scrollable
/// 4. Assign scrollRect reference (atau biarkan auto-detect)
/// 5. Component akan otomatis add Image transparan dan enable raycast
/// 
/// TIPS:
/// - Posisi GameObject ini DI BELAKANG shop items (Order in Hierarchy lebih tinggi)
/// - Atau set Canvas sorting order lebih rendah
/// - Pastikan tidak menutupi button yang perlu diklik
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ShopScrollArea : MonoBehaviour, 
    IBeginDragHandler, 
    IDragHandler, 
    IEndDragHandler,
    IScrollHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("📜 Target ScrollRect")]
    [Tooltip("ScrollRect yang akan dikontrol. Kosongkan untuk auto-detect.")]
    public ScrollRect scrollRect;

    [Header("🎨 Visual Settings")]
    [Tooltip("Show debug overlay? (untuk testing)")]
    public bool showDebugOverlay = false;

    [Tooltip("Debug overlay color")]
    public Color debugColor = new Color(0, 1, 0, 0.1f);

    [Header("⚙️ Behavior Settings")]
    [Tooltip("Block raycasts ke object di belakang area ini?")]
    public bool blockRaycasts = true;

    [Header("🐛 Debug")]
    public bool enableDebugLogs = false;

    private Image overlayImage;
    private RectTransform rectTransform;
    private bool isDragging = false;

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

        Log("ShopScrollArea initialized");
    }

    void Start()
    {
        // Auto-detect ScrollRect if not assigned
        if (scrollRect == null)
        {
            scrollRect = FindScrollRect();

            if (scrollRect != null)
            {
                Log($"✓ Auto-detected ScrollRect: {scrollRect.gameObject.name}");
            }
            else
            {
                LogWarning("⚠️ ScrollRect not found! Please assign manually.");
            }
        }
        else
        {
            Log($"✓ Using assigned ScrollRect: {scrollRect.gameObject.name}");
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

        // Set color based on debug mode
        overlayImage.color = showDebugOverlay ? debugColor : new Color(1, 1, 1, 0);

        // Always enable raycast target
        overlayImage.raycastTarget = true;

        Log($"Overlay updated: visible={showDebugOverlay}, raycast={overlayImage.raycastTarget}");
    }

    ScrollRect FindScrollRect()
    {
        // 1. Try parent
        var parent = GetComponentInParent<ScrollRect>();
        if (parent != null) return parent;

        // 2. Try children
        var child = GetComponentInChildren<ScrollRect>();
        if (child != null) return child;

        // 3. Search in scene
        var found = FindFirstObjectByType<ScrollRect>();
        if (found != null) return found;

        return null;
    }

    // ========================================
    // EVENT HANDLERS
    // ========================================

    public void OnPointerDown(PointerEventData eventData)
    {
        Log("OnPointerDown");
        // Tidak forward ke ScrollRect, biar tidak interfere dengan item clicks
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Log("OnPointerUp");
        isDragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            isDragging = true;
            Log("OnBeginDrag → forwarded to ScrollRect");
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
            Log("OnEndDrag → forwarded to ScrollRect");
            scrollRect.OnEndDrag(eventData);
            isDragging = false;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            Log("OnScroll → forwarded to ScrollRect");
            scrollRect.OnScroll(eventData);
        }
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[ShopScrollArea] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[ShopScrollArea] {message}");
    }

    // ========================================
    // PUBLIC API
    // ========================================

    /// <summary>
    /// Enable/disable scroll area
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (overlayImage != null)
        {
            overlayImage.raycastTarget = enabled;
        }

        Log($"Scroll area {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Set ScrollRect target
    /// </summary>
    public void SetScrollRect(ScrollRect target)
    {
        scrollRect = target;
        Log($"ScrollRect target set to: {(target != null ? target.gameObject.name : "NULL")}");
    }

    // ========================================
    // CONTEXT MENU (DEBUG)
    // ========================================

    [ContextMenu("🔍 Debug: Print Setup Info")]
    void Context_PrintSetup()
    {
        Debug.Log("=== SHOPSCROLLAREA SETUP ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"RectTransform: {(rectTransform != null ? "YES" : "NO")}");
        Debug.Log($"Image: {(overlayImage != null ? "YES" : "NO")}");
        Debug.Log($"Raycast Target: {(overlayImage != null ? overlayImage.raycastTarget.ToString() : "N/A")}");
        Debug.Log($"ScrollRect: {(scrollRect != null ? scrollRect.gameObject.name : "NULL")}");
        Debug.Log($"Debug Overlay: {showDebugOverlay}");
        Debug.Log($"Is Dragging: {isDragging}");
        Debug.Log("============================");
    }

    [ContextMenu("🎨 Toggle Debug Overlay")]
    void Context_ToggleDebugOverlay()
    {
        showDebugOverlay = !showDebugOverlay;
        UpdateOverlayVisual();
        Debug.Log($"[ShopScrollArea] Debug overlay: {showDebugOverlay}");
    }

    [ContextMenu("🔧 Fix: Setup Overlay")]
    void Context_SetupOverlay()
    {
        if (overlayImage == null)
        {
            overlayImage = GetComponent<Image>();
            if (overlayImage == null)
            {
                overlayImage = gameObject.AddComponent<Image>();
            }
        }

        UpdateOverlayVisual();
        Debug.Log("[ShopScrollArea] ✓ Overlay setup complete");
    }

    [ContextMenu("🔍 Test: Auto-Detect ScrollRect")]
    void Context_TestAutoDetect()
    {
        scrollRect = null;
        scrollRect = FindScrollRect();
        
        if (scrollRect != null)
        {
            Debug.Log($"[ShopScrollArea] ✓ Found: {scrollRect.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[ShopScrollArea] ✗ ScrollRect not found");
        }
    }
}