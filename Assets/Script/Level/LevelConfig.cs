// Assets/Scripts/Level/LevelConfig.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelConfig
{
    // Required by many existing scripts
    public string id = "level_1";    // unique id
    public int number = 1;           // human-readable number

    // locked/unlocked state (inspector)
    public bool locked = true;

    // Star/best info can be stored externally (LevelProgressManager) but
    // some UI scripts referenced a bestStars field — we expose it for safety.
    [NonSerialized] public int bestStars = 0;

    // Requirements for this level (fragmen, counts, variants)
    public List<LevelRequirement> requirements = new List<LevelRequirement>();

    // Optional description for editor
    [TextArea] public string description;
}
