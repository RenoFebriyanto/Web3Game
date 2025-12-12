using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class LevelSelectionItem : MonoBehaviour
{
    [Header("Assign")]
    public LevelConfig levelConfig;
    public TMP_Text numberText;
    public GameObject lockedOverlay;
    public Image bgImage;
    public string gameplaySceneName = "Gameplay";

    [Header("Stars (assign 3 star objects)")]
    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

    [Header("⚡ Energy Cost")]
    [Tooltip("Energy cost per level")]
    public int energyCostPerLevel = 10;

    [Header("✨ NEW: Particle Effect")]
    [Tooltip("Particle effect GameObject (child of this level item)")]
    public GameObject particleEffect;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    void Start()
    {
        if (levelConfig != null)
        {
            Refresh();
        }
        else
        {
            Debug.LogWarning($"[LevelSelectionItem] {gameObject.name}: levelConfig is NULL!");
        }
    }

    public void Refresh()
    {
        if (levelConfig == null)
        {
            Debug.LogWarning($"[LevelSelectionItem] levelConfig not set on {gameObject.name}");
            return;
        }

        bool unlocked = LevelProgressManager.Instance != null ?
                        LevelProgressManager.Instance.IsUnlocked(levelConfig.number) :
                        (levelConfig.number == 1);

        // ✅ Check if this is the NEWEST unlocked level
        int highestUnlocked = LevelProgressManager.Instance != null ?
                              LevelProgressManager.Instance.GetHighestUnlocked() : 1;

        bool isNewestUnlock = unlocked && (levelConfig.number == highestUnlocked);

        // Update UI
        if (numberText != null)
        {
            numberText.text = levelConfig.number.ToString();
            numberText.gameObject.SetActive(unlocked);
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!unlocked);
        }

        if (btn != null)
        {
            btn.interactable = unlocked;
        }

        // ✅ PARTICLE CONTROL
        UpdateParticleEffect(unlocked, isNewestUnlock);

        RefreshStars();

        Debug.Log($"[LevelSelectionItem] ✓ {levelConfig.id} refreshed (unlocked: {unlocked}, newest: {isNewestUnlock})");
    }

    /// <summary>
    /// ✅ NEW: Control particle effect based on level status
    /// </summary>
    void UpdateParticleEffect(bool unlocked, bool isNewestUnlock)
    {
        if (particleEffect == null) return;

        // Rule:
        // - Locked level = NO particle
        // - Newest unlocked level = SHOW particle
        // - Old unlocked levels = NO particle

        if (!unlocked)
        {
            // Level LOCKED → Particle OFF
            particleEffect.SetActive(false);
        }
        else if (isNewestUnlock)
        {
            // Level BARU UNLOCK → Particle ON
            particleEffect.SetActive(true);
            Debug.Log($"[LevelSelectionItem] ✨ Particle ON for newest level: {levelConfig.number}");
        }
        else
        {
            // Level LAMA (sudah unlock sebelumnya) → Particle OFF
            particleEffect.SetActive(false);
        }
    }

    void RefreshStars()
    {
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        if (levelConfig == null) return;

        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(levelConfig.id);
        }

        if (earnedStars >= 1 && star1 != null) star1.SetActive(true);
        if (earnedStars >= 2 && star2 != null) star2.SetActive(true);
        if (earnedStars >= 3 && star3 != null) star3.SetActive(true);
    }

    void OnClicked()
    {
        if (levelConfig == null)
        {
            Debug.LogError("[LevelSelectionItem] Cannot start level: levelConfig is NULL!");
            return;
        }

        // Show level preview
        LevelPreviewController.ShowPreview(levelConfig);

        Debug.Log($"[LevelSelectionItem] Showing preview for {levelConfig.id}");
    }

    [ContextMenu("Debug: Force Refresh")]
    void Context_ForceRefresh()
    {
        Refresh();
    }
}