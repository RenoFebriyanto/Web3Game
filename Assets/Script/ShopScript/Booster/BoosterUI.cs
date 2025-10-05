// BoosterUI.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public class BoosterSlot
{
    public string boosterId;         // must match ShopItemData.itemId for that booster
    public TMP_Text countText;       // Text pro yang menampilkan angka
    public GameObject root;          // optional root to enable/disable
}

public class BoosterUI : MonoBehaviour
{
    public List<BoosterSlot> slots = new List<BoosterSlot>();

    void Awake()
    {
        // subscribe
        if (BoosterInventory.Instance != null)
        {
            BoosterInventory.Instance.OnBoosterChanged += OnBoosterChanged;
            BoosterInventory.Instance.OnInventoryChanged += RefreshAll;
        }
    }

    void Start()
    {
        RefreshAll();

        if (BoosterInventory.Instance != null)
        {
            BoosterInventory.Instance.OnBoosterChanged += OnBoosterChanged;
            BoosterInventory.Instance.OnInventoryChanged += RefreshAll;
        }
    }

    void OnDestroy()
    {
        if (BoosterInventory.Instance != null)
        {
            BoosterInventory.Instance.OnBoosterChanged -= OnBoosterChanged;
            BoosterInventory.Instance.OnInventoryChanged -= RefreshAll;
        }
    }

    void OnBoosterChanged(string id, int count)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (string.Equals(slots[i].boosterId, id, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSlot(slots[i], count);
                break;
            }
        }
    }

    void RefreshAll()
    {
        if (BoosterInventory.Instance == null)
        {
            // nothing to show
            foreach (var s in slots) { UpdateSlot(s, 0); }
            return;
        }
        foreach (var s in slots)
        {
            int c = BoosterInventory.Instance != null ? BoosterInventory.Instance.GetBoosterCount(s.boosterId) : 0;
            UpdateSlot(s, c);
        }

    }

    void UpdateSlot(BoosterSlot slot, int count)
    {
        // update angka
        if (slot.countText != null) slot.countText.text = count.ToString();

        // selalu biarkan root aktif (agar slot selalu terlihat).
        if (slot.root != null)
        {
            slot.root.SetActive(true);

            // Opsional: beri tampilan "disabled" bila count == 0
            // (gunakan CanvasGroup jika ada, atau ubah warna child image bila ingin)
            var cg = slot.root.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                // jika belum ada, coba tambahkan satu (aman)
                cg = slot.root.AddComponent<CanvasGroup>();
            }
            cg.alpha = (count > 0) ? 1f : 0.6f;   // 0.6 membuatnya terlihat "mati" tapi masih kelihatan
            cg.interactable = true; // agar tombol/tap tetap responsif kalau Anda mau
            cg.blocksRaycasts = true;
        }
    }

}
