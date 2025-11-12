using System.Collections;
using UnityEngine;

/// <summary>
/// ✅ NEW: Dynamic Obstacle Mover - Subway Surf Style
/// Handle obstacle yang bergerak horizontal kearah player
/// 
/// USAGE:
/// - Attach ke obstacle prefab yang ingin bergerak
/// - Set targetLane dan speed via Initialize()
/// - Script akan handle smooth horizontal movement
/// </summary>
public class DynamicObstacleMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public float horizontalSpeed = 5f;
    public float verticalSpeed = 3f;
    
    [Header("Target")]
    public int targetLane = 1;
    public float startDelay = 0.5f;
    
    [Header("Runtime Status")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private float targetX = 0f;
    [SerializeField] private Vector3 startPosition;
    
    private bool initialized = false;
    
    /// <summary>
    /// Initialize dynamic obstacle
    /// </summary>
    public void Initialize(int lane, float horizontalSpd, float verticalSpd, float delay)
    {
        targetLane = lane;
        horizontalSpeed = horizontalSpd;
        verticalSpeed = verticalSpd;
        startDelay = delay;
        
        startPosition = transform.position;
        
        // Calculate target X
        if (LanesManager.Instance != null)
        {
            targetX = LanesManager.Instance.LaneToWorldX(targetLane);
        }
        else
        {
            // Fallback calculation
            float laneOffset = 2.5f;
            float centerLane = 1f;
            targetX = (targetLane - centerLane) * laneOffset;
        }
        
        initialized = true;
        
        // Start movement after delay
        StartCoroutine(StartMovementAfterDelay());
    }
    
    IEnumerator StartMovementAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        isMoving = true;
    }
    
    void Update()
    {
        if (!initialized || !isMoving) return;
        
        // Move down (vertical)
        transform.position += Vector3.down * verticalSpeed * Time.deltaTime;
        
        // Move toward target lane (horizontal)
        if (Mathf.Abs(transform.position.x - targetX) > 0.1f)
        {
            float direction = Mathf.Sign(targetX - transform.position.x);
            float step = horizontalSpeed * Time.deltaTime;
            
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, step);
            transform.position = pos;
        }
        
        // Destroy when off-screen
        if (transform.position.y < -12f)
        {
            Destroy(gameObject);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!initialized || !isMoving) return;
        
        // Draw movement path
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(targetX, transform.position.y - 5f, 0f));
        
        // Draw target point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(targetX, transform.position.y, 0f), 0.3f);
    }
}