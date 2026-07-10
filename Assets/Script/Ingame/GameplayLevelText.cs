using UnityEngine;
using TMPro;

/// <summary>
/// Script untuk mengatur UI gameplay:
/// - Level name
/// - Level number
/// - Timer
/// </summary>
public class GameplayLevelText : MonoBehaviour
{
    [Header("Level Display")]

    [Tooltip("Text pertama untuk tampilan level")]
    public TMP_Text levelName;

    [Tooltip("Text kedua untuk tampilan level")]
    public TMP_Text levelText;

    [Tooltip("Format level (gunakan {0} untuk nomor level)")]
    public string textFormat = "Level {0}";



    [Header("Timer")]
    public GameplayTimer timerScript;

    public TMP_Text timerText;

    public bool enableTimer;

    private float timer;


    [Header("Auto Find")]

    public bool autoFindComponent = true;


    [Header("Debug")]

    public bool enableDebugLogs = false;


    private int currentLevelNumber = 0;


    private void Awake()
    {
        if (autoFindComponent)
        {
            if (levelText == null)
            {
                levelText = GetComponent<TMP_Text>();
            }

            if (levelText == null)
            {
                levelText = GetComponentInChildren<TMP_Text>();
            }
        }


        if (levelText == null && levelName == null)
        {
            LogWarning("Tidak ada TMP_Text level yang ditemukan!");
        }
    }


    private void Start()
    {
        UpdateLevelText();

        timer = 0;
        enableTimer = true;

        if (timerText != null)
        {
            timerText.text = "";
        }
        timerScript.StartTimer();
    }


    private void Update()
    {
        if (enableTimer)
        {
            UpdateTimer();
        }
    }



    // =========================
    // LEVEL DISPLAY
    // =========================

    public void UpdateLevelText()
    {
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        UpdateLevelDisplay();
    }


    public void SetLevelNumber(int levelNumber)
    {
        currentLevelNumber = levelNumber;

        UpdateLevelDisplay();
    }


    private void UpdateLevelDisplay()
    {
        string formattedText = string.Format(
            textFormat,
            currentLevelNumber
        );


        if (levelText != null)
        {
            levelText.text = formattedText;
        }


        if (levelName != null)
        {
            levelName.text = formattedText;
        }


        Log($"Level updated: {formattedText}");
    }


    public int GetCurrentLevelNumber()
    {
        return currentLevelNumber;
    }



    // =========================
    // TIMER
    // =========================

    private void UpdateTimer()
    {
        timer += Time.deltaTime;


        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);
        int milliseconds = Mathf.FloorToInt((timer * 100) % 100);


        if (timer < 60)
        {
            // detik.milidetik
            timerText.text = $"{seconds}.{milliseconds:00}";
        }
        else
        {
            // menit.detik
            timerText.text = $"{minutes}.{seconds:00}";
        }
    }


    public void StartTimer()
    {
        timer = 0;
        enableTimer = true;


        if (timerText != null)
        {
            timerText.text = "0.00";
        }


        Log("Timer started");
    }


    public void StopTimer()
    {
        enableTimer = false;

        Log($"Timer stopped: {timer}");
    }


    public void ResetTimer()
    {
        timer = 0;


        if (timerText != null)
        {
            timerText.text = "0.00";
        }
    }


    public float GetTimer()
    {
        return timer;
    }



    // =========================
    // DEBUG
    // =========================

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GameplayLevelText] {message}");
        }
    }


    private void LogWarning(string message)
    {
        Debug.LogWarning($"[GameplayLevelText] {message}");
    }



    // =========================
    // TEST
    // =========================

    [ContextMenu("Test: Update Level")]
    private void TestUpdateLevel()
    {
        UpdateLevelText();
    }


    [ContextMenu("Test: Set Level 10")]
    private void TestSetLevel10()
    {
        SetLevelNumber(10);
    }


    [ContextMenu("Test: Start Timer")]
    private void TestStartTimer()
    {
        StartTimer();
    }
}