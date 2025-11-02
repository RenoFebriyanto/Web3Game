#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window untuk Level Generator - easier workflow
/// Access via: Window > Kulino > Level Generator
/// </summary>
public class LevelGeneratorWindow : EditorWindow
{
    private LevelGeneratorConfig config;
    private Vector2 scrollPosition;
    private bool showPreview = false;
    private int previewLevelNumber = 1;

    [MenuItem("Window/Kulino/Level Generator")]
    static void OpenWindow()
    {
        var window = GetWindow<LevelGeneratorWindow>("Level Generator");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        EditorGUILayout.Space(10);
        GUILayout.Label("Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generate 100+ levels automatically with configurable difficulty progression.", MessageType.Info);

        EditorGUILayout.Space(10);

        // Config reference
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        config = (LevelGeneratorConfig)EditorGUILayout.ObjectField("Generator Config", config, typeof(LevelGeneratorConfig), false);

        if (config == null)
        {
            EditorGUILayout.HelpBox("Please assign a LevelGeneratorConfig.\n\nCreate one via: Assets > Create > Kulino > Level Generator Config", MessageType.Warning);

            if (GUILayout.Button("Create New Config", GUILayout.Height(30)))
            {
                CreateNewConfig();
            }

            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(10);

        // Quick info
        EditorGUILayout.LabelField("Generation Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Total Levels:", config.totalLevels.ToString());
        EditorGUILayout.LabelField("Output Folder:", config.outputFolder);
        EditorGUILayout.LabelField("Target Database:", config.targetDatabase != null ? config.targetDatabase.name : "Not assigned");
        EditorGUILayout.LabelField("Difficulty Tiers:", config.difficultyTiers.Count.ToString());

        EditorGUILayout.Space(10);

        // Preview
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview Level Generation");

        if (showPreview)
        {
            EditorGUI.indentLevel++;

            previewLevelNumber = EditorGUILayout.IntSlider("Level Number", previewLevelNumber, 1, config.totalLevels);

            if (GUILayout.Button("Generate Preview", GUILayout.Height(25)))
            {
                PreviewLevel(previewLevelNumber);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(20);

        // Generate button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button($"GENERATE {config.totalLevels} LEVELS", GUILayout.Height(50)))
        {
            LevelGenerator.GenerateLevels(config);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Quick actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Open Config"))
        {
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        if (GUILayout.Button("Open Output Folder"))
        {
            string path = "Assets/" + config.outputFolder;
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            else
            {
                Debug.LogWarning($"Folder not found: {path}");
            }
        }

        EditorGUILayout.EndHorizontal();

        if (config.targetDatabase != null)
        {
            if (GUILayout.Button("Open Database"))
            {
                Selection.activeObject = config.targetDatabase;
                EditorGUIUtility.PingObject(config.targetDatabase);
            }
        }

        EditorGUILayout.Space(10);

        // Documentation
        EditorGUILayout.LabelField("How to Use", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Assign or create a LevelGeneratorConfig\n" +
            "2. Configure difficulty tiers and progression\n" +
            "3. Click 'Generate Levels' button\n" +
            "4. Levels will be created in the output folder\n" +
            "5. Database will be automatically updated",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    void CreateNewConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Level Generator Config",
            "LevelGeneratorConfig",
            "asset",
            "Create a new Level Generator Config"
        );

        if (string.IsNullOrEmpty(path)) return;

        var newConfig = ScriptableObject.CreateInstance<LevelGeneratorConfig>();
        AssetDatabase.CreateAsset(newConfig, path);
        AssetDatabase.SaveAssets();

        config = newConfig;
        Selection.activeObject = newConfig;

        Debug.Log($"[LevelGenerator] Created new config at: {path}");
    }

    void PreviewLevel(int levelNumber)
    {
        var rng = config.randomSeed > 0 ? new System.Random(config.randomSeed + levelNumber) : new System.Random();

        // Get tier
        var tier = config.GetTierForLevel(levelNumber);
        if (tier == null)
        {
            Debug.LogError($"No tier found for level {levelNumber}");
            return;
        }

        // Calculate requirements
        float progress = config.GetLevelProgress(levelNumber);
        float curveValue = config.requirementCountCurve.Evaluate(progress);
        int requirementCount = Mathf.RoundToInt(Mathf.Lerp(tier.minRequirements, tier.maxRequirements, curveValue));

        Debug.Log($"=== PREVIEW LEVEL {levelNumber} ===");
        Debug.Log($"Tier: {tier.tierName} ({tier.startLevel}-{tier.endLevel})");
        Debug.Log($"Progress: {progress:F2} (Curve: {curveValue:F2})");
        Debug.Log($"Requirements: {requirementCount}");
        Debug.Log($"Fragments:");

        var availableTypes = new System.Collections.Generic.List<FragmentType>(config.availableFragmentTypes);

        for (int i = 0; i < requirementCount; i++)
        {
            if (availableTypes.Count == 0) break;

            int typeIndex = rng.Next(availableTypes.Count);
            FragmentType fragType = availableTypes[typeIndex];
            availableTypes.RemoveAt(typeIndex);

            float amountCurve = config.fragmentAmountCurve.Evaluate(progress);
            int amount = Mathf.RoundToInt(Mathf.Lerp(config.minFragmentAmount, config.maxFragmentAmount, amountCurve));
            float randomFactor = (float)(rng.NextDouble() * 0.4 - 0.2);
            amount = Mathf.RoundToInt(amount * (1f + randomFactor));
            amount = Mathf.Clamp(amount, config.minFragmentAmount, config.maxFragmentAmount);

            int colorVariant = rng.Next(1, 4);

            Debug.Log($"  - {fragType} (Variant {colorVariant}): x{amount}");
        }

        Debug.Log("=========================");
    }
}
#endif