using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Kulino/LevelConfig", fileName = "LevelConfig_")]
public class LevelConfig : ScriptableObject
{
    [Header("Identity")]
    public string id = "level_1"; // unique id, eg "level_1"
    public int number = 1;       // human readable number

    [Header("Description (optional)")]
    public string displayName;

    [Header("Fragment requirements (what player must collect to complete this level)")]
    public List<FragmentRequirement> requirements = new List<FragmentRequirement>();

    [Header("Optional UI / tuning")]
    public bool showPreviewInSelector = true;
}
