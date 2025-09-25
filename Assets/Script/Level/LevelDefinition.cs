// Assets/Script/Level/LevelDefinition.cs
using System;

/// <summary>
/// Compatibility shim: jika ada skrip lama yang mengharapkan 'LevelDefinition',
/// ini membuat LevelDefinition sebagai alias/child dari LevelDefData sehingga
/// tidak perlu merombak skrip lain.
/// </summary>
[Serializable]
public class LevelDefinition : LevelDefData
{
    // kosong — mewarisi semua field dan konstruktor dari LevelDefData
    public LevelDefinition() : base() { }

    public LevelDefinition(string id, int number, bool locked = true, int target = 20, int s1 = 1, int s2 = 2, int s3 = 3)
        : base(id, number, locked, target, s1, s2, s3)
    {
    }
}
