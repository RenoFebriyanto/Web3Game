using System.Collections;
using UnityEngine;

/// <summary>
/// ✅ FIXED: Dynamic Obstacle Mover - ONLY active when initialized
/// CRITICAL: Hanya obstacle dengan isDynamicObstacle=true yang boleh pakai component ini
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
    
    void Start()
    {
        // ✅ CRITICAL: Jika tidak di-initialize dalam 0.5 detik, destroy component ini
        if (!initialized)
        {
            StartCoroutine(DestroyIfNotInitialized());
        }
    }
    
    IEnumerator DestroyIfNotInitialized()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (!initialized)
        {
            Debug.LogWarning($"[DynamicObstacleMover] {gameObject.name}: NOT INITIALIZED - Destroying component!");
            Destroy(this);
        }
    }
    
    /// <summary>
    /// Initialize dynamic obstacle - MUST be called by spawner
    /// </summary>
    public void Initialize(int lane, float horizontalSpd, float verticalSpd, float delay)
    {
        if (initialized)
        {
            Debug.LogWarning($"[DynamicObstacleMover] {gameObject.name}: Already initialized!");
            return;
        }
        
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
        
        Debug.Log($"[DynamicObstacleMover] ✓ {gameObject.name} initialized → Target Lane {targetLane} (X: {targetX})");
        
        // Start movement after delay
        StartCoroutine(StartMovementAfterDelay());
    }
    
    IEnumerator StartMovementAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        isMoving = true;
        
        Debug.Log($"[DynamicObstacleMover] ▶️ {gameObject.name} started moving!");
    }
    
    void Update()
    {
        // ✅ CRITICAL: Only update if initialized AND moving
        if (!initialized || !isMoving) return;
        
        // Move down (vertical) - handled by PlanetMover component
        // No need to move down here, PlanetMover handles it
        
        // Move toward target lane (horizontal)
        if (Mathf.Abs(transform.position.x - targetX) > 0.1f)
        {
            float direction = Mathf.Sign(targetX - transform.position.x);
            float step = horizontalSpeed * Time.deltaTime;
            
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, step);
            transform.position = pos;
        }
        
        // Destroy when off-screen (safety - PlanetMover also handles this)
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
        
        // Draw status label
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1f,
            $"DYNAMIC → Lane {targetLane}",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow }, fontSize = 10 }
        );
#endif
    }
    
    [ContextMenu("Debug: Print Status")]
    void Debug_PrintStatus()
    {
        Debug.Log($"=== DYNAMIC OBSTACLE MOVER: {gameObject.name} ===");
        Debug.Log($"Initialized: {initialized}");
        Debug.Log($"Is Moving: {isMoving}");
        Debug.Log($"Target Lane: {targetLane}");
        Debug.Log($"Target X: {targetX}");
        Debug.Log($"Horizontal Speed: {horizontalSpeed}");
        Debug.Log($"Vertical Speed: {verticalSpeed}");
        Debug.Log($"Current Position: {transform.position}");
        Debug.Log("===========================================");
    }
}