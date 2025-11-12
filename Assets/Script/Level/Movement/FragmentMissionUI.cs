using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// ✅ FIXED: Fragment Mission UI - Maximum 3 boxes
/// </summary>
public class FragmentMissionUI : MonoBehaviour
{
    [Header("Mission Boxes (MAX 3)")]
    [Tooltip("Assign 3 mission box GameObjects. Boxes not needed will be hidden.")]
    public List<MissionBoxUI> missionBoxes = new List<MissionBoxUI>();

    [Header("Registry")]
    public FragmentPrefabRegistry fragmentRegistry;

    [Header("Database")]
    public LevelDatabase levelDatabase;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private FragmentRequirement[] currentRequirements;
    private int[] collectedCounts;

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

        if (levelDatabase != null)
        {
            levelConfig = levelDatabase.GetById(levelId);
            if (levelConfig == null)
                levelConfig = levelDatabase.GetByNumber(levelNum);
        }

        if (levelConfig == null)
        {
            var allConfigs = Resources.FindObjectsOfTypeAll<LevelConfig>();
            levelConfig = allConfigs.FirstOrDefault(c => c.id == levelId || c.number == levelNum);

            if (levelConfig != null && enableDebugLogs)
            {
                Debug.Log($"[FragmentMissionUI] Found LevelConfig via FindObjectsOfTypeAll");
            }
        }

        if (levelConfig == null)
        {
            Debug.LogError($"[FragmentMissionUI] LevelConfig '{levelId}' not found!");
            return;
        }

        if (levelConfig.requirements == null || levelConfig.requirements.Count == 0)
        {
            Debug.LogError($"[FragmentMissionUI] LevelConfig '{levelId}' has no requirements!");
            return;
        }

        // ✅ CRITICAL: LIMIT TO 3 REQUIREMENTS MAXIMUM
        int reqCount = Mathf.Min(levelConfig.requirements.Count, 3);

        if (levelConfig.requirements.Count > 3)
        {
            Debug.LogWarning($"[FragmentMissionUI] Level has {levelConfig.requirements.Count} requirements! Limiting to 3.");
        }

        currentRequirements = new FragmentRequirement[reqCount];
        collectedCounts = new int[reqCount];

        for (int i = 0; i < reqCount; i++)
        {
            currentRequirements[i] = levelConfig.requirements[i];
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[FragmentMissionUI] Loaded {reqCount} requirements (max 3) from {levelConfig.id}");
        }
    }

    void UpdateUI()
    {
        if (currentRequirements == null)
        {
            Debug.LogWarning("[FragmentMissionUI] currentRequirements is null!");
            HideAllBoxes();
            return;
        }

        int reqCount = currentRequirements.Length;

        if (enableDebugLogs)
        {
            Debug.Log($"[FragmentMissionUI] UpdateUI: {reqCount} requirements, {missionBoxes.Count} boxes available");
        }

        // Update each box
        for (int i = 0; i < missionBoxes.Count; i++)
        {
            // Null checks
            if (missionBoxes[i] == null)
            {
                Debug.LogWarning($"[FragmentMissionUI] missionBoxes[{i}] is NULL!");
                continue;
            }

            if (missionBoxes[i].rootObject == null)
            {
                Debug.LogWarning($"[FragmentMissionUI] missionBoxes[{i}].rootObject is NULL!");
                continue;
            }

            if (i < reqCount && currentRequirements[i] != null)
            {
                // Show and update this box
                missionBoxes[i].rootObject.SetActive(true);
                UpdateBox(i, missionBoxes[i]);
            }
            else
            {
                // Hide unused box
                missionBoxes[i].rootObject.SetActive(false);

                if (enableDebugLogs)
                {
                    Debug.Log($"[FragmentMissionUI] Hidden box {i} (not needed)");
                }
            }
        }
    }

    void UpdateBox(int index, MissionBoxUI box)
    {
        if (currentRequirements == null || index >= currentRequirements.Length || currentRequirements[index] == null)
        {
            if (box != null && box.rootObject != null)
                box.rootObject.SetActive(false);
            return;
        }

        var req = currentRequirements[index];

        // Update icon
        if (fragmentRegistry != null && box.iconImage != null)
        {
            GameObject prefab = fragmentRegistry.GetPrefab(req.type, req.colorVariant);
            if (prefab != null)
            {
                SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    box.iconImage.sprite = sr.sprite;
                    box.iconImage.gameObject.SetActive(true);
                }
            }
        }

        // Update count text
        if (box.countText != null)
        {
            int collected = collectedCounts != null && index < collectedCounts.Length ? collectedCounts[index] : 0;
            box.countText.text = $"{collected}/{req.count}";
            box.countText.gameObject.SetActive(true);
        }

        // Activate box
        if (box.rootObject != null)
            box.rootObject.SetActive(true);
    }

    void HideAllBoxes()
    {
        foreach (var box in missionBoxes)
        {
            if (box != null && box.rootObject != null)
            {
                box.rootObject.SetActive(false);
            }
        }
    }

    public void OnFragmentCollected(FragmentType type, int colorVariant)
    {
        if (currentRequirements == null) return;

        bool found = false;

        for (int i = 0; i < currentRequirements.Length; i++)
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
                found = true;
                break;
            }
        }

        if (!found && enableDebugLogs)
        {
            Debug.LogWarning($"[FragmentMissionUI] Collected fragment {type} variant {colorVariant} but not in requirements!");
        }
    }

    void CheckMissionComplete()
    {
        if (currentRequirements == null) return;

        bool allComplete = true;

        for (int i = 0; i < currentRequirements.Length; i++)
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
            Debug.Log("[FragmentMissionUI] 🎉 MISSION COMPLETE!");
            OnMissionComplete();
        }
    }

    void OnMissionComplete()
    {
        string levelId = PlayerPrefs.GetString("SelectedLevelId", "");
        int levelNum = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        var starManager = FindFirstObjectByType<GameplayStarManager>();
        if (starManager != null)
        {
            starManager.CompleteLevelWithStars();
        }
        else
        {
            LevelProgressManager.Instance?.UnlockNextLevel(levelNum);
        }

        if (LevelGameSession.Instance != null)
        {
            Debug.Log("[FragmentMissionUI] ✅ Invoking LevelGameSession.OnLevelCompleted event");
            LevelGameSession.Instance.OnLevelCompleted?.Invoke();
        }
        else
        {
            Debug.LogError("[FragmentMissionUI] ❌ LevelGameSession.Instance is NULL! Cannot invoke OnLevelCompleted!");
        }
    }

    [ContextMenu("Debug: Print Current Requirements")]
    void Context_PrintRequirements()
    {
        if (currentRequirements == null || currentRequirements.Length == 0)
        {
            Debug.Log("[FragmentMissionUI] No requirements loaded");
            return;
        }

        Debug.Log($"=== CURRENT LEVEL REQUIREMENTS ({currentRequirements.Length}/3 max) ===");
        for (int i = 0; i < currentRequirements.Length; i++)
        {
            var req = currentRequirements[i];
            if (req == null) continue;

            int collected = collectedCounts != null && i < collectedCounts.Length ? collectedCounts[i] : 0;
            Debug.Log($"Box {i + 1}: {req.type} variant {req.colorVariant} - {collected}/{req.count}");
        }
    }

    [ContextMenu("Debug: Force Complete Mission")]
    void Context_ForceComplete()
    {
        if (currentRequirements == null) return;

        for (int i = 0; i < currentRequirements.Length; i++)
        {
            if (currentRequirements[i] != null)
            {
                collectedCounts[i] = currentRequirements[i].count;
            }
        }

        UpdateUI();
        CheckMissionComplete();
    }

    [ContextMenu("Debug: Reset Progress")]
    void Context_ResetProgress()
    {
        if (collectedCounts != null)
        {
            for (int i = 0; i < collectedCounts.Length; i++)
            {
                collectedCounts[i] = 0;
            }
        }

        UpdateUI();
        Debug.Log("[FragmentMissionUI] Progress reset");
    }
}

[System.Serializable]
public class MissionBoxUI
{
    [Tooltip("Root GameObject untuk box ini (untuk show/hide)")]
    public GameObject rootObject;

    [Tooltip("Image untuk icon fragment")]
    public Image iconImage;

    [Tooltip("Text untuk count (e.g. '5/10')")]
    public TMP_Text countText;
}