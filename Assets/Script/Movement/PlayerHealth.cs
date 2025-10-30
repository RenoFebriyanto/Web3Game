using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UPDATED: Added player damage sound
/// </summary>
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

        // ✅ AUDIO: Play damage sound
        SoundManager.PlayerDamage();

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
        var plMove = GetComponent<PlayerLaneMovement>();
        if (plMove != null) plMove.enabled = false;

        yield return new WaitForSeconds(deathDelay);

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

    [ContextMenu("Debug Damage")]
    public void DebugDamage() => TakeDamage(1);
}