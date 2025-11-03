// LevelGameSession.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RequirementChangedEvent : UnityEvent<FragmentType, int, int> { }

[Serializable]
public class LevelStartedEvent : UnityEvent<LevelConfig> { }

/// <summary>
/// FIXED: Properly manages level session and events
/// </summary>
public class LevelGameSession : MonoBehaviour
{
    public static LevelGameSession Instance { get; private set; }

    [Header("Events")]
    public LevelStartedEvent OnLevelStarted;
    public RequirementChangedEvent OnRequirementChanged;
    public UnityEvent OnLevelCompleted;

    [Header("Runtime Status")]
    [HideInInspector] public LevelConfig currentLevel;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    // key = "type_variant"
    Dictionary<string, int> remainingMap = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ CRITICAL: Initialize events if null
        if (OnLevelStarted == null)
            OnLevelStarted = new LevelStartedEvent();

        if (OnRequirementChanged == null)
            OnRequirementChanged = new RequirementChangedEvent();

        if (OnLevelCompleted == null)
            OnLevelCompleted = new UnityEvent();

        if (enableDebugLogs)
        {
            Debug.Log("[LevelGameSession] ✓ Initialized with events");
        }
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
                if (remainingMap.ContainsKey(k))
                    remainingMap[k] += r.count;
                else
                    remainingMap[k] = r.count;
            }
        }

        OnLevelStarted?.Invoke(cfg);

        if (enableDebugLogs)
        {
            Debug.Log($"[LevelGameSession] Level started: {cfg?.displayName}");
        }
    }

    public int GetRemaining(FragmentType t, int variant)
    {
        string k = Key(t, variant);
        if (remainingMap.TryGetValue(k, out var v)) return v;
        return 0;
    }

    /// <summary>
    /// Called when player collects a fragment
    /// Returns true if it affected progress
    /// </summary>
    public bool OnFragmentCollected(FragmentType t, int variant)
    {
        string k = Key(t, variant);
        if (!remainingMap.ContainsKey(k)) return false;

        int rem = remainingMap[k];
        if (rem <= 0) return false;

        rem = Mathf.Max(0, rem - 1);
        remainingMap[k] = rem;

        OnRequirementChanged?.Invoke(t, variant, rem);

        if (enableDebugLogs)
        {
            Debug.Log($"[LevelGameSession] Fragment collected: {t} variant {variant}, remaining: {rem}");
        }

        // Check if all zero -> completed
        bool allZero = true;
        foreach (var kv in remainingMap)
        {
            if (kv.Value > 0)
            {
                allZero = false;
                break;
            }
        }

        if (allZero)
        {
            Debug.Log("[LevelGameSession] ✅ All fragments collected! Invoking OnLevelCompleted");

            // ✅ CRITICAL: Invoke event
            if (OnLevelCompleted != null)
            {
                OnLevelCompleted.Invoke();
                Debug.Log($"[LevelGameSession] ✓ OnLevelCompleted invoked, {OnLevelCompleted.GetPersistentEventCount()} persistent listeners");
            }
            else
            {
                Debug.LogError("[LevelGameSession] ❌ OnLevelCompleted is NULL!");
            }
        }

        return true;
    }

    [ContextMenu("Debug: Print Status")]
    void Context_PrintStatus()
    {
        Debug.Log("=== LEVEL GAME SESSION STATUS ===");
        Debug.Log($"Current Level: {(currentLevel != null ? currentLevel.displayName : "NONE")}");
        Debug.Log($"Remaining fragments:");
        foreach (var kv in remainingMap)
        {
            Debug.Log($"  {kv.Key}: {kv.Value}");
        }
        Debug.Log($"OnLevelCompleted listeners: {(OnLevelCompleted != null ? OnLevelCompleted.GetPersistentEventCount() : 0)}");
        Debug.Log("=================================");
    }

    [ContextMenu("Debug: Force Complete Level")]
    void Context_ForceComplete()
    {
        Debug.Log("[LevelGameSession] Force completing level...");

        // Set all remaining to 0
        var keys = new List<string>(remainingMap.Keys);
        foreach (var k in keys)
        {
            remainingMap[k] = 0;
        }

        // Invoke event
        if (OnLevelCompleted != null)
        {
            Debug.Log("[LevelGameSession] Invoking OnLevelCompleted");
            OnLevelCompleted.Invoke();
        }
        else
        {
            Debug.LogError("[LevelGameSession] OnLevelCompleted is NULL!");
        }
    }
}