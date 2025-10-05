using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Debug helper: tekan mouse kiri pada Game View saat Play.
/// Akan mencetak semua UI Raycast hits (dari depan ke belakang).
/// Tempel di scene, jalankan, klik area yang tidak bisa diklik.
/// </summary>
public class UIRaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (!Application.isPlaying) return;

        if (Input.GetMouseButtonDown(0))
        {
            var ev = EventSystem.current;
            if (ev == null)
            {
                Debug.LogWarning("UIRaycastDebugger: No EventSystem in scene!");
                return;
            }

            PointerEventData ped = new PointerEventData(ev);
            ped.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            ev.RaycastAll(ped, results);

            Debug.Log($"--- UI Raycast at {ped.position} returned {results.Count} results ---");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                string comp = r.gameObject.name;
                var graphic = r.gameObject.GetComponent<Graphic>();
                string gtype = graphic != null ? graphic.GetType().Name : "(no Graphic)";
                Debug.Log($"{i}: name='{comp}', distance={r.distance}, module={r.module}, graphic={gtype}, path={GetFullPath(r.gameObject.transform)}");
            }

            if (results.Count == 0)
                Debug.Log("No UI received the raycast (strange).");
        }
    }

    string GetFullPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
