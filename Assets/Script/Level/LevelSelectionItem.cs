using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class LevelSelectionItem : MonoBehaviour
{
    [Header("Assign")]
    public LevelConfig levelConfig;
    public TMP_Text numberText;
    public GameObject lockedOverlay;
    public Image bgImage;
    public string gameplaySceneName = "Gameplay";

    [Header("Stars (assign 3 star objects)")]
    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    void Start()
    {
        // ✅ FIXED: Check if levelConfig assigned before refresh
        if (levelConfig != null)
        {
            Refresh();
        }
        else
        {
            Debug.LogWarning($"[LevelSelectionItem] {gameObject.name}: levelConfig is NULL! Cannot refresh.");
        }
    }

    public void Refresh()
    {
        // ✅ CRITICAL: Null check
        if (levelConfig == null)
        {
            Debug.LogWarning($"[LevelSelectionItem] levelConfig not set on {gameObject.name}");
            return;
        }

        // Check if unlocked
        bool unlocked = LevelProgressManager.Instance != null ?
                        LevelProgressManager.Instance.IsUnlocked(levelConfig.number) :
                        (levelConfig.number == 1);

        // ✅ NEW: Update number text visibility based on locked state
        if (numberText != null)
        {
            numberText.text = levelConfig.number.ToString();
            numberText.gameObject.SetActive(unlocked); // Hide text when locked
        }

        // ✅ NEW: Update locked overlay visibility
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!unlocked); // Show overlay when locked
        }

        // Update button interactable
        if (btn != null)
        {
            btn.interactable = unlocked;
        }

        // ✅ Refresh stars
        RefreshStars();

        Debug.Log($"[LevelSelectionItem] ✓ {levelConfig.id} refreshed (unlocked: {unlocked}, text visible: {unlocked})");
    }

    void RefreshStars()
    {
        // Hide all stars by default
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        // ✅ Check if levelConfig exists
        if (levelConfig == null) return;

        // Get earned stars
        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(levelConfig.id);
        }

        // Show stars based on earned amount
        if (earnedStars >= 1 && star1 != null) star1.SetActive(true);
        if (earnedStars >= 2 && star2 != null) star2.SetActive(true);
        if (earnedStars >= 3 && star3 != null) star3.SetActive(true);
    }

    void OnClicked()
    {
        // ✅ CRITICAL: Null check before load scene
        if (levelConfig == null)
        {
            Debug.LogError("[LevelSelectionItem] Cannot start level: levelConfig is NULL!");
            return;
        }

        PlayerPrefs.SetString("SelectedLevelId", levelConfig.id);
        PlayerPrefs.SetInt("SelectedLevelNumber", levelConfig.number);
        PlayerPrefs.Save();

        Debug.Log($"[LevelSelectionItem] Loading {levelConfig.id} ({levelConfig.displayName})");
        SceneManager.LoadScene(gameplaySceneName);
    }
}