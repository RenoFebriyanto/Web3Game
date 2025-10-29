using UnityEngine;

public class FragmentMover : MonoBehaviour
{
    [Header("Settings")]
    public float destroyY = -12f;
    public bool useDynamicSpeed = true;

    [Header("Runtime (Debug)")]
    [SerializeField] private float currentSpeed = 3f;
    [SerializeField] private float baseSpeed = 3f;

    void Update()
    {
        // Get base speed from DifficultyManager
        if (useDynamicSpeed && DifficultyManager.Instance != null)
        {
            baseSpeed = DifficultyManager.Instance.CurrentSpeed;
        }

        // Apply SpeedBoost multiplier
        float multiplier = BoosterManager.Instance != null ? BoosterManager.Instance.GetSpeedMultiplier() : 1f;
        currentSpeed = baseSpeed * multiplier;

        // Move down
        transform.position += Vector3.down * currentSpeed * Time.deltaTime;

        // Destroy when off screen
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    public void SetSpeed(float speed)
    {
        baseSpeed = speed;
        currentSpeed = speed;
    }

    public void SetFixedSpeed(float speed)
    {
        baseSpeed = speed;
        currentSpeed = speed;
        useDynamicSpeed = false;
    }
}