// Assets/Script/Level/MissionManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text missionDescText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image missionIcon;
    [SerializeField] private GameObject starContainer;
    [SerializeField] private Image[] starImages = new Image[3];

    [Header("Star Visual")]
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private Sprite starFilled;
    [SerializeField] private ParticleSystem starEarnedEffect;

    [Header("Mission Icons")]
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private Sprite crystalIcon;
    [SerializeField] private Sprite distanceIcon;
    [SerializeField] private Sprite avoidIcon;
    [SerializeField] private Sprite starIcon;

    // Current mission data
    private LevelMission.MissionType currentMissionType;
    private int currentMissionTarget;
    private string currentMissionDescription;
    private int currentProgress = 0;
    private int starsEarned = 0;

    // Progress tracking
    private int coinsCollected = 0;
    private int crystalsCollected = 0;
    private float distanceTraveled = 0f;
    private int planetsAvoided = 0;
    private int starsCollected = 0;

    // Star thresholds (will be calculated based on mission)
    private int starThreshold1, starThreshold2, starThreshold3;

    // Events
    public event Action<int> OnStarEarned;
    public event Action<int> OnMissionProgress;
    public event Action OnMissionComplete;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        LoadMissionData();
        InitializeUI();

        // Subscribe to relevant events
        SubscribeToGameEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    private void LoadMissionData()
    {
        // Load mission data from PlayerPrefs (set by LevelButtonUI)
        string missionTypeStr = PlayerPrefs.GetString("CurrentLevelMission", "CollectCoins");
        currentMissionTarget = PlayerPrefs.GetInt("CurrentLevelMissionAmount", 20);
        currentMissionDescription = PlayerPrefs.GetString("CurrentLevelMissionDesc", "Collect 20 coins");

        if (Enum.TryParse(missionTypeStr, out LevelMission.MissionType missionType))
        {
            currentMissionType = missionType;
        }
        else
        {
            currentMissionType = LevelMission.MissionType.CollectCoins;
        }

        CalculateStarThresholds();

        Debug.Log($"[MissionManager] Loaded mission: {currentMissionDescription}");
    }

    private void CalculateStarThresholds()
    {
        // Calculate star thresholds based on mission type
        switch (currentMissionType)
        {
            case LevelMission.MissionType.CollectCoins:
            case LevelMission.MissionType.CollectCrystals:
            case LevelMission.MissionType.CollectStars:
                starThreshold1 = Mathf.RoundToInt(currentMissionTarget * 0.3f);
                starThreshold2 = Mathf.RoundToInt(currentMissionTarget * 0.6f);
                starThreshold3 = currentMissionTarget;
                break;

            case LevelMission.MissionType.SurviveDistance:
                starThreshold1 = Mathf.RoundToInt(currentMissionTarget * 0.5f);
                starThreshold2 = Mathf.RoundToInt(currentMissionTarget * 0.8f);
                starThreshold3 = currentMissionTarget;
                break;

            case LevelMission.MissionType.AvoidPlanets:
                starThreshold1 = Mathf.RoundToInt(currentMissionTarget * 0.4f);
                starThreshold2 = Mathf.RoundToInt(currentMissionTarget * 0.7f);
                starThreshold3 = currentMissionTarget;
                break;
        }

        // Ensure minimum values
        starThreshold1 = Mathf.Max(starThreshold1, 1);
        starThreshold2 = Mathf.Max(starThreshold2, starThreshold1 + 1);
        starThreshold3 = Mathf.Max(starThreshold3, starThreshold2 + 1);
    }

    private void InitializeUI()
    {
        // Set mission description
        if (missionDescText != null)
            missionDescText.text = currentMissionDescription;

        // Set mission icon
        if (missionIcon != null)
            missionIcon.sprite = GetMissionIcon(currentMissionType);

        // Initialize progress bar
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = currentMissionTarget;
            progressBar.value = 0;
        }

        // Initialize stars
        UpdateStarsUI();

        UpdateProgressUI();
    }

    private void SubscribeToGameEvents()
    {
        // Subscribe to game events that affect mission progress
        // These would typically be called by your game systems

        // Example subscriptions (implement based on your game systems):
        // EventSystem.OnCoinCollected += OnCoinCollected;
        // EventSystem.OnCrystalCollected += OnCrystalCollected;
        // EventSystem.OnDistanceTraveled += OnDistanceTraveled;
        // EventSystem.OnPlanetAvoided += OnPlanetAvoided;
    }

    private void UnsubscribeFromGameEvents()
    {
        // Unsubscribe from events
        // EventSystem.OnCoinCollected -= OnCoinCollected;
        // EventSystem.OnCrystalCollected -= OnCrystalCollected;
        // EventSystem.OnDistanceTraveled -= OnDistanceTraveled;
        // EventSystem.OnPlanetAvoided -= OnPlanetAvoided;
    }

    // Public methods to be called by game systems
    public void OnCoinCollected(int amount = 1)
    {
        if (currentMissionType != LevelMission.MissionType.CollectCoins) return;

        coinsCollected += amount;
        UpdateProgress(coinsCollected);
    }

    public void OnCrystalCollected(int amount = 1)
    {
        if (currentMissionType != LevelMission.MissionType.CollectCrystals) return;

        crystalsCollected += amount;
        UpdateProgress(crystalsCollected);
    }

    public void OnDistanceTraveled(float distance)
    {
        if (currentMissionType != LevelMission.MissionType.SurviveDistance) return;

        distanceTraveled += distance;
        UpdateProgress(Mathf.RoundToInt(distanceTraveled));
    }

    public void OnPlanetAvoided(int amount = 1)
    {
        if (currentMissionType != LevelMission.MissionType.AvoidPlanets) return;

        planetsAvoided += amount;
        UpdateProgress(planetsAvoided);
    }

    public void OnStarCollected(int amount = 1)
    {
        if (currentMissionType != LevelMission.MissionType.CollectStars) return;

        starsCollected += amount;
        UpdateProgress(starsCollected);
    }

    private void UpdateProgress(int newProgress)
    {
        int previousProgress = currentProgress;
        currentProgress = Mathf.Clamp(newProgress, 0, currentMissionTarget);

        // Check for new stars earned
        CheckStarProgress(previousProgress, currentProgress);

        // Update UI
        UpdateProgressUI();

        // Fire progress event
        OnMissionProgress?.Invoke(currentProgress);

        // Check mission completion
        if (currentProgress >= currentMissionTarget && previousProgress < currentMissionTarget)
        {
            CompleteMission();
        }
    }

    private void CheckStarProgress(int previousProgress, int newProgress)
    {
        // Check for star threshold crossings
        if (previousProgress < starThreshold1 && newProgress >= starThreshold1 && starsEarned < 1)
        {
            EarnStar(1);
        }
        if (previousProgress < starThreshold2 && newProgress >= starThreshold2 && starsEarned < 2)
        {
            EarnStar(2);
        }
        if (previousProgress < starThreshold3 && newProgress >= starThreshold3 && starsEarned < 3)
        {
            EarnStar(3);
        }
    }

    private void EarnStar(int starNumber)
    {
        starsEarned = starNumber;
        UpdateStarsUI();

        // Play star earned effect
        if (starEarnedEffect != null)
            starEarnedEffect.Play();

        // Fire star earned event
        OnStarEarned?.Invoke(starNumber);

        Debug.Log($"[MissionManager] Star {starNumber} earned!");
    }

    private void CompleteMission()
    {
        Debug.Log($"[MissionManager] Mission completed with {starsEarned} stars!");

        // Save progress to LevelManager
        SaveMissionCompletion();

        // Fire mission complete event
        OnMissionComplete?.Invoke();
    }

    private void SaveMissionCompletion()
    {
        string currentLevelId = LevelLoader.CurrentLevelId;
        if (string.IsNullOrEmpty(currentLevelId)) return;

        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.MarkLevelCompleted(currentLevelId, starsEarned);
        }

        // Also save to LevelProgressManager if it exists
        var progressManager = LevelProgressManager.Instance;
        if (progressManager != null)
        {
            progressManager.SaveBestStars(currentLevelId, starsEarned);
        }
    }

    private void UpdateProgressUI()
    {
        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"{currentProgress} / {currentMissionTarget}";
        }

        // Update progress bar
        if (progressBar != null)
        {
            progressBar.value = currentProgress;
        }
    }

    private void UpdateStarsUI()
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                bool starEarned = i < starsEarned;
                starImages[i].sprite = starEarned ? starFilled : starEmpty;
                starImages[i].color = starEarned ? Color.yellow : Color.gray;
            }
        }
    }

    private Sprite GetMissionIcon(LevelMission.MissionType missionType)
    {
        switch (missionType)
        {
            case LevelMission.MissionType.CollectCoins: return coinIcon;
            case LevelMission.MissionType.CollectCrystals: return crystalIcon;
            case LevelMission.MissionType.SurviveDistance: return distanceIcon;
            case LevelMission.MissionType.AvoidPlanets: return avoidIcon;
            case LevelMission.MissionType.CollectStars: return starIcon;
            default: return coinIcon;
        }
    }

    // Public getters
    public int GetCurrentProgress() => currentProgress;
    public int GetMissionTarget() => currentMissionTarget;
    public int GetStarsEarned() => starsEarned;
    public LevelMission.MissionType GetMissionType() => currentMissionType;
    public string GetMissionDescription() => currentMissionDescription;
    public bool IsMissionComplete() => currentProgress >= currentMissionTarget;

    // Method for manual testing
    [ContextMenu("Test Complete Mission")]
    public void TestCompleteMission()
    {
        UpdateProgress(currentMissionTarget);
    }

    [ContextMenu("Test Add Progress")]
    public void TestAddProgress()
    {
        switch (currentMissionType)
        {
            case LevelMission.MissionType.CollectCoins:
                OnCoinCollected(5);
                break;
            case LevelMission.MissionType.CollectCrystals:
                OnCrystalCollected(2);
                break;
            case LevelMission.MissionType.SurviveDistance:
                OnDistanceTraveled(10f);
                break;
            case LevelMission.MissionType.AvoidPlanets:
                OnPlanetAvoided(3);
                break;
            case LevelMission.MissionType.CollectStars:
                OnStarCollected(1);
                break;
        }
    }
}