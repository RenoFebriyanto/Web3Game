// Assets/Script/Movement/FragmentMissionUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class FragmentMissionUI : MonoBehaviour
{
    [Header("3 Mission Boxes")]
    public Image box1Image;
    public Image box2Image;
    public Image box3Image;

    public TMP_Text box1CountText;
    public TMP_Text box2CountText;
    public TMP_Text box3CountText;

    [Header("Registry")]
    public FragmentPrefabRegistry fragmentRegistry;

    [Header("Database")]
    public LevelDatabase levelDatabase;

    private FragmentRequirement[] currentRequirements;
    private int[] collectedCounts = new int[3];

    void Start()
    {
        LoadCurrentLevelRequirements();
        UpdateUI();
    }

    void LoadCurrentLevelRequirements()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        int levelNum = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        LevelConfig levelConfig = null;

        // Method 1: Dari database
        if (levelDatabase != null)
        {
            levelConfig = levelDatabase.GetById(levelId);
            if (levelConfig == null)
                levelConfig = levelDatabase.GetByNumber(levelNum);
        }

        // Method 2: FindObjectsOfType untuk find semua LevelConfig di scene/project
        if (levelConfig == null)
        {
            var allConfigs = Resources.FindObjectsOfTypeAll<LevelConfig>();
            levelConfig = allConfigs.FirstOrDefault(c => c.id == levelId || c.number == levelNum);

            if (levelConfig != null)
            {
                Debug.Log($"[FragmentMissionUI] Found LevelConfig via FindObjectsOfTypeAll");
            }
        }

        if (levelConfig == null)
        {
            Debug.LogError($"[FragmentMissionUI] LevelConfig '{levelId}' not found anywhere!");
            Debug.LogError($"Database assigned: {(levelDatabase != null)}");
            if (levelDatabase != null)
            {
                Debug.LogError($"Database has {levelDatabase.levels.Count} levels");
            }
            return;
        }

        // Load requirements
        if (levelConfig.requirements == null || levelConfig.requirements.Count == 0)
        {
            Debug.LogError($"[FragmentMissionUI] LevelConfig '{levelId}' has no requirements!");
            return;
        }

        currentRequirements = new FragmentRequirement[3];
        for (int i = 0; i < 3 && i < levelConfig.requirements.Count; i++)
        {
            currentRequirements[i] = levelConfig.requirements[i];
        }

        collectedCounts = new int[3];
        Debug.Log($"[FragmentMissionUI] Loaded {levelConfig.requirements.Count} requirements from {levelConfig.id}");
    }

    void UpdateUI()
    {
        UpdateBox(0, box1Image, box1CountText);
        UpdateBox(1, box2Image, box2CountText);
        UpdateBox(2, box3Image, box3CountText);
    }

    void UpdateBox(int index, Image boxImage, TMP_Text countText)
    {
        if (currentRequirements == null || index >= currentRequirements.Length || currentRequirements[index] == null)
        {
            if (boxImage != null) boxImage.gameObject.SetActive(false);
            if (countText != null) countText.gameObject.SetActive(false);
            return;
        }

        var req = currentRequirements[index];

        if (fragmentRegistry != null && boxImage != null)
        {
            GameObject prefab = fragmentRegistry.GetPrefab(req.type, req.colorVariant);
            if (prefab != null)
            {
                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    boxImage.sprite = sr.sprite;
                    boxImage.gameObject.SetActive(true);
                }
            }
        }

        if (countText != null)
        {
            countText.text = $"{collectedCounts[index]}/{req.count}";
            countText.gameObject.SetActive(true);
        }
    }

    public void OnFragmentCollected(FragmentType type, int colorVariant)
    {
        if (currentRequirements == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (currentRequirements[i] == null) continue;

            if (currentRequirements[i].type == type &&
                currentRequirements[i].colorVariant == colorVariant)
            {
                collectedCounts[i]++;
                if (collectedCounts[i] > currentRequirements[i].count)
                    collectedCounts[i] = currentRequirements[i].count;

                UpdateUI();
                CheckMissionComplete();
                break;
            }
        }
    }

    void CheckMissionComplete()
    {
        if (currentRequirements == null) return;

        bool allComplete = true;

        for (int i = 0; i < 3; i++)
        {
            if (currentRequirements[i] == null) continue;
            if (collectedCounts[i] < currentRequirements[i].count)
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete)
        {
            Debug.Log("MISSION COMPLETE!");
            OnMissionComplete();
        }
    }

    void OnMissionComplete()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "");
        int levelNum = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        LevelProgressManager.Instance?.UnlockNextLevel(levelNum);
    }
}