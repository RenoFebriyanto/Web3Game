// Assets/Script/Level/LevelConfig.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Kulino/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string id = "level_1";
    public int number = 1;
    public string displayName = "Level 1";

    [Header("Fragment Requirements")]
    public List<FragmentRequirement> requirements = new List<FragmentRequirement>();

    public bool showPreviewInSelector = true;
}