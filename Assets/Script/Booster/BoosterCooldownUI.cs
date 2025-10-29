using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script untuk 1 cooldown timer UI (prefab)
/// FIXED: Shield slider tetap FULL sampai shield hancur
/// </summary>
public class BoosterCooldownUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public Slider cooldownSlider;

    [Header("Runtime (Read-Only)")]
    [SerializeField] private string boosterId;
    [SerializeField] private float maxDuration;
    [SerializeField] private float remainingTime;
    [SerializeField] private bool isShield;

    public string BoosterId => boosterId;

    public void Initialize(string id, Sprite icon, float duration)
    {
        boosterId = id;
        maxDuration = duration;
        remainingTime = duration;

        // ✅ FIXED: Check if shield
        isShield = id.ToLower() == "shield";

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }

        // ✅ FIXED: Setup slider - Shield always shows FULL
        if (cooldownSlider != null)
        {
            if (isShield)
            {
                // Shield: slider always full (max=1, value=1)
                cooldownSlider.minValue = 0f;
                cooldownSlider.maxValue = 1f;
                cooldownSlider.value = 1f; // ALWAYS FULL
            }
            else
            {
                // Normal timed booster
                cooldownSlider.minValue = 0f;
                cooldownSlider.maxValue = maxDuration;
                cooldownSlider.value = maxDuration;
            }
        }

        gameObject.SetActive(true);

        Debug.Log($"[BoosterCooldownUI] Initialized {id}, isShield={isShield}, duration={duration}, slider max={cooldownSlider?.maxValue}");
    }

    void Update()
    {
        if (BoosterManager.Instance == null) return;

        if (isShield)
        {
            UpdateShieldCooldown();
        }
        else
        {
            UpdateTimedCooldown();
        }
    }

    /// <summary>
    /// ✅ Shield: Slider tetap PENUH, destroy ketika shield inactive
    /// </summary>
    void UpdateShieldCooldown()
    {
        bool shieldActive = BoosterManager.Instance.IsActive(boosterId);

        // ✅ CRITICAL: Keep slider at MAX (always full)
        if (cooldownSlider != null)
        {
            cooldownSlider.value = cooldownSlider.maxValue; // ALWAYS FULL (1.0)
        }

        // Destroy only when shield becomes inactive
        if (!shieldActive)
        {
            Debug.Log("[BoosterCooldownUI] Shield destroyed/inactive, removing UI");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Timed booster: Slider countdown normal
    /// </summary>
    void UpdateTimedCooldown()
    {
        remainingTime = BoosterManager.Instance.GetRemainingTime(boosterId);

        if (cooldownSlider != null)
        {
            cooldownSlider.value = remainingTime;
        }

        if (remainingTime <= 0f)
        {
            Debug.Log($"[BoosterCooldownUI] {boosterId} expired, removing UI");
            Destroy(gameObject);
        }
    }
}