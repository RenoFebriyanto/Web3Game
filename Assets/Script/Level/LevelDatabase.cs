using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Kulino/LevelDatabase", fileName = "LevelDatabase")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelConfig> levels = new List<LevelConfig>();

    public LevelConfig GetById(string id)
    {
        return levels.Find(l => l != null && l.id == id);
    }

    public LevelConfig GetByNumber(int number)
    {
        return levels.Find(l => l != null && l.number == number);
    }
}
