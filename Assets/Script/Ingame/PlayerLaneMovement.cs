using UnityEngine;

/// <summary>
/// ✅ UPDATED: Support DESKTOP (Keyboard) + MOBILE (Swipe)
/// Logic movement TIDAK DIUBAH - hanya tambah input method
/// </summary>
public class PlayerLaneMovement : MonoBehaviour
{
    [Header("Lane settings")]
    public float laneOffset = 2.5f;
    public int laneCount = 3;
    public float moveSpeed = 10f;

    [Header("🎮 Mobile Swipe Settings")]
    [Tooltip("Minimum swipe distance (pixels) untuk trigger lane change")]
    public float minSwipeDistance = 50f;
    
    [Tooltip("Enable debug logs untuk swipe detection")]
    public bool debugSwipe = false;

    private int currentLane = 1;
    private Vector3 targetPosition;

    // Mobile swipe detection
    private Vector2 touchStartPos;
    private bool isSwiping = false;

    void Start()
    {
        // set posisi awal di lane tengah
        targetPosition = transform.position;
        targetPosition.x = LaneToWorldX(currentLane);
        transform.position = targetPosition;
    }

    void Update()
    {
        HandleInput();

        // gerakkan rocket menuju target lane secara smooth
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetPosition.x, Time.deltaTime * moveSpeed);
        transform.position = pos;
    }

    void HandleInput()
    {
        // ========================================
        // DESKTOP INPUT (Keyboard)
        // ========================================
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLane(-1);
        
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveLane(1);

        // ========================================
        // MOBILE INPUT (Touch/Swipe)
        // ========================================
        HandleMobileInput();
    }

    /// <summary>
    /// ✅ NEW: Mobile swipe detection
    /// Works on: Android, iOS, and Unity Editor (mouse simulation)
    /// </summary>
    void HandleMobileInput()
    {
        // ========================================
        // MOBILE TOUCH (Android/iOS)
        // ========================================
        #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isSwiping = true;
                    
                    if (debugSwipe)
                        Debug.Log($"[Mobile] Touch began at: {touchStartPos}");
                    break;
                    
                case TouchPhase.Ended:
                    if (isSwiping)
                    {
                        ProcessSwipe(touch.position);
                    }
                    isSwiping = false;
                    break;
                    
                case TouchPhase.Canceled:
                    isSwiping = false;
                    if (debugSwipe)
                        Debug.Log("[Mobile] Touch canceled");
                    break;
            }
        }
        #endif

        // ========================================
        // EDITOR TESTING (Mouse = Touch Simulation)
        // ========================================
        #if UNITY_EDITOR
        // Mouse down = touch begin
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isSwiping = true;
            
            if (debugSwipe)
                Debug.Log($"[Editor] Mouse down at: {touchStartPos}");
        }
        
        // Mouse up = touch end
        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            ProcessSwipe(Input.mousePosition);
            isSwiping = false;
        }
        #endif
    }

    /// <summary>
    /// ✅ Process swipe direction and move lane
    /// </summary>
    void ProcessSwipe(Vector2 touchEndPos)
    {
        Vector2 swipeDelta = touchEndPos - touchStartPos;
        float swipeDistance = Mathf.Abs(swipeDelta.x);
        
        if (debugSwipe)
        {
            Debug.Log($"[Swipe] Start: {touchStartPos}, End: {touchEndPos}");
            Debug.Log($"[Swipe] Delta: {swipeDelta}, Distance: {swipeDistance}");
        }
        
        // Check if swipe distance is enough
        if (swipeDistance > minSwipeDistance)
        {
            // Horizontal swipe detected
            if (swipeDelta.x > 0)
            {
                // Swipe RIGHT
                MoveLane(1);
                
                if (debugSwipe)
                    Debug.Log("[Swipe] RIGHT detected → Move to right lane");
            }
            else
            {
                // Swipe LEFT
                MoveLane(-1);
                
                if (debugSwipe)
                    Debug.Log("[Swipe] LEFT detected → Move to left lane");
            }
        }
        else
        {
            if (debugSwipe)
                Debug.Log($"[Swipe] Too short: {swipeDistance}px < {minSwipeDistance}px");
        }
    }

    /// <summary>
    /// ✅ EXISTING METHOD - Tidak diubah
    /// </summary>
    void MoveLane(int direction)
    {
        int newLane = Mathf.Clamp(currentLane + direction, 0, laneCount - 1);
        
        if (newLane != currentLane)
        {
            currentLane = newLane;
            targetPosition = transform.position;
            targetPosition.x = LaneToWorldX(currentLane);
            
            // Play sound (if available)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonClick();
            }
        }
    }

    /// <summary>
    /// ✅ EXISTING METHOD - Tidak diubah
    /// </summary>
    float LaneToWorldX(int laneIndex)
    {
        // lane 0 = kiri, laneCount-1 = kanan
        float centerLane = (laneCount - 1) / 2f;
        return (laneIndex - centerLane) * laneOffset;
    }

    // ========================================
    // 🎨 DEBUG GIZMOS
    // ========================================
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw current lane
        Gizmos.color = Color.green;
        Vector3 currentPos = new Vector3(LaneToWorldX(currentLane), transform.position.y, 0);
        Gizmos.DrawWireSphere(currentPos, 0.5f);

        // Draw target position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 0.3f);
        Gizmos.DrawLine(transform.position, targetPosition);
    }

    // ========================================
    // 🧪 CONTEXT MENU (Testing)
    // ========================================
    [ContextMenu("Test: Move Left")]
    void Test_MoveLeft() => MoveLane(-1);

    [ContextMenu("Test: Move Right")]
    void Test_MoveRight() => MoveLane(1);

    [ContextMenu("Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log("=== PLAYER LANE MOVEMENT ===");
        Debug.Log($"Current Lane: {currentLane}");
        Debug.Log($"Target Position: {targetPosition}");
        Debug.Log($"Is Swiping: {isSwiping}");
        Debug.Log($"Min Swipe Distance: {minSwipeDistance}px");
        Debug.Log("===========================");
    }
}
