// Assets/Scripts/Level/LevelRequirement.cs
using System;
using UnityEngine;

[Serializable]
public class LevelRequirement
{
    // field lama / utama - string agar kompatibel dengan prefab/nama yang Anda pakai
    public string fragmentId = "";    // contoh: "ufo", "planet", "rocket", "star", "moon", "sun"

    // beberapa skrip lama mungkin memakai 'type' - sediakan alias agar kompatibel
    // Anda bisa mengisi salah satu dari fragmentId atau type di inspector; kita gunakan keduanya.
    public string type = "";          // alias untuk fragmentId (legacy)

    public int colorVariant = 0;      // 0..2 (3 variant warna)
    public int count = 1;             // jumlah yang diperlukan

    // optional label untuk ditampilkan
    public string label;

    // Helper property (runtime) : prefer fragmentId, fallback ke type
    public string EffectiveFragmentId
    {
        get
        {
            if (!string.IsNullOrEmpty(fragmentId)) return fragmentId;
            return type ?? "";
        }
    }
}
