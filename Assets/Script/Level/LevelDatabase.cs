// Assets/Scripts/Level/LevelDatabase.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Kulino/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelConfig> levels = new List<LevelConfig>();

    public LevelConfig GetById(string id)
    {
        return levels.Find(l => l != null && l.id == id);
    }

    public LevelConfig GetByNumber(int num)
    {
        return levels.Find(l => l != null && l.number == num);
    }
}
