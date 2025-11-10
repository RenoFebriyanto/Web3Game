using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// ✅ CRITICAL FIX: Null safety untuk semua GameObject references
/// </summary>
public class FragmentMissionUI : MonoBehaviour
{
    [Header("Mission Boxes (Assign up to 6 boxes)")]
    [Tooltip("Assign semua mission box GameObjects (max 6). Boxes yang tidak dipakai akan di-hide.")]
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

        int reqCount = levelConfig.requirements.Count;

        if (reqCount > 6)
        {
            Debug.LogWarning($"[FragmentMissionUI] Level has {reqCount} requirements! Max 6 supported. Using first 6.");
            reqCount = 6;
        }

        currentRequirements = new FragmentRequirement[reqCount];
        collectedCounts = new int[reqCount];

        for (int i = 0; i < reqCount && i < levelConfig.requirements.Count; i++)
        {
            currentRequirements[i] = levelConfig.requirements[i];
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[FragmentMissionUI] Loaded {reqCount} requirements from {levelConfig.id}");
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

        // ✅ CRITICAL FIX: Null safety untuk semua operations
        for (int i = 0; i < missionBoxes.Count; i++)
        {
            // ✅ Check if box exists
            if (missionBoxes[i] == null) 
            {
                Debug.LogWarning($"[FragmentMissionUI] missionBoxes[{i}] is NULL!");
                continue;
            }

            // ✅ Check if rootObject exists BEFORE SetActive
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

        // ✅ Update icon with null checks
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

        // ✅ Update count text with null checks
        if (box.countText != null)
        {
            int collected = collectedCounts != null && index < collectedCounts.Length ? collectedCounts[index] : 0;
            box.countText.text = $"{collected}/{req.count}";
            box.countText.gameObject.SetActive(true);
        }

        // ✅ Activate box with null check
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

        Debug.Log($"=== CURRENT LEVEL REQUIREMENTS ({currentRequirements.Length}) ===");
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