// Assets/Script/Level/LevelDefinition.cs
using System;

[Serializable]
public class LevelDefinition
{
    public string id;           // unique id e.g. "level_1"
    public int number;          // human number (1..N)
    public bool locked = false; // locked/unlocked
    public int targetDistanceMeters = 20; // sample condition (you can change per-level)
    public int starThreshold1 = 1; // not used if you want custom logic, but example
    public int starThreshold2 = 2;
    public int starThreshold3 = 3;
}
