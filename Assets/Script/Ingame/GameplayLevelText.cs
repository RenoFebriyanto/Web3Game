using UnityEngine;
using TMPro;

/// <summary>
/// Script untuk update text level number di gameplay
/// Attach ke GameObject "LevelText" di hierarchy
/// </summary>
public class GameplayLevelText : MonoBehaviour
{
    [Header("Text Component")]
    [Tooltip("Drag TMP_Text component dari LevelText GameObject")]
    public TMP_Text levelText;

    [Header("Format Settings")]
    [Tooltip("Format text (gunakan {0} untuk level number)")]
    public string textFormat = "Lv. {0}";

    [Header("Auto-Find")]
    [Tooltip("Jika true, akan auto-find TMP_Text component")]
    public bool autoFindComponent = true;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private int currentLevelNumber = 0;

    void Awake()
    {
        // Auto-find component if needed
        if (autoFindComponent && levelText == null)
        {
            levelText = GetComponent<TMP_Text>();

            if (levelText == null)
            {
                levelText = GetComponentInChildren<TMP_Text>();
            }

            if (levelText != null)
            {
                Log("✓ Auto-found TMP_Text component");
            }
            else
            {
                LogWarning("❌ TMP_Text component not found! Please assign manually in Inspector.");
            }
        }
    }

    void Start()
    {
        UpdateLevelText();
    }

    /// <summary>
    /// Update text dengan level number dari PlayerPrefs
    /// </summary>
    public void UpdateLevelText()
    {
        if (levelText == null)
        {
            LogWarning("levelText is NULL! Cannot update.");
            return;
        }

        // Get current level number from PlayerPrefs
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        // Format and set text
        string formattedText = string.Format(textFormat, currentLevelNumber);
        levelText.text = formattedText;

        Log($"✓ Updated level text: '{formattedText}' (Level {currentLevelNumber})");
    }

    /// <summary>
    /// Manual update dengan level number spesifik
    /// </summary>
    public void SetLevelNumber(int levelNumber)
    {
        if (levelText == null)
        {
            LogWarning("levelText is NULL! Cannot update.");
            return;
        }

        currentLevelNumber = levelNumber;
        string formattedText = string.Format(textFormat, currentLevelNumber);
        levelText.text = formattedText;

        Log($"✓ Manually set level text: '{formattedText}' (Level {currentLevelNumber})");
    }

    /// <summary>
    /// Get current level number
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        return currentLevelNumber;
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[GameplayLevelText] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[GameplayLevelText] {message}");
    }

    // ========================================
    // CONTEXT MENU FOR TESTING
    // ========================================

    [ContextMenu("Test: Update Level Text")]
    void TestUpdateLevelText()
    {
        UpdateLevelText();
    }

    [ContextMenu("Test: Set Level 30")]
    void TestSetLevel30()
    {
        SetLevelNumber(30);
    }

    [ContextMenu("Test: Set Level 100")]
    void TestSetLevel100()
    {
        SetLevelNumber(100);
    }
}