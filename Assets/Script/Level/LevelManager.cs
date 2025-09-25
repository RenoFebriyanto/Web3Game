using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.Events;

[Serializable]
public class LevelDefData
{
    public int id;
    public bool locked;
    public int bestStars; // 0..3
    public int[] starLanes; // length = 3, lane index per star (0..laneCount-1)

    public LevelDefData() { } // for JsonUtility

    public LevelDefData(int id, bool locked, int laneCount, System.Random rnd)
    {
        this.id = id;
        this.locked = locked;
        bestStars = 0;
        starLanes = new int[3];
        for (int i = 0; i < 3; i++)
        {
            starLanes[i] = rnd.Next(0, Math.Max(1, laneCount));
        }
    }
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("UI Prefab / Container")]
    public GameObject levelItemPrefab;        // prefab untuk tiap tile level (must have LevelItemUI)
    public RectTransform contentParent;      // Content Rect (GridLayoutGroup parent)

    [Header("Generation")]
    public int levelCount = 50;
    public int gridColumns = 5;              // jumlah kolom di grid layout
    public int initiallyUnlocked = 1;        // jumlah level awal terbuka (biasanya 1)

    [Header("Persistence")]
    public string savePrefix = "LevelDef_v1_"; // PlayerPrefs key prefix

    List<LevelDefData> defs = new List<LevelDefData>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (contentParent == null)
        {
            Debug.LogError("[LevelManager] contentParent is null!");
            return;
        }
        var grid = contentParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, gridColumns);
        }

        GenerateOrLoad();
        BuildUI();
    }

    void GenerateOrLoad()
    {
        defs.Clear();
        System.Random rnd = new System.Random(12345);
        for (int i = 1; i <= levelCount; i++)
        {
            string key = savePrefix + i;
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                try
                {
                    LevelDefData d = JsonUtility.FromJson<LevelDefData>(json);
                    if (d.starLanes == null || d.starLanes.Length != 3)
                    {
                        d.starLanes = new int[3];
                        for (int s = 0; s < 3; s++) d.starLanes[s] = rnd.Next(0, Math.Max(1, gridColumns));
                    }
                    defs.Add(d);
                }
                catch
                {
                    defs.Add(new LevelDefData(i, i > initiallyUnlocked, gridColumns, rnd));
                }
            }
            else
            {
                defs.Add(new LevelDefData(i, i > initiallyUnlocked, gridColumns, rnd));
            }
        }
    }

    void BuildUI()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(contentParent.GetChild(i).gameObject);

        for (int i = 0; i < defs.Count; i++)
        {
            var def = defs[i];
            var go = Instantiate(levelItemPrefab, contentParent);
            var li = go.GetComponent<LevelItemUI>();
            if (li != null)
            {
                li.Setup(def);
                li.onClick.RemoveAllListeners();
                li.onClick.AddListener(() => OnLevelClicked(def.id));
            }
            else
            {
#if TMP_PRESENT
                var t = go.GetComponentInChildren<TMP_Text>();
                if (t != null) t.text = (def.id).ToString();
#else
                var t = go.GetComponentInChildren<Text>();
                if (t != null) t.text = (def.id).ToString();
#endif
            }
        }
    }

    void OnLevelClicked(int levelId)
    {
        var def = GetDefinition(levelId);
        if (def == null) return;
        if (def.locked)
        {
            Debug.Log("[LevelManager] Level locked " + levelId);
            return;
        }

        Debug.Log("[LevelManager] Selected level " + levelId);
        PlayerPrefs.SetInt("SelectedLevelId", levelId);
        PlayerPrefs.Save();

        // NOTE: do NOT auto-load scene here if you manage transitions elsewhere.
        // If you want automatic load: SceneManager.LoadScene("Gameplay");
    }

    public LevelDefData GetDefinition(int levelId)
    {
        if (levelId <= 0 || levelId > defs.Count) return null;
        return defs[levelId - 1];
    }

    public void OnLevelCompleted(int levelId, int starsEarned)
    {
        var def = GetDefinition(levelId);
        if (def == null) return;
        def.bestStars = Math.Max(def.bestStars, Mathf.Clamp(starsEarned, 0, 3));
        if (levelId < levelCount)
        {
            var next = GetDefinition(levelId + 1);
            if (next != null) next.locked = false;
        }
        SaveDefinition(def);
        BuildUI();
    }

    void SaveDefinition(LevelDefData def)
    {
        string key = savePrefix + def.id;
        string json = JsonUtility.ToJson(def);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }
}
