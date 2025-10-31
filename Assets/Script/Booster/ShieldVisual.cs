using UnityEngine;

/// <summary>
/// UPDATED: Shield visual dengan activate sound
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

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (shieldRenderer == null) return;

        float pulse = Mathf.PingPong(Time.time * pulseSpeed, pulseAmount);
        float alpha = baseAlpha + pulse;

        Color color = shieldRenderer.color;
        color.a = alpha;
        shieldRenderer.color = color;
    }

    void OnEnable()
    {
        Debug.Log("[ShieldVisual] Shield visual enabled");

        // ✅ NEW: Play shield activate sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShieldActivate();
        }
    }

    void OnDisable()
    {
        Debug.Log("[ShieldVisual] Shield visual disabled");
    }
}