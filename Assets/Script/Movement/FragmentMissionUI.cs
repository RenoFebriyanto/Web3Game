// Assets/Script/Movement/FragmentMissionUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        LevelConfig config = Resources.Load<LevelConfig>($"Levels/{levelId}");

        if (config == null)
        {
            Debug.LogError($"LevelConfig not found: {levelId}");
            return;
        }

        currentRequirements = new FragmentRequirement[3];
        for (int i = 0; i < 3 && i < config.requirements.Count; i++)
        {
            currentRequirements[i] = config.requirements[i];
        }

        collectedCounts = new int[3];
    }

    void UpdateUI()
    {
        UpdateBox(0, box1Image, box1CountText);
        UpdateBox(1, box2Image, box2CountText);
        UpdateBox(2, box3Image, box3CountText);
    }

    void UpdateBox(int index, Image boxImage, TMP_Text countText)
    {
        if (currentRequirements[index] == null)
        {
            if (boxImage != null) boxImage.gameObject.SetActive(false);
            return;
        }

        var req = currentRequirements[index];

        if (fragmentRegistry != null)
        {
            GameObject prefab = fragmentRegistry.GetPrefab(req.type, req.colorVariant);
            if (prefab != null && boxImage != null)
            {
                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) boxImage.sprite = sr.sprite;
            }
        }

        if (countText != null)
            countText.text = $"{collectedCounts[index]}/{req.count}";
    }

    public void OnFragmentCollected(FragmentType type, int colorVariant)
    {
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