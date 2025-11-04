using System.Collections;
using UnityEngine;

/// <summary>
/// Manager untuk handle semua booster active effects di gameplay
/// UPDATED: Fix SpeedBoost (obstacles speed up) & Magnet (pull all collectibles)
/// </summary>
public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("Active Booster States")]
    public bool coin2xActive = false;
    public bool magnetActive = false;
    public bool shieldActive = false;
    public bool speedBoostActive = false;
    public bool timeFreezeActive = false;

    [Header("Booster Settings")]
    [Tooltip("Duration untuk Coin2x (menit)")]
    public float coin2xDuration = 10f;

    [Tooltip("Duration untuk Magnet (detik)")]
    public float magnetDuration = 30f;

    [Tooltip("Jarak magnet menarik collectibles")]
    public float magnetRange = 5f;

    [Tooltip("Speed magnet menarik collectibles")]
    public float magnetPullSpeed = 10f;

    [Tooltip("Duration untuk SpeedBoost (detik)")]
    public float speedBoostDuration = 10f;

    [Tooltip("Speed multiplier untuk obstacles (planet/coin/fragment/star)")]
    public float speedBoostMultiplier = 2f;

    [Tooltip("Duration untuk TimeFreeze (detik)")]
    public float timeFreezeDuration = 5f;

    // Runtime timers
    private float coin2xTimer = 0f;
    private float magnetTimer = 0f;
    private float speedBoostTimer = 0f;
    private float timeFreezeTimer = 0f;

    // References
    private Transform playerTransform;
    private PlayerHealth playerHealth;

    // Shield state
    private GameObject shieldVisual;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Find references
        var playerMovement = FindFirstObjectByType<PlayerLaneMovement>();
        if (playerMovement != null)
            playerTransform = playerMovement.transform;

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerTransform == null)
            Debug.LogWarning("[BoosterManager] Player not found!");

        if (playerHealth == null)
            Debug.LogWarning("[BoosterManager] PlayerHealth not found!");

        // Subscribe to player death untuk reset shield
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath.AddListener(OnPlayerDied);
        }
    }

    void Update()
    {
        // Update timers
        UpdateCoin2xTimer();
        UpdateMagnetTimer();
        UpdateSpeedBoostTimer();
        UpdateTimeFreezeTimer();

        // Magnet logic - pull collectibles
        if (magnetActive && playerTransform != null)
        {
            PullNearbyCollectibles();
        }
    }

    #region COIN 2X BOOSTER

    public bool ActivateCoin2x()
    {
        if (coin2xActive)
        {
            Debug.Log("[BoosterManager] Coin2x already active!");
            return false;
        }

        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("coin2x"))
        {
            Debug.Log("[BoosterManager] No Coin2x booster available!");
            return false;
        }

        coin2xActive = true;
        coin2xTimer = coin2xDuration * 60f;

        Debug.Log($"[BoosterManager] Coin2x activated for {coin2xDuration} minutes!");
        return true;
    }

    void UpdateCoin2xTimer()
    {
        if (!coin2xActive) return;

        coin2xTimer -= Time.deltaTime;
        if (coin2xTimer <= 0f)
        {
            coin2xActive = false;
            coin2xTimer = 0f;
            Debug.Log("[BoosterManager] Coin2x expired!");
        }
    }

    public long ApplyCoinMultiplier(long baseAmount)
    {
        return coin2xActive ? baseAmount * 2 : baseAmount;
    }

    #endregion

    #region MAGNET BOOSTER

    public bool ActivateMagnet()
    {
        if (magnetActive)
        {
            Debug.Log("[BoosterManager] Magnet already active!");
            return false;
        }

        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("magnet"))
        {
            Debug.Log("[BoosterManager] No Magnet booster available!");
            return false;
        }

        magnetActive = true;
        magnetTimer = magnetDuration;

        Debug.Log($"[BoosterManager] Magnet activated for {magnetDuration} seconds!");
        return true;
    }

    void UpdateMagnetTimer()
    {
        if (!magnetActive) return;

        magnetTimer -= Time.deltaTime;
        if (magnetTimer <= 0f)
        {
            magnetActive = false;
            magnetTimer = 0f;
            Debug.Log("[BoosterManager] Magnet expired!");
        }
    }

    void PullNearbyCollectibles()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;

        // Find all collectibles dalam range (Coin, Fragment, Star)
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, magnetRange);

        foreach (var hit in hits)
        {
            // Check if it's a collectible
            bool isCollectible = false;

            // Coin
            if (hit.CompareTag("Coin") || hit.GetComponent<CoinPickup>() != null)
                isCollectible = true;

            // Fragment
            if (hit.GetComponent<FragmentCollectible>() != null)
                isCollectible = true;

            // Star
            if (hit.GetComponent<StarCollectible>() != null)
                isCollectible = true;

            if (isCollectible)
            {
                // Pull ke arah player
                Vector3 direction = (playerPos - hit.transform.position).normalized;
                hit.transform.position += direction * magnetPullSpeed * Time.deltaTime;
            }
        }
    }

    #endregion

    #region SHIELD BOOSTER

    public bool ActivateShield()
    {
        if (shieldActive)
        {
            Debug.Log("[BoosterManager] Shield already active!");
            return false;
        }

        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("shield"))
        {
            Debug.Log("[BoosterManager] No Shield booster available!");
            return false;
        }

        shieldActive = true;
        ShowShieldVisual();

        Debug.Log("[BoosterManager] Shield activated!");
        return true;
    }

    void ShowShieldVisual()
    {
        if (playerTransform == null) return;

        // Find existing shield visual di player
        shieldVisual = playerTransform.Find("Shield")?.gameObject;

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[BoosterManager] Shield visual not found! Add 'Shield' GameObject as child of player.");
        }
    }

    void HideShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
    }

    public bool TryAbsorbHit()
    {
        if (!shieldActive) return false;

        Debug.Log("[BoosterManager] Shield absorbed hit!");

        // Shield hancur setelah 1 hit
        shieldActive = false;
        HideShieldVisual();

        return true; // Hit absorbed, planet hancur
    }

    void OnPlayerDied()
    {
        shieldActive = false;
        HideShieldVisual();
        Debug.Log("[BoosterManager] Shield reset (player died)");
    }

    #endregion

    #region SPEED BOOST

    public bool ActivateSpeedBoost()
    {
        if (speedBoostActive)
        {
            Debug.Log("[BoosterManager] SpeedBoost already active!");
            return false;
        }

        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("speedboost"))
        {
            Debug.Log("[BoosterManager] No SpeedBoost available!");
            return false;
        }

        speedBoostActive = true;
        speedBoostTimer = speedBoostDuration;

        Debug.Log($"[BoosterManager] SpeedBoost activated for {speedBoostDuration} seconds!");
        return true;
    }

    void UpdateSpeedBoostTimer()
    {
        if (!speedBoostActive) return;

        speedBoostTimer -= Time.deltaTime;
        if (speedBoostTimer <= 0f)
        {
            speedBoostActive = false;
            speedBoostTimer = 0f;
            Debug.Log("[BoosterManager] SpeedBoost expired!");
        }
    }

    /// <summary>
    /// Get speed multiplier for obstacles (planet/coin/fragment/star)
    /// Call dari mover scripts untuk apply speed boost
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return speedBoostActive ? speedBoostMultiplier : 1f;
    }

    /// <summary>
    /// Check apakah planet harus hancur saat kena player (speedboost mode)
    /// </summary>
    public bool ShouldDestroyPlanet()
    {
        return speedBoostActive;
    }

    #endregion

    #region TIME FREEZE

    public bool ActivateTimeFreeze()
    {
        if (timeFreezeActive)
        {
            Debug.Log("[BoosterManager] TimeFreeze already active!");
            return false;
        }

        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("timefreeze"))
        {
            Debug.Log("[BoosterManager] No TimeFreeze available!");
            return false;
        }

        timeFreezeActive = true;
        timeFreezeTimer = timeFreezeDuration;

        Debug.Log($"[BoosterManager] TimeFreeze activated for {timeFreezeDuration} seconds!");
        return true;
    }

    void UpdateTimeFreezeTimer()
    {
        if (!timeFreezeActive) return;

        timeFreezeTimer -= Time.deltaTime;
        if (timeFreezeTimer <= 0f)
        {
            timeFreezeActive = false;
            timeFreezeTimer = 0f;
            Debug.Log("[BoosterManager] TimeFreeze expired!");
        }
    }

    public bool CanSpawn()
    {
        return !timeFreezeActive;
    }

    #endregion

    #region UTILITY

    public float GetRemainingTime(string boosterType)
    {
        switch (boosterType.ToLower())
        {
            case "coin2x": return coin2xActive ? coin2xTimer : 0f;
            case "magnet": return magnetActive ? magnetTimer : 0f;
            case "speedboost":
            case "rocketboost": return speedBoostActive ? speedBoostTimer : 0f;
            case "timefreeze": return timeFreezeActive ? timeFreezeTimer : 0f;
            case "shield": return shieldActive ? 1f : 0f; // ✅ FIXED: Return 1 if active (for slider full display)
            default: return 0f;
        }
    }

    public float GetMaxDuration(string boosterType)
    {
        switch (boosterType.ToLower())
        {
            case "coin2x": return coin2xDuration * 60f;
            case "magnet": return magnetDuration;
            case "speedboost":
            case "rocketboost": return speedBoostDuration;
            case "timefreeze": return timeFreezeDuration;
            case "shield": return 1f; // ✅ FIXED: Return 1 (shield has no timer, always full slider)
            default: return 0f;
        }
    }

    public bool IsActive(string boosterType)
    {
        switch (boosterType.ToLower())
        {
            case "coin2x": return coin2xActive;
            case "magnet": return magnetActive;
            case "shield": return shieldActive;
            case "speedboost":
            case "rocketboost": return speedBoostActive;
            case "timefreeze": return timeFreezeActive;
            default: return false;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !magnetActive || playerTransform == null) return;

        // Draw magnet range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, magnetRange);
    }

    #endregion
}