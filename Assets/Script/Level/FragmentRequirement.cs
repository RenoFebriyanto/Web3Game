// Assets/Script/Level/FragmentRequirement.cs
using System;
using UnityEngine;

[Serializable]
public enum FragmentType
{
    Planet = 0,
    Rocket = 1,
    UFO = 2,
    Star = 3,
    Moon = 4,
    Sun = 5
}

[Serializable]
public class FragmentRequirement
{
    public FragmentType type;
    [Tooltip("Color variant index (0..2)")]
    public int colorVariant = 0;
    [Tooltip("Jumlah fragmen yang harus dikumpulkan")]
    public int count = 1;
}