// Assets/Script/Level/LevelButtonUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LevelButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Button mainButton;
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image lockIcon;
    [SerializeField] private GameObject starContainer;
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private TMP_Text missionText;
    [SerializeField] private Image missionIcon;

    [Header("Visual States")]
    [SerializeField] private Sprite unlockedBackground;
    [SerializeField] private Sprite lockedBackground;
    [SerializeField] private Sprite completedBackground;
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private Sprite starFilled;
    [SerializeField] private Sprite lockSprite;

    [Header("Mission Icons")]
    [SerializeField] private Sprite coinMissionIcon;
    [SerializeField] private Sprite crystalMissionIcon;
    [SerializeField] private Sprite distanceMissionIcon;
    [SerializeField] private Sprite avoidMissionIcon;
    [SerializeField] private Sprite starMissionIcon;

    [Header("Colors")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color world1Color = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color world2Color = new Color(0.3f, 0.9f, 1f);
    [SerializeField] private Color world3Color = new Color(0.9f, 0.3f, 1f);

    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 0.2f;

    // Runtime references
    private LevelDefinition levelData;
    private LevelManager levelManager;
    private LevelAutoGenerator levelGenerator;
    private ExtendedLevelData extendedData;

    // Animation
    private Vector3 originalScale;
    private bool isHovering = false;

    void Awake()
    {
        originalScale = transform.localScale;

        // Ensure main button is assigned
        if (mainButton == null)
            mainButton = GetComponent<Button>();

        if (mainButton != null)
            mainButton.onClick.AddListener(OnButtonClick);
    }

    public void SetupLevel(LevelDefinition level, LevelManager manager, LevelAutoGenerator generator)
    {
        levelData = level;
        levelManager = manager;
        levelGenerator = generator;

        // Try to get extended data
        if (generator != null)
        {
            extendedData = generator.GetExtendedLevelData(level.id);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (levelData == null) return;

        bool isUnlocked = levelManager?.IsUnlocked(levelData.id) ?? !levelData.locked;
        int bestStars = levelManager?.GetBestStars(levelData.id) ?? levelData.bestStars;
        bool isCompleted = bestStars > 0;

        // Update level number
        if (levelNumberText != null)
        {
            levelNumberText.text = levelData.number.ToString();
            levelNumberText.color = isUnlocked ? Color.white : Color.gray;
        }

        // Update background and main button
        UpdateBackground(isUnlocked, isCompleted);

        // Update lock icon
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(!isUnlocked);
            if (lockSprite != null)
                lockIcon.sprite = lockSprite;
        }

        // Update stars
        UpdateStars(bestStars);

        // Update mission info
        UpdateMissionInfo();

        // Update button interactability
        if (mainButton != null)
        {
            mainButton.interactable = isUnlocked;
        }

        // Update world color theme
        UpdateWorldTheme();
    }

    private void UpdateBackground(bool isUnlocked, bool isCompleted)
    {
        if (backgroundImage == null) return;

        if (isCompleted && completedBackground != null)
        {
            backgroundImage.sprite = completedBackground;
            backgroundImage.color = completedColor;
        }
        else if (isUnlocked && unlockedBackground != null)
        {
            backgroundImage.sprite = unlockedBackground;
            backgroundImage.color = unlockedColor;
        }
        else if (lockedBackground != null)
        {
            backgroundImage.sprite = lockedBackground;
            backgroundImage.color = lockedColor;
        }
    }

    private void UpdateStars(int bestStars)
    {
        if (starContainer != null)
        {
            starContainer.SetActive(bestStars > 0 || levelManager?.IsUnlocked(levelData.id) == true);
        }

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                bool starEarned = i < bestStars;
                starImages[i].sprite = starEarned ? starFilled : starEmpty;
                starImages[i].color = starEarned ? Color.yellow : Color.gray;

                // Add slight animation to earned stars
                if (starEarned)
                {
                    StartCoroutine(AnimateStar(starImages[i], i * 0.1f));
                }
            }
        }
    }

    private void UpdateMissionInfo()
    {
        if (extendedData?.mission == null) return;

        // Update mission text
        if (missionText != null)
        {
            missionText.text = extendedData.mission.description;
            missionText.gameObject.SetActive(levelManager?.IsUnlocked(levelData.id) == true);
        }

        // Update mission icon
        if (missionIcon != null)
        {
            missionIcon.sprite = GetMissionIcon(extendedData.mission.type);
            missionIcon.gameObject.SetActive(levelManager?.IsUnlocked(levelData.id) == true);
        }
    }

    private void UpdateWorldTheme()
    {
        if (extendedData == null) return;

        // Apply world-specific color theme
        Color worldColor = GetWorldColor(extendedData.worldNumber);

        // You can apply this color to various UI elements
        if (backgroundImage != null && levelManager?.IsUnlocked(levelData.id) == true)
        {
            backgroundImage.color = Color.Lerp(backgroundImage.color, worldColor, 0.3f);
        }
    }

    private Color GetWorldColor(int worldNumber)
    {
        switch (worldNumber)
        {
            case 1: return world1Color;
            case 2: return world2Color;
            case 3: return world3Color;
            default: return world1Color;
        }
    }

    private Sprite GetMissionIcon(LevelMission.MissionType missionType)
    {
        switch (missionType)
        {
            case LevelMission.MissionType.CollectCoins: return coinMissionIcon;
            case LevelMission.MissionType.CollectCrystals: return crystalMissionIcon;
            case LevelMission.MissionType.SurviveDistance: return distanceMissionIcon;
            case LevelMission.MissionType.AvoidPlanets: return avoidMissionIcon;
            case LevelMission.MissionType.CollectStars: return starMissionIcon;
            default: return coinMissionIcon;
        }
    }

    private System.Collections.IEnumerator AnimateStar(Image starImage, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 originalScale = starImage.transform.localScale;
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            starImage.transform.localScale = originalScale * scale;
            yield return null;
        }

        starImage.transform.localScale = originalScale;
    }

    private void OnButtonClick()
    {
        if (levelData == null || levelManager == null) return;

        bool isUnlocked = levelManager.IsUnlocked(levelData.id);
        if (!isUnlocked)
        {
            Debug.Log($"[LevelButtonUI] Level {levelData.number} is locked!");

            // Play locked sound or show locked message
            ShowLockedMessage();
            return;
        }

        Debug.Log($"[LevelButtonUI] Loading Level {levelData.number} (ID: {levelData.id})");

        // Set current level data for gameplay scene
        SetCurrentLevelData();

        // Load gameplay scene
        LevelLoader.LoadLevel(levelData.id, levelData.number);
    }

    private void SetCurrentLevelData()
    {
        // Store level mission data for gameplay scene
        if (extendedData?.mission != null)
        {
            PlayerPrefs.SetString("CurrentLevelMission", extendedData.mission.type.ToString());
            PlayerPrefs.SetInt("CurrentLevelMissionAmount", extendedData.mission.targetAmount);
            PlayerPrefs.SetString("CurrentLevelMissionDesc", extendedData.mission.description);
            PlayerPrefs.Save();
        }
    }

    private void ShowLockedMessage()
    {
        // Simple debug message for now - you can implement a proper popup later
        Debug.Log("This level is locked! Complete the previous level to unlock it.");

        // Add shake animation
        StartCoroutine(ShakeButton());
    }

    private System.Collections.IEnumerator ShakeButton()
    {
        Vector3 originalPos = transform.position;
        float shakeAmount = 10f;
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float x = originalPos.x + Random.Range(-shakeAmount, shakeAmount);
            float y = originalPos.y + Random.Range(-shakeAmount, shakeAmount);

            transform.position = new Vector3(x, y, originalPos.z);
            yield return null;
        }

        transform.position = originalPos;
    }

    // Hover effects
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (levelManager?.IsUnlocked(levelData.id) == true)
        {
            isHovering = true;
            StartCoroutine(AnimateScale(hoverScale));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        StartCoroutine(AnimateScale(1f));
    }

    private System.Collections.IEnumerator AnimateScale(float targetScale)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;
        float elapsed = 0;

        while (elapsed < animationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationSpeed;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
    }

    // Public getter methods
    public string GetLevelId() => levelData?.id;
    public int GetLevelNumber() => levelData?.number ?? 0;
    public LevelMission GetMission() => extendedData?.mission;
}