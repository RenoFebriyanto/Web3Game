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

    void Start() => Refresh();

    public void Refresh()
    {
        if (levelConfig == null)
        {
            Debug.LogWarning("[LevelSelectionItem] levelConfig not set on " + gameObject.name);
            return;
        }

        if (numberText != null) numberText.text = levelConfig.number.ToString();

        bool unlocked = LevelProgressManager.Instance != null ?
                        LevelProgressManager.Instance.IsUnlocked(levelConfig.number) :
                        (levelConfig.number == 1);

        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
        if (btn != null) btn.interactable = unlocked;

        // BARU: Refresh stars
        RefreshStars();
    }

    void RefreshStars()
    {
        // Hide all stars by default
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        // Get earned stars
        int earnedStars = 0;
        if (levelConfig != null && LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(levelConfig.id);
        }

        // Show stars based on earned amount
        if (earnedStars >= 1 && star1 != null) star1.SetActive(true);
        if (earnedStars >= 2 && star2 != null) star2.SetActive(true);
        if (earnedStars >= 3 && star3 != null) star3.SetActive(true);

        Debug.Log($"[LevelSelectionItem] {levelConfig.id}: {earnedStars} stars");
    }

    void OnClicked()
    {
        if (levelConfig == null) return;
        PlayerPrefs.SetString("SelectedLevelId", levelConfig.id);
        PlayerPrefs.SetInt("SelectedLevelNumber", levelConfig.number);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameplaySceneName);
    }
}