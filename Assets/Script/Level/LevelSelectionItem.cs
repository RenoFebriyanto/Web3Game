using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class LevelSelectionItem : MonoBehaviour
{
    [Header("Assign")]
    public LevelConfig levelConfig;              // set this in inspector per prefab instance
    public TMP_Text numberText;                  // level number text (TMP)
    public GameObject lockedOverlay;             // overlay image when locked
    public Image bgImage;                        // optional background to change sprite
    public string gameplaySceneName = "Gameplay";

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
    }

    void OnClicked()
    {
        if (levelConfig == null) return;
        // Save selected level to PlayerPrefs for the Gameplay scene to read
        PlayerPrefs.SetString("SelectedLevelId", levelConfig.id);
        PlayerPrefs.SetInt("SelectedLevelNumber", levelConfig.number);
        PlayerPrefs.Save();

        // load gameplay
        SceneManager.LoadScene(gameplaySceneName);
    }
}
