using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UPDATED: Added booster activation sound
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
    public GameObject cooldownOverlay;

    private Button button;
    private CanvasGroup canvasGroup;

    private bool wasActive = false;
    private int lastCount = -1;

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
        if (BoosterManager.Instance == null) return;

        bool isActive = BoosterManager.Instance.IsActive(boosterId);
        int currentCount = GetBoosterCount();

        if (cooldownOverlay != null)
        {
            cooldownOverlay.SetActive(isActive);
        }

        if (isActive != wasActive || currentCount != lastCount)
        {
            RefreshButtonState(isActive, currentCount);
            wasActive = isActive;
            lastCount = currentCount;
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

        int count = BoosterInventory.Instance.GetBoosterCount(boosterId);
        if (count <= 0)
        {
            Debug.Log($"[BoosterButton] No {boosterId} available!");
            return;
        }

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
            Debug.Log($"[BoosterButton] ✓ Activated {boosterId}!");

            // ✅ AUDIO: Booster activation sound
            SoundManager.BoosterActivate();

            if (BoosterCooldownManager.Instance != null)
            {
                float duration = BoosterManager.Instance.GetMaxDuration(boosterId);
                BoosterCooldownManager.Instance.ShowCooldown(boosterId, duration);
            }

            RefreshUI();
        }
        else
        {
            Debug.LogWarning($"[BoosterButton] Failed to activate {boosterId}");
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
        bool isActive = BoosterManager.Instance != null && BoosterManager.Instance.IsActive(boosterId);

        if (countText != null)
        {
            countText.text = count.ToString();
        }

        RefreshButtonState(isActive, count);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = count > 0 ? 1f : 0.5f;
        }
    }

    void RefreshButtonState(bool isActive, int count)
    {
        if (button == null) return;

        bool canActivate = boosterId.ToLower() == "coin2x" ? count > 0 : (count > 0 && !isActive);

        button.interactable = canActivate;

        if (button.interactable != wasActive)
        {
            Debug.Log($"[BoosterButton] {boosterId} button.interactable = {button.interactable} (count={count}, active={isActive})");
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