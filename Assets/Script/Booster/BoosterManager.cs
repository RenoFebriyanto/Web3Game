using System.Collections;
using UnityEngine;

/// <summary>
/// Manager untuk handle semua booster active effects di gameplay
/// Letakkan di: Assets/Script/Booster/BoosterManager.cs
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
    public float coin2xDuration = 10f; // 10 menit dalam detik akan di convert

    [Tooltip("Duration untuk Magnet (detik)")]
    public float magnetDuration = 30f;

    [Tooltip("Jarak magnet menarik coin")]
    public float magnetRange = 5f;

    [Tooltip("Speed magnet menarik coin")]
    public float magnetPullSpeed = 10f;

    [Tooltip("Duration untuk SpeedBoost (detik)")]
    public float speedBoostDuration = 10f;

    [Tooltip("Speed multiplier untuk SpeedBoost")]
    public float speedBoostMultiplier = 2f;

    [Tooltip("Duration untuk TimeFreeze (detik)")]
    public float timeFreezeDuration = 5f;

    // Runtime timers
    private float coin2xTimer = 0f;
    private float magnetTimer = 0f;
    private float speedBoostTimer = 0f;
    private float timeFreezeTimer = 0f;

    // References
    private PlayerLaneMovement playerMovement;
    private FixedGameplaySpawner gameplaySpawner;
    private PlayerHealth playerHealth;

    // Shield state
    private int shieldHitCount = 0;
    private bool shieldUsedThisLife = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Find references
        playerMovement = FindFirstObjectByType<PlayerLaneMovement>();
        gameplaySpawner = FindFirstObjectByType<FixedGameplaySpawner>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerMovement == null)
            Debug.LogWarning("[BoosterManager] PlayerLaneMovement not found!");

        if (gameplaySpawner == null)
            Debug.LogWarning("[BoosterManager] FixedGameplaySpawner not found!");

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
        // Update timers untuk booster yang aktif
        UpdateCoin2xTimer();
        UpdateMagnetTimer();
        UpdateSpeedBoostTimer();
        UpdateTimeFreezeTimer();

        // Magnet logic - pull coins
        if (magnetActive)
        {
            PullNearbyCoins();
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

        // Check inventory
        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("coin2x"))
        {
            Debug.Log("[BoosterManager] No Coin2x booster available!");
            return false;
        }

        coin2xActive = true;
        coin2xTimer = coin2xDuration * 60f; // convert menit ke detik

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

    /// <summary>
    /// Call ini dari CoinPickup untuk apply multiplier
    /// </summary>
    public long ApplyCoinMultiplier(long baseAmount)
    {
        if (coin2xActive)
        {
            return baseAmount * 2;
        }
        return baseAmount;
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

        // Check inventory
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

    void PullNearbyCoins()
    {
        if (playerMovement == null) return;

        Vector3 playerPos = playerMovement.transform.position;

        // Find all coins dalam range
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, magnetRange);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Coin") || hit.GetComponent<CoinPickup>() != null)
            {
                // Pull coin ke arah player
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

        // Check inventory
        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("shield"))
        {
            Debug.Log("[BoosterManager] No Shield booster available!");
            return false;
        }

        shieldActive = true;
        shieldHitCount = 0;
        shieldUsedThisLife = false;

        Debug.Log("[BoosterManager] Shield activated!");
        return true;
    }

    /// <summary>
    /// Call ini dari PlanetDamage sebelum apply damage ke player
    /// Return true jika shield absorb hit, false jika damage harus apply
    /// </summary>
    public bool TryAbsorbHit()
    {
        if (!shieldActive) return false;

        shieldHitCount++;
        Debug.Log($"[BoosterManager] Shield absorbed hit {shieldHitCount}");

        // Shield hancur setelah 1 hit (sesuai spec: shield melindungi dari 1x hit planet)
        shieldActive = false;
        shieldUsedThisLife = true;

        return true; // Hit absorbed
    }

    void OnPlayerDied()
    {
        // Reset shield saat player mati
        shieldActive = false;
        shieldHitCount = 0;
        shieldUsedThisLife = false;
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

        // Check inventory
        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("speedboost"))
        {
            Debug.Log("[BoosterManager] No SpeedBoost available!");
            return false;
        }

        speedBoostActive = true;
        speedBoostTimer = speedBoostDuration;

        // Apply speed boost ke player
        if (playerMovement != null)
        {
            playerMovement.moveSpeed *= speedBoostMultiplier;
        }

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

            // Reset speed ke normal
            if (playerMovement != null)
            {
                playerMovement.moveSpeed /= speedBoostMultiplier;
            }

            Debug.Log("[BoosterManager] SpeedBoost expired!");
        }
    }

    /// <summary>
    /// Call ini dari PlanetDamage untuk check apakah planet harus hancur
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

        // Check inventory
        if (BoosterInventory.Instance == null || !BoosterInventory.Instance.UseBooster("timefreeze"))
        {
            Debug.Log("[BoosterManager] No TimeFreeze available!");
            return false;
        }

        timeFreezeActive = true;
        timeFreezeTimer = timeFreezeDuration;

        // Stop spawner
        if (gameplaySpawner != null)
        {
            StopAllCoroutines(); // Stop spawner coroutines jika perlu
        }

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

            // Resume spawner jika perlu

            Debug.Log("[BoosterManager] TimeFreeze expired!");
        }
    }

    /// <summary>
    /// Call ini dari spawner untuk check apakah spawn dibolehkan
    /// </summary>
    public bool CanSpawn()
    {
        return !timeFreezeActive;
    }

    #endregion

    #region UTILITY

    /// <summary>
    /// Get remaining time untuk booster (untuk UI display)
    /// </summary>
    public float GetRemainingTime(string boosterType)
    {
        switch (boosterType.ToLower())
        {
            case "coin2x": return coin2xActive ? coin2xTimer : 0f;
            case "magnet": return magnetActive ? magnetTimer : 0f;
            case "speedboost": return speedBoostActive ? speedBoostTimer : 0f;
            case "timefreeze": return timeFreezeActive ? timeFreezeTimer : 0f;
            default: return 0f;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !magnetActive) return;

        if (playerMovement != null)
        {
            // Draw magnet range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerMovement.transform.position, magnetRange);
        }
    }

    #endregion
}