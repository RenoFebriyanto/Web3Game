using System;
using UnityEngine;

/// <summary>
/// Singleton runtime tracker untuk progress yang digunakan oleh quest (login days, play time, coins collected, distance).
/// Integrasikan call dari InGameManager, Coin pickup, Player movement, dan login logic.
/// </summary>
public class QuestProgress : MonoBehaviour
{
    public static QuestProgress Instance { get; private set; }

    // persisted
    const string KEY_LOGIN_DAYS = "player_login_days";
    const string KEY_WEEKLY_CYCLE = "quest_weekly_cycle";
    const string KEY_WEEKLY_RESET_TS = "quest_weekly_reset_ts";

    // runtime (not persisted except login days and weekly cycle/time)
    public int LoginDays { get; private set; } = 0;
    public float PlayTimeSeconds { get; private set; } = 0f; // accumulate per session
    public long CoinsCollectedSinceStart { get; private set; } = 0;
    public float DistanceMeters { get; private set; } = 0f;

    public event Action OnProgressChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoginDays = PlayerPrefs.GetInt(KEY_LOGIN_DAYS, 0);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pastikan root object
        if (transform.parent != null)
        {
            Debug.LogWarning($"[{GetType().Name}] Should be root object for DontDestroyOnLoad");
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    // Call this when user logs in for the day (login detection logic elsewhere)
    public void AddLoginDay(int delta = 1)
    {
        LoginDays = Mathf.Max(0, LoginDays + delta);
        PlayerPrefs.SetInt(KEY_LOGIN_DAYS, LoginDays);
        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    // Call regularly from InGameManager (e.g. in Update while in-game)
    public void AddPlayTime(float seconds)
    {
        if (seconds <= 0f) return;
        PlayTimeSeconds += seconds;
        OnProgressChanged?.Invoke();
    }

    // Call when coin pickup happens (pass the coin amount)
    public void AddCoinsCollected(long amount)
    {
        if (amount == 0) return;
        CoinsCollectedSinceStart = Math.Max(0, CoinsCollectedSinceStart + amount);
        OnProgressChanged?.Invoke();
    }

    // Call when player travels (delta meters). Could be from player rigidbody movement or track Y diff.
    public void AddDistance(float meters)
    {
        if (meters == 0f) return;
        DistanceMeters += Mathf.Max(0f, meters);
        OnProgressChanged?.Invoke();
    }

    // Weekly cycle control (used by QuestSpawner)
    public int GetWeeklyCycleId()
    {
        return PlayerPrefs.GetInt(KEY_WEEKLY_CYCLE, 0);
    }

    // returns true if we created a new cycle (i.e. reset)
    public bool EnsureWeeklyCycleUpToDate(int daysBetween = 7)
    {
        long lastTs = PlayerPrefs.GetInt(KEY_WEEKLY_RESET_TS, 0);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // if never set or older than daysBetween -> start new cycle
        if (lastTs == 0 || (now - lastTs) >= daysBetween * 86400)
        {
            int cycle = PlayerPrefs.GetInt(KEY_WEEKLY_CYCLE, 0);
            cycle++;
            PlayerPrefs.SetInt(KEY_WEEKLY_CYCLE, cycle);
            PlayerPrefs.SetInt(KEY_WEEKLY_RESET_TS, (int)now);
            PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
            return true;
        }
        return false;
    }

    // helper to get simple values for checks (quest uses these)
    public int GetLoginDays() => LoginDays;
    public float GetPlayTimeSeconds() => PlayTimeSeconds;
    public long GetCoinsCollected() => CoinsCollectedSinceStart;
    public float GetDistanceMeters() => DistanceMeters;

    // Optional debug helpers (call from console)
    [ContextMenu("Debug_Add100CoinsCollected")]
    public void DebugAddCoins100() { AddCoinsCollected(100); }

    [ContextMenu("Debug_Add60sPlay")]
    public void DebugAdd60s() { AddPlayTime(60); }

    [ContextMenu("Debug_Add10mDistance")]
    public void DebugAdd10() { AddDistance(10f); }
}
