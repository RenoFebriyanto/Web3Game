// PlayerHealth.cs - UPDATED: Tambah game over trigger
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Health")]
    public int maxLives = 3;
    public int currentLives = 3;

    [Header("Invincibility")]
    public float invincibilityDuration = 2f;
    public float blinkFrequency = 10f;

    [Header("Death")]
    public float deathDelay = 0.9f;
    public bool disableOnDeath = true;

    [Header("Visual")]
    public SpriteRenderer[] renderersToBlink;

    [Header("Events")]
    public UnityEvent OnPlayerDamaged;
    public UnityEvent OnPlayerDeath;

    bool invincible = false;
    Coroutine invCoroutine = null;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;
        currentLives = Mathf.Clamp(currentLives, 0, maxLives);
    }

    public bool IsInvincible() => invincible;

    public void TakeDamage(int amount = 1)
    {
        if (invincible) return;
        currentLives = Mathf.Max(0, currentLives - amount);
        OnPlayerDamaged?.Invoke();

        if (invCoroutine != null) StopCoroutine(invCoroutine);
        invCoroutine = StartCoroutine(InvincibilityRoutine());

        if (currentLives <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else
        {
            Debug.Log($"[PlayerHealth] Took damage -> lives {currentLives}/{maxLives}");
        }
    }

    IEnumerator InvincibilityRoutine()
    {
        invincible = true;
        float t = 0f;
        while (t < invincibilityDuration)
        {
            float phase = Mathf.PingPong(t * blinkFrequency, 1f);
            bool visible = phase > 0.5f;
            SetRenderersVisible(visible);
            t += Time.deltaTime;
            yield return null;
        }
        SetRenderersVisible(true);
        invincible = false;
    }

    IEnumerator DieRoutine()
    {
        var laneMove = GetComponent<PlayerLaneMovement>();
        if (plMove != null) plMove.enabled = false;

        yield return new WaitForSeconds(deathDelay);

        // ✅ NEW: Trigger Game Over
        TriggerGameOver();

        OnPlayerDeath?.Invoke();

        if (disableOnDeath)
        {
            gameObject.SetActive(false);
        }
    }

    void SetRenderersVisible(bool visible)
    {
        if (renderersToBlink == null) return;
        foreach (var r in renderersToBlink) if (r != null) r.enabled = visible;
    }

    // ✅ NEW: Trigger Game Over
    void TriggerGameOver()
    {
        Debug.Log("[PlayerHealth] ⚠️ GAME OVER - Player died!");

        // Stop all spawners
        if (SpawnerController.Instance != null)
        {
            SpawnerController.Instance.StopSpawner();
        }

        // Show game over UI (using LevelCompleteUI in game over mode)
        if (LevelCompleteUI.Instance != null)
        {
            LevelCompleteUI.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogError("[PlayerHealth] LevelCompleteUI.Instance is NULL!");
        }
    }

    [ContextMenu("Debug Damage")]
    public void DebugDamage() => TakeDamage(1);
    
    [ContextMenu("Debug Kill Player")]
    public void DebugKillPlayer()
    {
        currentLives = 1;
        TakeDamage(1);
    }
}