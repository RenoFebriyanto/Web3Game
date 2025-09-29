using System;
using UnityEngine;

[Serializable]
public enum FragmentType
{
    Planet,
    Rocket,
    UFO,
    Star,
    Moon,
    Sun
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
