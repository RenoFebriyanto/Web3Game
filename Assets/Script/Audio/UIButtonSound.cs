using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach script ini ke Button untuk auto-play sound on click dan hover.
/// Bisa di-toggle via Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sound Settings")]
    [Tooltip("Play sound saat button diklik")]
    public bool playClickSound = true;

    [Tooltip("Play sound saat mouse hover")]
    public bool playHoverSound = true;

    [Header("Custom Sounds (Optional - leave empty to use default)")]
    public AudioClip customClickSound;
    public AudioClip customHoverSound;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        // Hook ke button onClick (as backup)
        if (button != null && playClickSound)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }

    // IPointerEnterHandler - called when mouse enters button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound) return;
        if (button != null && !button.interactable) return; // Don't play if disabled

        if (SoundManager.Instance != null)
        {
            if (customHoverSound != null)
            {
                SoundManager.Instance.PlaySFX(customHoverSound);
            }
            else
            {
                SoundManager.Instance.PlayButtonHover();
            }
        }
    }

    // IPointerClickHandler - called when button clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playClickSound) return;
        if (button != null && !button.interactable) return;

        PlayClickSound();
    }

    // Backup: called from button.onClick
    void OnClick()
    {
        if (!playClickSound) return;
        PlayClickSound();
    }

    void PlayClickSound()
    {
        if (SoundManager.Instance != null)
        {
            if (customClickSound != null)
            {
                SoundManager.Instance.PlaySFX(customClickSound);
            }
            else
            {
                SoundManager.Instance.PlayButtonClick();
            }
        }
    }

    // ========================================
    // PUBLIC API (untuk call manual dari code)
    // ========================================

    public void ForcePlayClick()
    {
        PlayClickSound();
    }

    public void ForcePlayHover()
    {
        if (SoundManager.Instance != null)
        {
            if (customHoverSound != null)
            {
                SoundManager.Instance.PlaySFX(customHoverSound);
            }
            else
            {
                SoundManager.Instance.PlayButtonHover();
            }
        }
    }
}