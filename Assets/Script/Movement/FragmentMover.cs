using UnityEngine;

/// <summary>
/// Moves fragment downward with synced speed from DifficultyManager
/// REPLACE: Assets/Script/Movement/FragmentMover.cs
/// </summary>
public class FragmentMover : MonoBehaviour
{
    [Header("Settings")]
    public float destroyY = -12f;
    public bool useDynamicSpeed = true;

    [Header("Runtime (Debug)")]
    [SerializeField] private float currentSpeed = 3f;

    void Update()
    {
        // Update speed from DifficultyManager if available
        if (useDynamicSpeed && DifficultyManager.Instance != null)
        {
            currentSpeed = DifficultyManager.Instance.CurrentSpeed;
        }

        // Move down
        transform.position += Vector3.down * currentSpeed * Time.deltaTime;

        // Destroy when off screen
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set initial speed (called by spawner)
    /// </summary>
    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
    }

    /// <summary>
    /// Disable dynamic speed (use fixed speed)
    /// </summary>
    public void SetFixedSpeed(float speed)
    {
        currentSpeed = speed;
        useDynamicSpeed = false;
    }
}