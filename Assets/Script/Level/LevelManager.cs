// Assets/Script/Level/LevelManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// LevelManager
/// - Mengelola definisi level (list dari LevelDefinition yang didefinisikan terpisah)
/// - Menyimpan runtime state (unlocked, bestStars) ke PlayerPrefs
/// - Menyediakan API publik: UnlockNextLevel, UnlockNextLevelByNumber, MarkLevelCompleted, GetState, dll.
/// - Memancarkan event OnLevelsChanged untuk UI agar rebuild/refresh.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // --- Public configurables (assign di Inspector) ---
    [Tooltip("List level definitions. Provide these in the Inspector or load at runtime.")]
    public List<LevelDefinition> levelDefinitions = new List<LevelDefinition>();

    [Tooltip("If true, automatically unlock levelDefinitions[0] for new players.")]
    public bool ensureFirstLevelUnlocked = true;

    // --- Singleton ---
    public static LevelManager Instance { get; private set; }

    // --- Runtime state definitions ---
    // LevelState stores whether level unlocked and best stars achieved
    [Serializable]
    public class LevelState
    {
        public bool unlocked = false;
        public int bestStars = 0;
    }

    // wrapper for saving list to PlayerPrefs via JsonUtility
    [Serializable]
    class SaveWrapper
    {
        public List<SaveEntry> items = new List<SaveEntry>();
    }
    [Serializable]
    class SaveEntry
    {
        public string id;
        public bool unlocked;
        public int bestStars;
    }

    // runtime map
    private Dictionary<string, LevelState> runtimeStates = new Dictionary<string, LevelState>();

    // PlayerPrefs key
    const string PREF_KEY = "Kulino_LevelManager_State_v1";

    // Event to inform UI/other systems to refresh when levels changed
    public event Action OnLevelsChanged;

    // ---------- Unity lifecycle ----------
    void Awake()
    {
        // simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.Log("[LevelManager] Duplicate instance found - destroying this.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // load definitions & state
        LoadState();

        // ensure first unlocked for new installs
        if (ensureFirstLevelUnlocked && levelDefinitions != null && levelDefinitions.Count > 0)
        {
            var first = levelDefinitions[0];
            var st = GetOrCreateState(first.id);
            if (!st.unlocked)
            {
                st.unlocked = true;
                SaveState(); // persist
                OnLevelsChanged?.Invoke();
            }
        }

        Debug.Log($"[LevelManager] Awake - loaded {runtimeStates.Count} saved entries; defs={levelDefinitions?.Count ?? 0}");
    }

    // ---------- Public API ----------

    /// <summary>
    /// Unlock the next level after the given level id.
    /// Safe no-op if id not found or already last level.
    /// </summary>
    public void UnlockNextLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        if (levelDefinitions == null || levelDefinitions.Count == 0) return;

        int idx = levelDefinitions.FindIndex(x => x.id == levelId);
        if (idx < 0) return;

        int nextIdx = idx + 1;
        if (nextIdx >= levelDefinitions.Count) return;

        var next = levelDefinitions[nextIdx];
        var stNext = GetOrCreateState(next.id);
        if (!stNext.unlocked)
        {
            stNext.unlocked = true;
            SaveState();
            Debug.Log($"[LevelManager] UnlockNextLevel -> unlocked {next.id}");
            OnLevelsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Convenience overload - unlock next level by numeric level number.
    /// </summary>
    public void UnlockNextLevelByNumber(int number)
    {
        if (levelDefinitions == null) return;
        var def = levelDefinitions.Find(d => d.number == number);
        if (def != null) UnlockNextLevel(def.id);
    }

    /// <summary>
    /// Mark the level as completed with stars (1..3). This will update bestStars and optionally unlock next level.
    /// </summary>
    public void MarkLevelCompleted(string levelId, int stars)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        var def = levelDefinitions?.Find(d => d.id == levelId);
        if (def == null) return;

        var st = GetOrCreateState(levelId);
        int clamped = Mathf.Clamp(stars, 0, 3);
        if (clamped > st.bestStars)
        {
            st.bestStars = clamped;
            SaveState();
        }

        // automatically unlock next level on first clear (you can change this behaviour)
        UnlockNextLevel(levelId);

        OnLevelsChanged?.Invoke();
        Debug.Log($"[LevelManager] MarkLevelCompleted {levelId} stars={clamped}");
    }

    /// <summary>
    /// Returns LevelState for given id (or null if not found). Use GetOrCreateState if you want guaranteed entry.
    /// </summary>
    public LevelState GetLevelState(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return null;
        runtimeStates.TryGetValue(levelId, out var st);
        return st;
    }

    /// <summary>
    /// Get best stars (0..3) for a level
    /// </summary>
    public int GetBestStars(string levelId)
    {
        var s = GetLevelState(levelId);
        return s != null ? s.bestStars : 0;
    }

    /// <summary>
    /// Returns whether level is unlocked.
    /// </summary>
    public bool IsUnlocked(string levelId)
    {
        var s = GetLevelState(levelId);
        return s != null && s.unlocked;
    }

    /// <summary>
    /// Force unlock (public API)
    /// </summary>
    public void ForceUnlock(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        var s = GetOrCreateState(levelId);
        if (!s.unlocked)
        {
            s.unlocked = true;
            SaveState();
            OnLevelsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Rebuild UI. This is a generic hook: if your LevelManager has a UI build method,
    /// you can call it here. This implementation simply invokes the OnLevelsChanged event.
    /// </summary>
    public void BuildUI()
    {
        // UI should subscribe to OnLevelsChanged and rebuild when event fires.
        OnLevelsChanged?.Invoke();
    }

    // ---------- Persistence ----------

    void SaveState()
    {
        try
        {
            var wrap = new SaveWrapper();
            wrap.items = runtimeStates.Select(kv => new SaveEntry
            {
                id = kv.Key,
                unlocked = kv.Value.unlocked,
                bestStars = kv.Value.bestStars
            }).ToList();

            string json = JsonUtility.ToJson(wrap);
            PlayerPrefs.SetString(PREF_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] SaveState -> {wrap.items.Count} entries");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LevelManager] SaveState failed: " + ex.Message);
        }
    }

    void LoadState()
    {
        runtimeStates.Clear();
        if (!PlayerPrefs.HasKey(PREF_KEY)) return;
        try
        {
            string json = PlayerPrefs.GetString(PREF_KEY, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            var wrap = JsonUtility.FromJson<SaveWrapper>(json);
            if (wrap?.items == null) return;
            foreach (var e in wrap.items)
            {
                var st = new LevelState() { unlocked = e.unlocked, bestStars = e.bestStars };
                runtimeStates[e.id] = st;
            }
            Debug.Log($"[LevelManager] LoadState -> loaded {runtimeStates.Count} entries");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LevelManager] LoadState failed: " + ex.Message);
        }
    }

    // convenience: persist a single level (calls SaveState for now)
    public void SaveState(string levelId)
    {
        SaveState();
    }

    // ---------- Helpers ----------
    private LevelState GetOrCreateState(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!runtimeStates.TryGetValue(id, out var s))
        {
            s = new LevelState();
            runtimeStates[id] = s;
        }
        return s;
    }

    // for debugging: print states
    [ContextMenu("Debug_PrintStates")]
    void Debug_PrintStates()
    {
        Debug.Log($"[LevelManager] Definitions={levelDefinitions?.Count ?? 0}, RuntimeStates={runtimeStates.Count}");
        foreach (var kv in runtimeStates)
        {
            Debug.Log($" - {kv.Key} unlocked={kv.Value.unlocked} bestStars={kv.Value.bestStars}");
        }
    }
}
