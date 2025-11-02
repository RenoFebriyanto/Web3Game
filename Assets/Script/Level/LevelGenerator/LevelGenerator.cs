#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool untuk auto-generate 100+ levels
/// Usage: Select LevelGeneratorConfig asset > Right click > Generate Levels
/// </summary>
public class LevelGenerator
{
    [MenuItem("Assets/Kulino/Generate Levels from Config", false, 100)]
    static void GenerateLevelsFromSelected()
    {
        // Get selected LevelGeneratorConfig
        var config = Selection.activeObject as LevelGeneratorConfig;

        if (config == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a LevelGeneratorConfig asset first!", "OK");
            return;
        }

        GenerateLevels(config);
    }

    [MenuItem("Assets/Kulino/Generate Levels from Config", true)]
    static bool ValidateGenerateLevels()
    {
        return Selection.activeObject is LevelGeneratorConfig;
    }

    public static void GenerateLevels(LevelGeneratorConfig config)
    {
        if (config == null)
        {
            Debug.LogError("[LevelGenerator] Config is null!");
            return;
        }

        // Confirm dialog
        bool confirm = EditorUtility.DisplayDialog(
            "Generate Levels",
            $"This will generate {config.totalLevels} levels.\n\nOutput folder: Assets/{config.outputFolder}\n\nThis may overwrite existing files. Continue?",
            "Generate",
            "Cancel"
        );

        if (!confirm) return;

        Debug.Log($"[LevelGenerator] Starting generation of {config.totalLevels} levels...");

        // Setup random seed
        System.Random rng = config.randomSeed > 0 ? new System.Random(config.randomSeed) : new System.Random();

        // Create output folder
        string outputPath = Path.Combine(Application.dataPath, config.outputFolder);
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            Debug.Log($"[LevelGenerator] Created output folder: {outputPath}");
        }

        List<LevelConfig> generatedLevels = new List<LevelConfig>();

        // Generate levels
        for (int i = 1; i <= config.totalLevels; i++)
        {
            LevelConfig levelConfig = GenerateSingleLevel(i, config, rng);

            if (levelConfig != null)
            {
                // Save as asset
                string assetPath = $"Assets/{config.outputFolder}/level_{i}.asset";

                // Check if exists
                var existing = AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath);
                if (existing != null)
                {
                    // Update existing
                    existing.id = levelConfig.id;
                    existing.number = levelConfig.number;
                    existing.displayName = levelConfig.displayName;
                    existing.requirements = levelConfig.requirements;
                    existing.showPreviewInSelector = levelConfig.showPreviewInSelector;
                    EditorUtility.SetDirty(existing);
                    generatedLevels.Add(existing);
                }
                else
                {
                    // Create new
                    AssetDatabase.CreateAsset(levelConfig, assetPath);
                    generatedLevels.Add(levelConfig);
                }
            }

            // Progress bar
            if (i % 10 == 0 || i == config.totalLevels)
            {
                EditorUtility.DisplayProgressBar("Generating Levels", $"Level {i}/{config.totalLevels}", (float)i / config.totalLevels);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Update database
        if (config.targetDatabase != null)
        {
            UpdateDatabase(config.targetDatabase, generatedLevels);
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"[LevelGenerator] ✓ Generated {generatedLevels.Count} levels successfully!");
        EditorUtility.DisplayDialog("Success", $"Generated {generatedLevels.Count} levels!\n\nLocation: Assets/{config.outputFolder}", "OK");
    }

    static LevelConfig GenerateSingleLevel(int levelNumber, LevelGeneratorConfig config, System.Random rng)
    {
        // Create instance
        LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();

        // Basic info
        level.id = $"level_{levelNumber}";
        level.number = levelNumber;
        level.displayName = string.Format(config.levelNamePattern, levelNumber);
        level.showPreviewInSelector = true;

        // Get tier for this level
        var tier = config.GetTierForLevel(levelNumber);
        if (tier == null)
        {
            Debug.LogWarning($"[LevelGenerator] No tier found for level {levelNumber}");
            return null;
        }

        // Calculate requirement count (based on tier and curve)
        float progress = config.GetLevelProgress(levelNumber);
        float curveValue = config.requirementCountCurve.Evaluate(progress);
        int requirementCount = Mathf.RoundToInt(Mathf.Lerp(tier.minRequirements, tier.maxRequirements, curveValue));
        requirementCount = Mathf.Clamp(requirementCount, 1, 6); // Max 6 (UI limit)

        // Generate requirements
        level.requirements = new List<FragmentRequirement>();

        List<FragmentType> availableTypes = new List<FragmentType>(config.availableFragmentTypes);

        for (int i = 0; i < requirementCount; i++)
        {
            if (availableTypes.Count == 0) break;

            // Pick random fragment type
            int typeIndex = rng.Next(availableTypes.Count);
            FragmentType fragType = availableTypes[typeIndex];

            // Check if rare (Sun, Moon) - control frequency
            if ((fragType == FragmentType.Sun || fragType == FragmentType.Moon) && rng.NextDouble() > config.rareFragmentChance)
            {
                // Skip rare fragment, try again
                if (availableTypes.Count > 1)
                {
                    availableTypes.RemoveAt(typeIndex);
                    i--; // Retry this slot
                    continue;
                }
            }

            // Remove from available (no duplicates in same level)
            availableTypes.RemoveAt(typeIndex);

            // Calculate amount (based on tier and curve)
            float amountCurve = config.fragmentAmountCurve.Evaluate(progress);
            int amount = Mathf.RoundToInt(Mathf.Lerp(config.minFragmentAmount, config.maxFragmentAmount, amountCurve));

            // Add some randomness (±20%)
            float randomFactor = (float)(rng.NextDouble() * 0.4 - 0.2); // -0.2 to +0.2
            amount = Mathf.RoundToInt(amount * (1f + randomFactor));
            amount = Mathf.Clamp(amount, config.minFragmentAmount, config.maxFragmentAmount);

            // Pick random color variant (1-3)
            int colorVariant = rng.Next(1, 4);

            // Create requirement
            FragmentRequirement req = new FragmentRequirement
            {
                type = fragType,
                colorVariant = colorVariant,
                count = amount
            };

            level.requirements.Add(req);
        }

        return level;
    }

    static void UpdateDatabase(LevelDatabase database, List<LevelConfig> newLevels)
    {
        if (database == null) return;

        // Clear old list
        database.levels.Clear();

        // Add new levels (sorted by number)
        newLevels.Sort((a, b) => a.number.CompareTo(b.number));
        database.levels.AddRange(newLevels);

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[LevelGenerator] Updated LevelDatabase with {newLevels.Count} levels");
    }

    [MenuItem("Kulino/Level Generator/Create Generator Config")]
    static void CreateGeneratorConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Level Generator Config",
            "LevelGeneratorConfig",
            "asset",
            "Create a new Level Generator Config"
        );

        if (string.IsNullOrEmpty(path)) return;

        var config = ScriptableObject.CreateInstance<LevelGeneratorConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = config;
        Debug.Log($"[LevelGenerator] Created new config at: {path}");
    }
}
#endif