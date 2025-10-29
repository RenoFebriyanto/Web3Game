using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager untuk spawn dan manage multiple booster cooldown timers
/// Attach ke GameObject di gameplay scene
/// </summary>
public class BoosterCooldownManager : MonoBehaviour
{
    public static BoosterCooldownManager Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Prefab BoosterCooldown (drag prefab dari Project)")]
    public GameObject cooldownPrefab;

    [Tooltip("Parent transform untuk spawn cooldown (BoosterCooldown di hierarchy)")]
    public Transform cooldownParent;

    [Header("Booster Icons (Assign di Inspector)")]
    public Sprite iconCoin2x;
    public Sprite iconMagnet;
    public Sprite iconShield;
    public Sprite iconSpeedBoost;
    public Sprite iconTimeFreeze;

    // Runtime tracking
    private List<BoosterCooldownUI> activeCooldowns = new List<BoosterCooldownUI>();

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
        // Validate setup
        if (cooldownPrefab == null)
        {
            Debug.LogError("[BoosterCooldownManager] cooldownPrefab not assigned!");
        }

        if (cooldownParent == null)
        {
            Debug.LogError("[BoosterCooldownManager] cooldownParent not assigned! Assign BoosterCooldown GameObject dari hierarchy.");
        }
    }

    /// <summary>
    /// Spawn cooldown timer untuk booster yang diaktifkan
    /// Call dari BoosterButton saat booster berhasil diaktifkan
    /// </summary>
    public void ShowCooldown(string boosterId, float duration)
    {
        if (cooldownPrefab == null || cooldownParent == null)
        {
            Debug.LogError("[BoosterCooldownManager] Cannot show cooldown: prefab or parent not assigned!");
            return;
        }

        // Check apakah cooldown untuk booster ini sudah ada
        var existing = activeCooldowns.Find(c => c != null && c.BoosterId == boosterId);
        if (existing != null)
        {
            Debug.LogWarning($"[BoosterCooldownManager] Cooldown for {boosterId} already exists!");
            return;
        }

        // Get icon untuk booster ini
        Sprite icon = GetBoosterIcon(boosterId);
        if (icon == null)
        {
            Debug.LogWarning($"[BoosterCooldownManager] No icon found for {boosterId}!");
        }

        // Spawn cooldown prefab
        GameObject go = Instantiate(cooldownPrefab, cooldownParent);
        go.name = $"Cooldown_{boosterId}";

        var cooldownUI = go.GetComponent<BoosterCooldownUI>();
        if (cooldownUI != null)
        {
            cooldownUI.Initialize(boosterId, icon, duration);
            activeCooldowns.Add(cooldownUI);

            Debug.Log($"[BoosterCooldownManager] Spawned cooldown for {boosterId} ({duration}s)");
        }
        else
        {
            Debug.LogError("[BoosterCooldownManager] cooldownPrefab doesn't have BoosterCooldownUI component!");
            Destroy(go);
        }
    }

    /// <summary>
    /// Clean up expired cooldowns dari list
    /// </summary>
    void Update()
    {
        // Remove null/destroyed cooldowns
        activeCooldowns.RemoveAll(c => c == null);
    }

    /// <summary>
    /// Get icon sprite berdasarkan booster ID
    /// </summary>
    Sprite GetBoosterIcon(string boosterId)
    {
        string id = boosterId.ToLower().Trim();

        switch (id)
        {
            case "coin2x": return iconCoin2x;
            case "magnet": return iconMagnet;
            case "shield": return iconShield;
            case "speedboost":
            case "rocketboost": return iconSpeedBoost;
            case "timefreeze": return iconTimeFreeze;
            default:
                Debug.LogWarning($"[BoosterCooldownManager] Unknown booster ID: {boosterId}");
                return null;
        }
    }

    /// <summary>
    /// Clear semua active cooldowns (untuk testing)
    /// </summary>
    [ContextMenu("Clear All Cooldowns")]
    public void ClearAllCooldowns()
    {
        foreach (var cooldown in activeCooldowns)
        {
            if (cooldown != null)
            {
                Destroy(cooldown.gameObject);
            }
        }
        activeCooldowns.Clear();
        Debug.Log("[BoosterCooldownManager] All cooldowns cleared");
    }
}