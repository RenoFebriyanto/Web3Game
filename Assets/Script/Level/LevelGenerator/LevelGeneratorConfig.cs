using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration untuk Level Generator - define rules untuk auto-generate 100+ levels
/// Create via: Assets > Create > Kulino > Level Generator Config
/// </summary>
[CreateAssetMenu(fileName = "LevelGeneratorConfig", menuName = "Kulino/Level Generator Config")]
public class LevelGeneratorConfig : ScriptableObject
{
    [Header("Generation Settings")]
    [Tooltip("Total number of levels to generate")]
    public int totalLevels = 100;

    [Tooltip("Output folder for generated LevelConfig assets (relative to Assets/)")]
    public string outputFolder = "Script/Level/Config/GeneratedLevels";

    [Tooltip("Level naming pattern. Use {0} for level number. Example: 'Level {0}'")]
    public string levelNamePattern = "Level {0}";

    [Header("Difficulty Progression")]
    [Tooltip("Difficulty tiers - levels will be split into these tiers")]
    public List<DifficultyTier> difficultyTiers = new List<DifficultyTier>()
    {
        new DifficultyTier { tierName = "Easy", startLevel = 1, endLevel = 20, minRequirements = 1, maxRequirements = 2 },
        new DifficultyTier { tierName = "Medium", startLevel = 21, endLevel = 50, minRequirements = 2, maxRequirements = 4 },
        new DifficultyTier { tierName = "Hard", startLevel = 51, endLevel = 80, minRequirements = 3, maxRequirements = 5 },
        new DifficultyTier { tierName = "Expert", startLevel = 81, endLevel = 100, minRequirements = 4, maxRequirements = 6 }
    };

    [Header("Fragment Settings")]
    [Tooltip("Available fragment types for requirements")]
    public List<FragmentType> availableFragmentTypes = new List<FragmentType>()
    {
        FragmentType.Planet,
        FragmentType.Rocket,
        FragmentType.UFO,
        FragmentType.Star,
        FragmentType.Moon,
        FragmentType.Sun
    };

    [Tooltip("Min amount per fragment requirement")]
    public int minFragmentAmount = 3;

    [Tooltip("Max amount per fragment requirement")]
    public int maxFragmentAmount = 20;

    [Header("Progression Curves")]
    [Tooltip("How fragment amounts scale per level (0-1 = level progress)")]
    public AnimationCurve fragmentAmountCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("How requirement count scales per level")]
    public AnimationCurve requirementCountCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Randomization")]
    [Tooltip("Random seed for consistent generation (0 = random)")]
    public int randomSeed = 0;

    [Tooltip("Chance for rare fragments (Sun, Moon) to appear")]
    [Range(0f, 1f)]
    public float rareFragmentChance = 0.3f;

    [Header("Database Reference")]
    [Tooltip("Assign your LevelDatabase here - generated levels will be added to this")]
    public LevelDatabase targetDatabase;

    /// <summary>
    /// Get difficulty tier for a specific level number
    /// </summary>
    public DifficultyTier GetTierForLevel(int levelNumber)
    {
        foreach (var tier in difficultyTiers)
        {
            if (levelNumber >= tier.startLevel && levelNumber <= tier.endLevel)
            {
                return tier;
            }
        }

        // Fallback: return last tier
        return difficultyTiers.Count > 0 ? difficultyTiers[difficultyTiers.Count - 1] : null;
    }

    /// <summary>
    /// Get progress value (0-1) for a level number within total levels
    /// </summary>
    public float GetLevelProgress(int levelNumber)
    {
        if (totalLevels <= 1) return 1f;
        return Mathf.Clamp01((float)(levelNumber - 1) / (totalLevels - 1));
    }
}

[System.Serializable]
public class DifficultyTier
{
    public string tierName = "Easy";
    public int startLevel = 1;
    public int endLevel = 20;
    public int minRequirements = 1;
    public int maxRequirements = 3;
}