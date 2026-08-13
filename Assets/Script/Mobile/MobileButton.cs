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

    void OnEnable()
    {
        // ✅ Coba link tiap kali tombol ini aktif (bukan cuma sekali di Start),
        // jadi kalau Rocket belum ada saat ini, nanti dicoba lagi.
        TryLinkMovement();
    }

    void TryLinkMovement()
    {
        if (_movement != null) return; // Unity fake-null: otomatis re-check kalau Rocket lama sudah destroyed

        var rocket = GameObject.Find("Rocket");
        if (rocket != null)
            _movement = rocket.GetComponent<PlayerLaneMovement>();

        // Fallback: cari ke seluruh scene, termasuk yang lagi nonaktif
        if (_movement == null)
            _movement = FindFirstObjectByType<PlayerLaneMovement>(FindObjectsInactive.Include);

        if (_movement != null)
            Log($"✓ Linked ke PlayerLaneMovement ({direction})");
    }

    // IPointerDownHandler: bereaksi saat tombol ditekan (lebih responsive dari onClick)
    public void OnPointerDown(PointerEventData eventData)
    {
        // ✅ Selalu coba re-link kalau belum ketemu, sebelum nyerah
        TryLinkMovement();

        if (_movement == null)
        {
            Debug.LogError($"[MobileButton] ❌ PlayerLaneMovement tidak ditemukan saat ditekan! ({gameObject.name})");
            return;
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