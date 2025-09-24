// Assets/Script/Level/LevelManager.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LevelManager : MonoBehaviour
{
    [Header("UI Prefab / Container")]
    public GameObject levelItemPrefab;    // prefab reference (UI)
    public Transform contentParent;       // ContentLevel (where items will be instantiated)

    [Header("Generation")]
    public int levelCount = 50;
    public int initiallyUnlocked = 1;     // how many levels unlocked at start

    // internal data
    public List<LevelDefinition> definitions = new List<LevelDefinition>();

    void Start()
    {
        if (levelItemPrefab == null || contentParent == null)
        {
            Debug.LogError("[LevelManager] Prefab or ContentParent missing");
            return;
        }

        GenerateDefinitionsIfNeeded();
        GenerateUI();
    }

    void GenerateDefinitionsIfNeeded()
    {
        if (definitions == null) definitions = new List<LevelDefinition>();
        if (definitions.Count >= levelCount) return;

        definitions.Clear();
        for (int i = 1; i <= levelCount; i++)
        {
            var def = new LevelDefinition
            {
                id = $"level_{i}",
                number = i,
                locked = i > initiallyUnlocked,
                targetDistanceMeters = 10 + (i - 1) * 10 // example growth (10,20,30,...)
            };
            definitions.Add(def);
        }
    }

    void GenerateUI()
    {
        // clear existing children
        for (int i = contentParent.childCount - 1; i >= 0; i--) DestroyImmediate(contentParent.GetChild(i).gameObject);

        foreach (var def in definitions)
        {
            var go = Instantiate(levelItemPrefab, contentParent);
            var item = go.GetComponent<LevelItemRow>();
            if (item == null)
            {
                Debug.LogError("[LevelManager] LevelItemPrefab missing LevelItemRow component");
                continue;
            }
            item.Setup(def);
        }
    }

    // helper: unlock next level after a level completed
    public void UnlockNextLevel(int number)
    {
        int next = number + 1;
        var def = definitions.Find(x => x.number == next);
        if (def != null && def.locked)
        {
            def.locked = false;
            SaveDefinitionsState();
            // refresh UI: re-generate or update that item
            GenerateUI();
        }
    }

    #region Save/Load locked state & stars (store only minimal)
    const string PREF_LEVEL_LOCKED_PREFIX = "Kulino_LevelLocked_";
    void SaveDefinitionsState()
    {
        foreach (var d in definitions)
        {
            PlayerPrefs.SetInt(PREF_LEVEL_LOCKED_PREFIX + d.id, d.locked ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    void LoadDefinitionsState()
    {
        foreach (var d in definitions)
        {
            if (PlayerPrefs.HasKey(PREF_LEVEL_LOCKED_PREFIX + d.id))
                d.locked = PlayerPrefs.GetInt(PREF_LEVEL_LOCKED_PREFIX + d.id, d.locked ? 1 : 0) == 1;
        }
    }
    #endregion
}
