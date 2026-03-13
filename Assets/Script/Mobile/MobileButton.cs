using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ✅ MobileButton - Attach langsung ke BTNLEFT atau BTNRIGHT
/// Tidak bergantung pada onClick binding dari luar — lebih reliable.
/// 
/// SETUP:
/// 1. Attach script ini ke GameObject BTNLEFT → set Direction = Left
/// 2. Attach script ini ke GameObject BTNRIGHT → set Direction = Right
/// </summary>
[RequireComponent(typeof(Button))]
public class MobileButton : MonoBehaviour, IPointerDownHandler
{
    public enum MoveDirection { Left, Right }

    [Header("Arah Gerak")]
    public MoveDirection direction = MoveDirection.Left;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private PlayerLaneMovement _movement;

    void Start()
    {
        // Cari PlayerLaneMovement dari Rocket
        var rocket = GameObject.Find("Rocket");
        if (rocket != null)
            _movement = rocket.GetComponent<PlayerLaneMovement>();

        // Fallback seluruh scene
        if (_movement == null)
            _movement = FindFirstObjectByType<PlayerLaneMovement>();

        if (_movement != null)
            Log($"✓ Linked ke PlayerLaneMovement ({direction})");
        else
            Debug.LogError($"[MobileButton] ❌ PlayerLaneMovement tidak ditemukan! ({gameObject.name})");
    }

    // IPointerDownHandler: bereaksi saat tombol ditekan (lebih responsive dari onClick)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_movement == null)
        {
            // Retry find
            _movement = FindFirstObjectByType<PlayerLaneMovement>();
            if (_movement == null) return;
        }

        if (direction == MoveDirection.Left)
        {
            Log("◀ MoveLeft()");
            _movement.MoveLeft();
        }
        else
        {
            Log("▶ MoveRight()");
            _movement.MoveRight();
        }
    }

    void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[MobileButton:{gameObject.name}] {msg}");
    }
}