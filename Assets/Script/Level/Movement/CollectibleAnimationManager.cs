using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅ COMPLETE FIXED: Animation path & Particle effects
/// - Popup flies CORRECTLY to target UI
/// - Particle effects spawn on collect
/// </summary>
public class CollectibleAnimationManager : MonoBehaviour
{
    public static CollectibleAnimationManager Instance { get; private set; }

    [Header("🎯 PREFAB REFERENCES")]
    [Tooltip("Prefab untuk coin popup (Image dengan Coin sprite)")]
    public GameObject coinPopupPrefab;

    [Tooltip("Prefab untuk fragment popup (Image dengan Fragment sprite)")]
    public GameObject fragmentPopupPrefab;

    [Tooltip("Prefab untuk star popup (Image dengan Star sprite)")]
    public GameObject starPopupPrefab;

    [Header("✨ PARTICLE EFFECTS")]
    [Tooltip("Particle effect untuk coin pickup")]
    public GameObject coinParticleEffect;

    [Tooltip("Particle effect untuk fragment pickup")]
    public GameObject fragmentParticleEffect;

    [Tooltip("Particle effect untuk star pickup")]
    public GameObject starParticleEffect;

    [Header("🎨 UI TARGETS")]
    [Tooltip("Target UI Coin (top-right corner)")]
    public RectTransform coinUITarget;

    [Tooltip("Canvas tempat spawn popup")]
    public Canvas targetCanvas;

    [Header("⚙️ ANIMATION SETTINGS")]
    [Tooltip("Durasi popup muncul & scale up")]
    public float popupDuration = 0.3f;

    [Tooltip("Durasi fly ke target")]
    public float flyDuration = 0.6f;

    [Tooltip("Scale popup saat muncul")]
    public float popupScale = 1.2f;

    [Tooltip("Curve untuk fly animation")]
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("📊 DEBUG")]
    public bool enableDebugLogs = true;
    public bool showDebugGizmos = false;

    // Pool untuk reuse popup objects
    private Queue<GameObject> coinPopupPool = new Queue<GameObject>();
    private Queue<GameObject> fragmentPopupPool = new Queue<GameObject>();
    private Queue<GameObject> starPopupPool = new Queue<GameObject>();

    private const int POOL_SIZE = 10;

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
        // Auto-find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        // Validate setup
        if (coinPopupPrefab == null)
        {
            LogError("coinPopupPrefab not assigned!");
        }

        if (fragmentPopupPrefab == null)
        {
            LogError("fragmentPopupPrefab not assigned!");
        }

        if (starPopupPrefab == null)
        {
            LogError("starPopupPrefab not assigned!");
        }

        if (coinUITarget == null)
        {
            LogWarning("coinUITarget not assigned! Searching for 'Coin' in UI...");
            // Try to find coin UI automatically
            var coinUI = GameObject.Find("Coin");
            if (coinUI != null)
            {
                coinUITarget = coinUI.GetComponent<RectTransform>();
                Log("✓ Found Coin UI automatically");
            }
        }

        // Pre-warm pools
        InitializePools();

        Log("✓ CollectibleAnimationManager initialized");
    }

    void InitializePools()
    {
        // Create coin pool
        if (coinPopupPrefab != null && targetCanvas != null)
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject obj = Instantiate(coinPopupPrefab, targetCanvas.transform);
                obj.SetActive(false);
                coinPopupPool.Enqueue(obj);
            }
        }

        // Create fragment pool
        if (fragmentPopupPrefab != null && targetCanvas != null)
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject obj = Instantiate(fragmentPopupPrefab, targetCanvas.transform);
                obj.SetActive(false);
                fragmentPopupPool.Enqueue(obj);
            }
        }

        // Create star pool
        if (starPopupPrefab != null && targetCanvas != null)
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject obj = Instantiate(starPopupPrefab, targetCanvas.transform);
                obj.SetActive(false);
                starPopupPool.Enqueue(obj);
            }
        }

        Log($"✓ Pools initialized: Coin={coinPopupPool.Count}, Fragment={fragmentPopupPool.Count}, Star={starPopupPool.Count}");
    }

    // ========================================
    // 💰 COIN ANIMATION
    // ========================================

    public void AnimateCoinCollect(Vector3 worldPosition, int coinValue = 1)
    {
        if (coinPopupPrefab == null || targetCanvas == null)
        {
            LogError("Cannot animate coin: missing references!");
            return;
        }

        Log($"🎬 Animating coin collect: value={coinValue}, position={worldPosition}");

        // ✅ Spawn particle effect
        SpawnParticleEffect(coinParticleEffect, worldPosition);

        StartCoroutine(CoinCollectRoutine(worldPosition, coinValue));
    }

    IEnumerator CoinCollectRoutine(Vector3 worldPosition, int coinValue)
    {
        GameObject popup = GetPopupFromPool(coinPopupPool, coinPopupPrefab);
        if (popup == null) yield break;

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        Image popupImage = popup.GetComponent<Image>();

        if (popupRect == null || popupImage == null)
        {
            LogError("Popup missing RectTransform or Image!");
            yield break;
        }

        // ✅ FIX: Convert world to canvas position CORRECTLY
        Vector2 canvasPos = WorldToCanvasPosition(worldPosition, popupRect);
        popupRect.anchoredPosition = canvasPos;

        // Reset state
        popupRect.localScale = Vector3.zero;
        Color color = popupImage.color;
        color.a = 1f;
        popupImage.color = color;

        popup.SetActive(true);

        Log($"Popup start position: {canvasPos}");

        // ✅ PHASE 1: Popup & Scale Up
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            float scale = Mathf.Lerp(0f, popupScale, EaseOutBack(t));
            popupRect.localScale = Vector3.one * scale;

            yield return null;
        }

        popupRect.localScale = Vector3.one * popupScale;

        // Small pause
        yield return new WaitForSeconds(0.1f);

        // ✅ PHASE 2: Fly to UI Target (FIXED)
        if (coinUITarget != null)
        {
            Vector2 startPos = popupRect.anchoredPosition;
            Vector2 targetPos = coinUITarget.anchoredPosition;

            Log($"Flying: start={startPos}, target={targetPos}");

            elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;
                float curveT = flyCurve.Evaluate(t);

                // ✅ CRITICAL: Lerp in anchored position space
                popupRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);

                // Scale down
                float scale = Mathf.Lerp(popupScale, 0.5f, t);
                popupRect.localScale = Vector3.one * scale;

                // Fade out near end
                if (t > 0.7f)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    color.a = Mathf.Lerp(1f, 0f, fadeT);
                    popupImage.color = color;
                }

                yield return null;
            }

            // Ensure final position
            popupRect.anchoredPosition = targetPos;
            Log($"Reached target: {targetPos}");
        }

        // ✅ PHASE 3: Trigger Counter Update
        if (CoinCounterUI.Instance != null)
        {
            CoinCounterUI.Instance.AddCoins(coinValue);
            Log($"✓ Added {coinValue} coins to counter");
        }

        // Return to pool
        ReturnToPool(popup, coinPopupPool);
    }

    // ========================================
    // 🧩 FRAGMENT ANIMATION
    // ========================================

    public void AnimateFragmentCollect(Vector3 worldPosition, Sprite fragmentSprite, int missionBoxIndex)
    {
        if (fragmentPopupPrefab == null || targetCanvas == null)
        {
            LogError("Cannot animate fragment: missing references!");
            return;
        }

        Log($"🎬 Animating fragment collect: box={missionBoxIndex}, position={worldPosition}");

        // ✅ Spawn particle effect
        SpawnParticleEffect(fragmentParticleEffect, worldPosition);

        StartCoroutine(FragmentCollectRoutine(worldPosition, fragmentSprite, missionBoxIndex));
    }

    IEnumerator FragmentCollectRoutine(Vector3 worldPosition, Sprite fragmentSprite, int missionBoxIndex)
    {
        GameObject popup = GetPopupFromPool(fragmentPopupPool, fragmentPopupPrefab);
        if (popup == null) yield break;

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        Image popupImage = popup.GetComponent<Image>();

        if (popupRect == null || popupImage == null) yield break;

        // Set sprite
        if (fragmentSprite != null)
        {
            popupImage.sprite = fragmentSprite;
        }

        // ✅ FIX: Convert world to canvas position
        Vector2 canvasPos = WorldToCanvasPosition(worldPosition, popupRect);
        popupRect.anchoredPosition = canvasPos;

        // Reset state
        popupRect.localScale = Vector3.zero;
        Color color = popupImage.color;
        color.a = 1f;
        popupImage.color = color;

        popup.SetActive(true);

        // ✅ PHASE 1: Popup & Scale Up
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            float scale = Mathf.Lerp(0f, popupScale, EaseOutBack(t));
            popupRect.localScale = Vector3.one * scale;

            yield return null;
        }

        // Small pause
        yield return new WaitForSeconds(0.15f);

        // ✅ PHASE 2: Find mission box target (FIXED)
        RectTransform missionBoxTarget = FindMissionBoxTarget(missionBoxIndex);

        if (missionBoxTarget != null)
        {
            Vector2 startPos = popupRect.anchoredPosition;
            Vector2 targetPos = missionBoxTarget.anchoredPosition;

            Log($"Fragment flying: start={startPos}, target={targetPos}");

            elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flyDuration;
                float curveT = flyCurve.Evaluate(t);

                // ✅ CRITICAL: Lerp in anchored position space
                popupRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveT);

                float scale = Mathf.Lerp(popupScale, 0.3f, t);
                popupRect.localScale = Vector3.one * scale;

                if (t > 0.7f)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    color.a = Mathf.Lerp(1f, 0f, fadeT);
                    popupImage.color = color;
                }

                yield return null;
            }

            // Ensure final position
            popupRect.anchoredPosition = targetPos;
            Log($"Fragment reached target: {targetPos}");
        }
        else
        {
            LogWarning($"Mission box {missionBoxIndex} not found!");
        }

        // Return to pool
        ReturnToPool(popup, fragmentPopupPool);
    }

    RectTransform FindMissionBoxTarget(int boxIndex)
    {
        var missionUI = FindFirstObjectByType<FragmentMissionUI>();
        if (missionUI == null)
        {
            LogWarning("FragmentMissionUI not found!");
            return null;
        }

        if (missionUI.missionBoxes != null && boxIndex < missionUI.missionBoxes.Count)
        {
            var box = missionUI.missionBoxes[boxIndex];
            if (box != null && box.rootObject != null)
            {
                var rectTransform = box.rootObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Log($"Found mission box {boxIndex}: {box.rootObject.name}");
                    return rectTransform;
                }
            }
        }

        LogWarning($"Mission box {boxIndex} not found or invalid!");
        return null;
    }

    // ========================================
    // ⭐ STAR ANIMATION
    // ========================================

    public void AnimateStarCollect(Vector3 worldPosition)
    {
        if (starPopupPrefab == null || targetCanvas == null)
        {
            LogError("Cannot animate star: missing references!");
            return;
        }

        Log($"🎬 Animating star collect: position={worldPosition}");

        // ✅ Spawn particle effect
        SpawnParticleEffect(starParticleEffect, worldPosition);

        StartCoroutine(StarCollectRoutine(worldPosition));
    }

    IEnumerator StarCollectRoutine(Vector3 worldPosition)
    {
        GameObject popup = GetPopupFromPool(starPopupPool, starPopupPrefab);
        if (popup == null) yield break;

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        Image popupImage = popup.GetComponent<Image>();

        if (popupRect == null || popupImage == null) yield break;

        Vector2 canvasPos = WorldToCanvasPosition(worldPosition, popupRect);
        popupRect.anchoredPosition = canvasPos;

        popupRect.localScale = Vector3.zero;
        Color color = popupImage.color;
        color.a = 1f;
        popupImage.color = color;

        popup.SetActive(true);

        // Scale up
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            float scale = Mathf.Lerp(0f, popupScale * 1.5f, EaseOutBack(t));
            popupRect.localScale = Vector3.one * scale;

            yield return null;
        }

        // Pause
        yield return new WaitForSeconds(0.3f);

        // Fade out
        elapsed = 0f;
        float fadeDuration = 0.3f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            color.a = Mathf.Lerp(1f, 0f, t);
            popupImage.color = color;

            float scale = Mathf.Lerp(popupScale * 1.5f, popupScale * 2f, t);
            popupRect.localScale = Vector3.one * scale;

            yield return null;
        }

        ReturnToPool(popup, starPopupPool);
    }

    // ========================================
    // ✨ PARTICLE EFFECTS
    // ========================================

    void SpawnParticleEffect(GameObject particlePrefab, Vector3 worldPosition)
    {
        if (particlePrefab == null)
        {
            // No particle assigned - skip silently
            return;
        }

        GameObject particle = Instantiate(particlePrefab, worldPosition, Quaternion.identity);
        
        // Auto-destroy after particle duration
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(particle, ps.main.duration + ps.main.startLifetime.constantMax);
            Log($"✨ Spawned particle at {worldPosition}");
        }
        else
        {
            // No ParticleSystem - destroy after 2 seconds
            Destroy(particle, 2f);
        }
    }

    // ========================================
    // 🔧 UTILITY METHODS
    // ========================================

    /// <summary>
    /// ✅ FIXED: Convert world position to canvas anchored position
    /// </summary>
    Vector2 WorldToCanvasPosition(Vector3 worldPosition, RectTransform rectTransform)
    {
        if (targetCanvas == null) return Vector2.zero;

        Camera cam = Camera.main;
        if (cam == null) return Vector2.zero;

        // Get canvas rect
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return Vector2.zero;

        // Convert world to viewport
        Vector2 viewportPos = cam.WorldToViewportPoint(worldPosition);

        // Convert viewport to canvas position
        Vector2 canvasPos = new Vector2(
            (viewportPos.x - 0.5f) * canvasRect.sizeDelta.x,
            (viewportPos.y - 0.5f) * canvasRect.sizeDelta.y
        );

        Log($"World {worldPosition} → Viewport {viewportPos} → Canvas {canvasPos}");

        return canvasPos;
    }

    GameObject GetPopupFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                return obj;
            }
        }

        // Create new if pool empty
        if (prefab != null && targetCanvas != null)
        {
            GameObject obj = Instantiate(prefab, targetCanvas.transform);
            obj.SetActive(false);
            return obj;
        }

        return null;
    }

    void ReturnToPool(GameObject obj, Queue<GameObject> pool)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CollectibleAnimation] {message}");
    }

    void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CollectibleAnimation] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[CollectibleAnimation] {message}");
    }

    // ========================================
    // 🎨 DEBUG GIZMOS
    // ========================================

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Draw coin UI target
        if (coinUITarget != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 worldPos = coinUITarget.position;
            Gizmos.DrawWireSphere(worldPos, 0.5f);
        }
    }
}