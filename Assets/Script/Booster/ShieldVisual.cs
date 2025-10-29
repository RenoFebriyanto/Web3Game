using UnityEngine;

/// <summary>
/// Script untuk visual shield (lingkaran biru di player)
/// Attach ke GameObject "Shield" sebagai child dari player
/// Structure: Player -> Shield (with SpriteRenderer of blue circle)
/// </summary>
public class ShieldVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    public SpriteRenderer shieldRenderer;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.3f;

    private float baseAlpha = 0.6f;

    void Start()
    {
        if (shieldRenderer == null)
        {
            shieldRenderer = GetComponent<SpriteRenderer>();
        }

        if (shieldRenderer == null)
        {
            Debug.LogWarning("[ShieldVisual] No SpriteRenderer found!");
        }

        // Disable di start (akan di-enable dari BoosterManager)
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (shieldRenderer == null) return;

        // Pulse effect (breathing animation)
        float pulse = Mathf.PingPong(Time.time * pulseSpeed, pulseAmount);
        float alpha = baseAlpha + pulse;

        Color color = shieldRenderer.color;
        color.a = alpha;
        shieldRenderer.color = color;
    }

    void OnEnable()
    {
        Debug.Log("[ShieldVisual] Shield visual enabled");
    }

    void OnDisable()
    {
        Debug.Log("[ShieldVisual] Shield visual disabled");
    }
}