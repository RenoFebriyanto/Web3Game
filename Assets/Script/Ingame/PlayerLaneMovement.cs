using UnityEngine;

/// <summary>
/// Support: Desktop (Keyboard) + Mobile (Swipe + UI Button)
/// MoveLeft() dan MoveRight() dipanggil oleh Button OnClick di Unity UI
/// </summary>
public class PlayerLaneMovement : MonoBehaviour
{
    [Header("Lane settings")]
    public float laneOffset = 2.5f;
    public int laneCount = 3;
    public float moveSpeed = 10f;

    [Header("Mobile Swipe Settings")]
    public float minSwipeDistance = 50f;
    public bool debugSwipe = false;

    private int currentLane = 1;
    private Vector3 targetPosition;
    private Vector2 touchStartPos;
    private bool isSwiping = false;

    void Start()
    {
        targetPosition = transform.position;
        targetPosition.x = LaneToWorldX(currentLane);
        transform.position = targetPosition;
    }

    void Update()
    {
        HandleDesktopInput();
        HandleSwipeInput();

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetPosition.x, Time.deltaTime * moveSpeed);
        transform.position = pos;
    }

    // ========================================
    // DESKTOP — Keyboard
    // ========================================
    void HandleDesktopInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLane(-1);

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveLane(1);
    }

    // ========================================
    // MOBILE — Swipe (WebGL + Android + iOS)
    // ========================================
    void HandleSwipeInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isSwiping = true;
                    break;
                case TouchPhase.Ended:
                    if (isSwiping) ProcessSwipe(touch.position);
                    isSwiping = false;
                    break;
                case TouchPhase.Canceled:
                    isSwiping = false;
                    break;
            }
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) { touchStartPos = Input.mousePosition; isSwiping = true; }
        if (Input.GetMouseButtonUp(0)) { if (isSwiping) ProcessSwipe(Input.mousePosition); isSwiping = false; }
#endif
    }

    void ProcessSwipe(Vector2 endPos)
    {
        Vector2 delta = endPos - touchStartPos;
        if (Mathf.Abs(delta.x) < minSwipeDistance) return;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            MoveLane(delta.x > 0 ? 1 : -1);
    }

    // ========================================
    // ✅ DIPANGGIL OLEH BUTTON OnClick() DI UNITY UI
    // ========================================
    public void MoveLeft()
    {
        MoveLane(-1);
        if (debugSwipe) Debug.Log("[PlayerLaneMovement] MoveLeft via Button");
    }

    public void MoveRight()
    {
        MoveLane(1);
        if (debugSwipe) Debug.Log("[PlayerLaneMovement] MoveRight via Button");
    }

    // ========================================
    // CORE LOGIC
    // ========================================
    void MoveLane(int direction)
    {
        int newLane = currentLane + direction;
        if (newLane < 0 || newLane >= laneCount) return;
        currentLane = newLane;
        targetPosition.x = LaneToWorldX(currentLane);
    }

    float LaneToWorldX(int lane)
    {
        int centerLane = laneCount / 2;
        return (lane - centerLane) * laneOffset;
    }
}