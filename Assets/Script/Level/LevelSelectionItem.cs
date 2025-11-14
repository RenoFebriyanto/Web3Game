using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("⚡ Energy Cost")]
    [Tooltip("Energy cost per level")]
    public int energyCostPerLevel = 10;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    void Start()
    {
        if (levelConfig != null)
        {
            Refresh();
        }
        else
        {
            Debug.LogWarning($"[LevelSelectionItem] {gameObject.name}: levelConfig is NULL!");
        }
    }

    public void Refresh()
    {
        if (levelConfig == null)
        {
            Debug.LogWarning($"[LevelSelectionItem] levelConfig not set on {gameObject.name}");
            return;
        }

        bool unlocked = LevelProgressManager.Instance != null ?
                        LevelProgressManager.Instance.IsUnlocked(levelConfig.number) :
                        (levelConfig.number == 1);

        if (numberText != null)
        {
            numberText.text = levelConfig.number.ToString();
            numberText.gameObject.SetActive(unlocked);
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!unlocked);
        }

        if (btn != null)
        {
            btn.interactable = unlocked;
        }

        RefreshStars();

        Debug.Log($"[LevelSelectionItem] ✓ {levelConfig.id} refreshed (unlocked: {unlocked})");
    }

    void RefreshStars()
    {
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        if (levelConfig == null) return;

        int earnedStars = 0;
        if (LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(levelConfig.id);
        }

        if (earnedStars >= 1 && star1 != null) star1.SetActive(true);
        if (earnedStars >= 2 && star2 != null) star2.SetActive(true);
        if (earnedStars >= 3 && star3 != null) star3.SetActive(true);
    }

    void OnClicked()
    {
        if (levelConfig == null)
        {
            Debug.LogError("[LevelSelectionItem] Cannot start level: levelConfig is NULL!");
            return;
        }



        // ✅ ENOUGH ENERGY - Show level preview
        LevelPreviewController.ShowPreview(levelConfig);

        Debug.Log($"[LevelSelectionItem] Showing preview for {levelConfig.id}");
    }
}