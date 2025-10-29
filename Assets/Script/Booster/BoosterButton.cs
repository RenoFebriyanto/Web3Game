using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script untuk button booster di gameplay
/// UPDATED: Spawn cooldown timer saat booster activated
/// </summary>
[RequireComponent(typeof(Button))]
public class BoosterButton : MonoBehaviour
{
    [Header("Booster Settings")]
    [Tooltip("ID booster (coin2x, magnet, shield, speedboost, timefreeze)")]
    public string boosterId = "coin2x";

    [Header("UI References")]
    public TMP_Text countText;
    public Image iconImage;
    public GameObject cooldownOverlay; // Optional: overlay untuk disable button saat cooldown

    private Button button;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }
    }

    void Start()
    {
        RefreshUI();

        // Subscribe ke inventory changes
        if (BoosterInventory.Instance != null)
        {
            BoosterInventory.Instance.OnBoosterChanged += OnInventoryChanged;
        }
    }

    void OnDestroy()
    {
        if (BoosterInventory.Instance != null)
        {
            BoosterInventory.Instance.OnBoosterChanged -= OnInventoryChanged;
        }
    }

    void Update()
    {
        // Check if booster is currently active (untuk disable button)
        if (cooldownOverlay != null && BoosterManager.Instance != null)
        {
            bool isActive = BoosterManager.Instance.IsActive(boosterId);
            cooldownOverlay.SetActive(isActive);

            // Disable button while active (kecuali coin2x yang bisa stack)
            if (button != null && boosterId.ToLower() != "coin2x")
            {
                button.interactable = !isActive && GetBoosterCount() > 0;
            }
        }
    }

    void OnButtonClick()
    {
        if (BoosterManager.Instance == null)
        {
            Debug.LogWarning("[BoosterButton] BoosterManager.Instance is null!");
            return;
        }

        if (BoosterInventory.Instance == null)
        {
            Debug.LogWarning("[BoosterButton] BoosterInventory.Instance is null!");
            return;
        }

        // Check count
        int count = BoosterInventory.Instance.GetBoosterCount(boosterId);
        if (count <= 0)
        {
            Debug.Log($"[BoosterButton] No {boosterId} available!");
            return;
        }

        // Activate booster
        bool success = false;

        switch (boosterId.ToLower())
        {
            case "coin2x":
                success = BoosterManager.Instance.ActivateCoin2x();
                break;

            case "magnet":
                success = BoosterManager.Instance.ActivateMagnet();
                break;

            case "shield":
                success = BoosterManager.Instance.ActivateShield();
                break;

            case "speedboost":
            case "rocketboost":
                success = BoosterManager.Instance.ActivateSpeedBoost();
                break;

            case "timefreeze":
                success = BoosterManager.Instance.ActivateTimeFreeze();
                break;

            default:
                Debug.LogWarning($"[BoosterButton] Unknown booster type: {boosterId}");
                break;
        }

        if (success)
        {
            Debug.Log($"[BoosterButton] Activated {boosterId}!");

            // Spawn cooldown timer UI
            if (BoosterCooldownManager.Instance != null)
            {
                float duration = BoosterManager.Instance.GetMaxDuration(boosterId);
                BoosterCooldownManager.Instance.ShowCooldown(boosterId, duration);
            }

            RefreshUI();
        }
    }

    void OnInventoryChanged(string id, int newCount)
    {
        if (id.Equals(boosterId, System.StringComparison.OrdinalIgnoreCase))
        {
            RefreshUI();
        }
    }

    void RefreshUI()
    {
        if (BoosterInventory.Instance == null) return;

        int count = GetBoosterCount();

        // Update count text
        if (countText != null)
        {
            countText.text = count.ToString();
        }

        // Update button interactable
        if (button != null)
        {
            bool isActive = BoosterManager.Instance != null && BoosterManager.Instance.IsActive(boosterId);
            button.interactable = count > 0 && !isActive;
        }

        // Update visual (alpha)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = count > 0 ? 1f : 0.5f;
        }
    }

    int GetBoosterCount()
    {
        if (BoosterInventory.Instance == null) return 0;
        return BoosterInventory.Instance.GetBoosterCount(boosterId);
    }

    [ContextMenu("Test Activate")]
    void TestActivate()
    {
        OnButtonClick();
    }
}