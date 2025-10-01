// LevelGameSession.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RequirementChangedEvent : UnityEvent<FragmentType, int, int> { }
// args: fragType, variant, remainingCount

[Serializable]
public class LevelStartedEvent : UnityEvent<LevelConfig> { }

public class LevelGameSession : MonoBehaviour
{
    public static LevelGameSession Instance { get; private set; }

    [Header("Events")]
    public LevelStartedEvent OnLevelStarted;
    public RequirementChangedEvent OnRequirementChanged;
    public UnityEvent OnLevelCompleted;

    [HideInInspector] public LevelConfig currentLevel;

    // key = "type_variant"
    Dictionary<string, int> remainingMap = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    static string Key(FragmentType t, int variant) => $"{t}_{variant}";

    public void StartLevel(LevelConfig cfg)
    {
        currentLevel = cfg;
        remainingMap.Clear();
        if (cfg != null)
        {
            foreach (var r in cfg.requirements)
            {
                string k = Key(r.type, r.colorVariant);
                if (remainingMap.ContainsKey(k)) remainingMap[k] += r.count;
                else remainingMap[k] = r.count;
            }
        }
        OnLevelStarted?.Invoke(cfg);
    }

    public int GetRemaining(FragmentType t, int variant)
    {
        string k = Key(t, variant);
        if (remainingMap.TryGetValue(k, out var v)) return v;
        return 0;
    }

    // Called when player collects a fragment (returns true if it affected progress)
    public bool OnFragmentCollected(FragmentType t, int variant)
    {
        string k = Key(t, variant);
        if (!remainingMap.ContainsKey(k)) return false;
        int rem = remainingMap[k];
        if (rem <= 0) return false;
        rem = Mathf.Max(0, rem - 1);
        remainingMap[k] = rem;
        OnRequirementChanged?.Invoke(t, variant, rem);

        // if all zero -> completed
        bool allZero = true;
        foreach (var kv in remainingMap)
        {
            if (kv.Value > 0) { allZero = false; break; }
        }
        if (allZero)
        {
            Debug.Log("[LevelGameSession] Level completed!");
            OnLevelCompleted?.Invoke();
        }
        return true;
    }
}
