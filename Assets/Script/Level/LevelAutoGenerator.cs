// Assets/Script/Level/LevelAutoGenerator.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelMission
{
    public enum MissionType
    {
        CollectCoins,
        CollectCrystals,
        SurviveDistance,
        AvoidPlanets,
        CollectStars
    }

    public MissionType type;
    public int targetAmount;
    public string description;

    public LevelMission(MissionType missionType, int amount)
    {
        type = missionType;
        targetAmount = amount;
        description = GetMissionDescription(missionType, amount);
    }

    private string GetMissionDescription(MissionType missionType, int amount)
    {
        switch (missionType)
        {
            case MissionType.CollectCoins:
                return $"Collect {amount} coins";
            case MissionType.CollectCrystals:
                return $"Collect {amount} crystals";
            case MissionType.SurviveDistance:
                return $"Travel {amount} meters";
            case MissionType.AvoidPlanets:
                return $"Avoid {amount} planets";
            case MissionType.CollectStars:
                return $"Collect {amount} stars";
            default:
                return $"Complete objective: {amount}";
        }
    }
}

[System.Serializable]
public class ExtendedLevelData : LevelDefinition
{
    public LevelMission mission;
    public int worldNumber; // untuk grouping level (setiap 10 level = 1 world)
    public float difficulty; // 1.0 = easy, 5.0 = very hard
    public Color levelColor = Color.yellow;

    public ExtendedLevelData(string id, int number, LevelMission.MissionType missionType, int missionAmount, bool locked = true)
        : base(id, number, locked, missionAmount)
    {
        mission = new LevelMission(missionType, missionAmount);
        worldNumber = (number - 1) / 10 + 1; // Level 1-10 = World 1, etc.
        difficulty = CalculateDifficulty(number);
        levelColor = GetWorldColor(worldNumber);

        // Set star thresholds based on mission and difficulty
        SetStarThresholds(missionType, missionAmount, difficulty);
    }

    private float CalculateDifficulty(int levelNumber)
    {
        // Difficulty increases gradually
        return Mathf.Clamp(1.0f + (levelNumber - 1) * 0.1f, 1.0f, 5.0f);
    }

    private Color GetWorldColor(int world)
    {
        Color[] worldColors = {
            new Color(1f, 0.9f, 0.3f, 1f), // Yellow - World 1
            new Color(0.3f, 0.9f, 1f, 1f), // Light Blue - World 2
            new Color(0.9f, 0.3f, 1f, 1f), // Purple - World 3
            new Color(1f, 0.5f, 0.3f, 1f), // Orange - World 4
            new Color(0.3f, 1f, 0.5f, 1f), // Green - World 5
        };
        return worldColors[(world - 1) % worldColors.Length];
    }

    private void SetStarThresholds(LevelMission.MissionType missionType, int amount, float difficulty)
    {
        switch (missionType)
        {
            case LevelMission.MissionType.CollectCoins:
                starThreshold1 = Mathf.RoundToInt(amount * 0.3f);
                starThreshold2 = Mathf.RoundToInt(amount * 0.6f);
                starThreshold3 = amount;
                break;
            case LevelMission.MissionType.SurviveDistance:
                starThreshold1 = Mathf.RoundToInt(amount * 0.5f);
                starThreshold2 = Mathf.RoundToInt(amount * 0.8f);
                starThreshold3 = amount;
                break;
            default:
                starThreshold1 = Mathf.RoundToInt(amount * 0.4f);
                starThreshold2 = Mathf.RoundToInt(amount * 0.7f);
                starThreshold3 = amount;
                break;
        }

        // Ensure minimum values
        starThreshold1 = Mathf.Max(starThreshold1, 1);
        starThreshold2 = Mathf.Max(starThreshold2, starThreshold1 + 1);
        starThreshold3 = Mathf.Max(starThreshold3, starThreshold2 + 1);
    }
}

public class LevelAutoGenerator : MonoBehaviour
{
    [Header("Auto Generation Settings")]
    [SerializeField] private bool autoGenerateOnStart = true;
    [SerializeField] private int totalLevels = 50;

    [Header("Mission Weights (higher = more common)")]
    [SerializeField] private int coinMissionWeight = 30;
    [SerializeField] private int crystalMissionWeight = 25;
    [SerializeField] private int distanceMissionWeight = 20;
    [SerializeField] private int avoidMissionWeight = 15;
    [SerializeField] private int starMissionWeight = 10;

    [Header("Debug")]
    [SerializeField] private bool showGenerationLog = true;

    private System.Random rng = new System.Random();

    void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateAllLevels();
        }
    }

    [ContextMenu("Generate All Levels")]
    public void GenerateAllLevels()
    {
        var levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("[LevelAutoGenerator] LevelManager component not found!");
            return;
        }

        levelManager.levelDefinitions.Clear();

        for (int i = 1; i <= totalLevels; i++)
        {
            var levelData = GenerateLevel(i);
            levelManager.levelDefinitions.Add(levelData);

            if (showGenerationLog)
            {
                Debug.Log($"[LevelAutoGenerator] Generated Level {i}: {levelData.mission.description}");
            }
        }

        // Ensure first level is unlocked
        if (levelManager.levelDefinitions.Count > 0)
        {
            levelManager.levelDefinitions[0].locked = false;
            levelManager.levelDefinitions[0].unlocked = true;
        }

        Debug.Log($"[LevelAutoGenerator] Successfully generated {totalLevels} levels!");

        // Refresh UI if LevelManager has BuildUI method
        levelManager.BuildUI();
    }

    private ExtendedLevelData GenerateLevel(int levelNumber)
    {
        string levelId = $"level_{levelNumber}";
        var missionType = GetRandomMissionType();
        int missionAmount = GetMissionAmount(missionType, levelNumber);
        bool locked = levelNumber > 1; // Only first level unlocked

        return new ExtendedLevelData(levelId, levelNumber, missionType, missionAmount, locked);
    }

    private LevelMission.MissionType GetRandomMissionType()
    {
        List<LevelMission.MissionType> weightedPool = new List<LevelMission.MissionType>();

        // Add mission types based on weights
        for (int i = 0; i < coinMissionWeight; i++)
            weightedPool.Add(LevelMission.MissionType.CollectCoins);
        for (int i = 0; i < crystalMissionWeight; i++)
            weightedPool.Add(LevelMission.MissionType.CollectCrystals);
        for (int i = 0; i < distanceMissionWeight; i++)
            weightedPool.Add(LevelMission.MissionType.SurviveDistance);
        for (int i = 0; i < avoidMissionWeight; i++)
            weightedPool.Add(LevelMission.MissionType.AvoidPlanets);
        for (int i = 0; i < starMissionWeight; i++)
            weightedPool.Add(LevelMission.MissionType.CollectStars);

        return weightedPool[rng.Next(weightedPool.Count)];
    }

    private int GetMissionAmount(LevelMission.MissionType missionType, int levelNumber)
    {
        // Base amounts that increase with level
        float levelMultiplier = 1.0f + (levelNumber - 1) * 0.1f;

        switch (missionType)
        {
            case LevelMission.MissionType.CollectCoins:
                return Mathf.RoundToInt(Random.Range(15, 30) * levelMultiplier);

            case LevelMission.MissionType.CollectCrystals:
                return Mathf.RoundToInt(Random.Range(8, 15) * levelMultiplier);

            case LevelMission.MissionType.SurviveDistance:
                return Mathf.RoundToInt(Random.Range(100, 200) * levelMultiplier);

            case LevelMission.MissionType.AvoidPlanets:
                return Mathf.RoundToInt(Random.Range(20, 40) * levelMultiplier);

            case LevelMission.MissionType.CollectStars:
                return Mathf.RoundToInt(Random.Range(5, 10) * levelMultiplier);

            default:
                return Mathf.RoundToInt(20 * levelMultiplier);
        }
    }

    // Method untuk mendapatkan level data sebagai ExtendedLevelData
    public ExtendedLevelData GetExtendedLevelData(string levelId)
{
    var levelManager = GetComponent<LevelManager>();
    if (levelManager == null) return null;

    var levelDef = levelManager.levelDefinitions.Find(x => x.id == levelId);
    if (levelDef is ExtendedLevelData extendedData)
    {
        return extendedData;
    }
    Debug.LogWarning($"[LevelAutoGenerator] Level {levelId} bukan ExtendedLevelData");
    return null;
}

    // Method untuk mendapatkan mission dari level ID
    public LevelMission GetLevelMission(string levelId)
    {
        var extendedData = GetExtendedLevelData(levelId);
        return extendedData?.mission;
    }
}