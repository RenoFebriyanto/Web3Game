using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public struct FallbackPrefabEntry
{
    public string id;        // contoh: "planet", "ufo", "rocket" (sesuaikan dengan key anda)
    public int variant;      // 0..2 (warna variant)
    public GameObject prefab; // drag prefab fragmen di inspector
}

public class FragmentIconResolver : MonoBehaviour
{
    [Header("Optional sources (priority order)")]
    public UnityEngine.Object fragmentRegistry; // drag your registry asset here if available (optional)
    public ScriptableObject fragmentIconDatabase; // optional, not required

    [Header("Fallback Prefabs (safe)")]
    public FallbackPrefabEntry[] fallbackPrefabs;

    // PUBLIC: panggil untuk mendapatkan sprite ikon (by id & variant)
    public Sprite GetIconSprite(string id, int variant)
    {
        // 1) coba fragmentIconDatabase jika Anda punya method GetSprite(string,int)
        if (fragmentIconDatabase != null)
        {
            try
            {
                var m = fragmentIconDatabase.GetType().GetMethod("GetSprite", BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    var r = m.Invoke(fragmentIconDatabase, new object[] { id, variant });
                    if (r is Sprite s && s != null) return s;
                }
            }
            catch { /* lanjut ke fallback */ }
        }

        // 2) coba fragmentRegistry via reflection: cari metode GetSprite(id, variant) atau GetIcon(...)
        if (fragmentRegistry != null)
        {
            try
            {
                var t = fragmentRegistry.GetType();
                // cari method GetSprite(string,int) atau GetIcon(string,int) atau GetSprite(int) etc.
                MethodInfo mi = t.GetMethod("GetSprite", new Type[] { typeof(string), typeof(int) })
                                ?? t.GetMethod("GetIcon", new Type[] { typeof(string), typeof(int) })
                                ?? t.GetMethod("GetSprite", new Type[] { typeof(int) }); // fallback
                if (mi != null)
                {
                    object[] args = mi.GetParameters().Length == 2 ? new object[] { id, variant } : new object[] { variant };
                    var outObj = mi.Invoke(fragmentRegistry, args);
                    if (outObj is Sprite sp && sp != null) return sp;
                }
            }
            catch { /* ignore and fallback */ }
        }

        // 3) fallback: check array fallbackPrefabs yang Anda assign manually
        if (fallbackPrefabs != null)
        {
            foreach (var e in fallbackPrefabs)
            {
                if (string.Equals(e.id, id, StringComparison.OrdinalIgnoreCase) && e.variant == variant && e.prefab != null)
                {
                    var sr = e.prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null && sr.sprite != null) return sr.sprite;
                    // jika prefab tidak punya SpriteRenderer, coba cek child
                    var childSr = e.prefab.GetComponentInChildren<SpriteRenderer>();
                    if (childSr != null && childSr.sprite != null) return childSr.sprite;
                }
            }
        }

        // 4) final fallback: coba load dari Resources/Fragments/{id}_{variant}
        string resourcePath = $"Fragments/{id}_{variant}";
        var resPrefab = Resources.Load<GameObject>(resourcePath);
        if (resPrefab != null)
        {
            var sr = resPrefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        // tidak ketemu -> return null
        return null;
    }
}
