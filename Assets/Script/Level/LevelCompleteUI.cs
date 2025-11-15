// LevelCompleteUI.cs - UPDATED: Game Over Mode + Particle Effects
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject levelCompletePanel;
    public Button continueButton;
    public Button homeButton;

    [Header("🎭 Character Lino")]
    public GameObject linoHappy;
    public GameObject linoSad;

    [Header("Stars Display")]
    public GameObject[] starObjects;
    public Image[] starImages;
    public Sprite starFilled;
    public Sprite starEmpty;

    [Header("✨ NEW: Star Particle Effects")]
    [Tooltip("Particle effect yang muncul saat star terisi (level complete)")]
    public GameObject[] starParticleEffects; // Array untuk 3 stars

    [Tooltip("Canvas tempat stars berada")]
    public Canvas targetCanvas; // ✅ NEW: Reference ke Canvas


    [Header("🎉 LEVEL COMPLETE Popup")]
    public GameObject lvlPopup;
    public RectTransform lvlPopupBackground;
    public TMP_Text lvlPopupText;
    public float lvlPopupExpandDuration = 0.4f;
    public float lvlPopupDisplayDuration = 0.8f;

    [Header("🎁 Random Rewards")]
    public GameObject rewardsContainer;
    public GameObject rewardItemPrefab;
    public Sprite coinIcon;
    public Sprite energyIcon;
    public Sprite speedBoostIcon;
    public Sprite timeFreezeIcon;
    public Sprite shieldIcon;
    public Sprite coin2xIcon;
    public Sprite magnetIcon;

    [Header("✨ Already Completed Text")]
    public TMP_Text alreadyCompletedText;
    public string alreadyCompletedMessage = "Level Already Completed";

    [Header("🎯 LVLDONE GameObject")]
    public GameObject lvlDoneObject;

    [Header("⚙️ Improved Reward Settings")]
    [Range(0, 100)]
    public float singleRewardChance = 90f;
    [Range(0, 100)]
    public float doubleRewardChance = 10f;
    public Vector2Int coinRewardRange = new Vector2Int(500, 3000);
    public Vector2Int energyRewardRange = new Vector2Int(3, 10);

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeOutDuration = 1f;
    public float fadeInDuration = 0.5f;

    [Header("Animation Settings")]
    public float starAnimationDelay = 0.3f;
    public float starScalePunch = 1.3f;
    public float starAnimationDuration = 0.3f;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "Gameplay";

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ✅ NEW: Game Over Mode
    private bool isGameOverMode = false;

    private const string PREF_REWARDED_LEVELS = "Kulino_RewardedLevels_v1";

    private bool isTransitioning = false;
    private int earnedStars = 0;
    private int currentLevelNumber = 0;
    private string currentLevelId = "";
    private List<RewardData> generatedRewards = new List<RewardData>();
    private bool isFirstCompletion = false;

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
        // ✅ Auto-find canvas if not assigned
    if (targetCanvas == null)
    {
        targetCanvas = levelCompletePanel.GetComponentInParent<Canvas>();
    }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (lvlPopup != null)
            lvlPopup.SetActive(false);

        if (rewardsContainer != null)
            rewardsContainer.SetActive(false);

        if (lvlDoneObject != null)
            lvlDoneObject.SetActive(false);

        if (alreadyCompletedText != null)
            alreadyCompletedText.gameObject.SetActive(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.interactable = false;
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(OnHomeClicked);
            homeButton.interactable = false;
        }

        if (starImages == null || starImages.Length == 0)
        {
            starImages = new Image[3];
            if (starObjects != null)
            {
                for (int i = 0; i < starObjects.Length && i < 3; i++)
                {
                    if (starObjects[i] != null)
                        starImages[i] = starObjects[i].GetComponent<Image>();
                }
            }
        }

        // ✅ Validate particle effects
        if (starParticleEffects != null && starParticleEffects.Length > 0)
        {
            foreach (var fx in starParticleEffects)
            {
                if (fx != null)
                {
                    fx.SetActive(false);
                }
            }
        }

        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Subscribed to OnLevelCompleted");
        }
        else
        {
            LogWarning("LevelGameSession.Instance not found! Searching...");
            StartCoroutine(LateSubscribe());
        }

        StartCoroutine(FadeIn());
    }

    IEnumerator LateSubscribe()
    {
        yield return new WaitForSeconds(0.5f);

        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.AddListener(OnLevelComplete);
            Log("✓ Late subscribed to OnLevelCompleted");
        }
        else
        {
            LogWarning("LevelGameSession still not found after delay!");
        }
    }

    void OnDestroy()
    {
        if (LevelGameSession.Instance != null)
        {
            LevelGameSession.Instance.OnLevelCompleted.RemoveListener(OnLevelComplete);
        }
    }

    // ✅ EXISTING: Normal level complete
    public void OnLevelComplete()
    {
        if (isTransitioning) return;

        Log("🎉 Level Complete triggered!");

        isGameOverMode = false; // Normal mode

        StopAllSpawners();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLevelComplete();
            Log("✓ Playing level complete sound");
        }

        if (GameplayStarManager.Instance != null)
        {
            earnedStars = GameplayStarManager.Instance.GetCollectedStars();
            Log($"Earned stars: {earnedStars}");
        }
        else
        {
            earnedStars = 0;
            LogWarning("GameplayStarManager not found!");
        }

        currentLevelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);
        Log($"Current level: {currentLevelId} (#{currentLevelNumber})");

        isFirstCompletion = !HasReceivedReward(currentLevelId);

        if (isFirstCompletion)
        {
            GenerateImprovedRewards();
            Log("✓ First completion - rewards generated");
        }
        else
        {
            generatedRewards.Clear();
            Log("⚠️ Level already completed - no rewards");
        }

        StartCoroutine(ShowLevelCompleteUI());
    }

    // ✅ NEW: Game Over Method
    public void ShowGameOver()
    {
        if (isTransitioning) return;

        Log("💀 Game Over triggered!");

        isGameOverMode = true; // Game Over mode
        earnedStars = 0; // NO STARS on game over

        StopAllSpawners();

        currentLevelId = PlayerPrefs.GetString("SelectedLevelId", "level_1");
        currentLevelNumber = PlayerPrefs.GetInt("SelectedLevelNumber", 1);

        generatedRewards.Clear(); // No rewards on game over

        StartCoroutine(ShowLevelCompleteUI());
    }

    void StopAllSpawners()
    {
        Log("Stopping all spawners...");

        if (SpawnerController.Instance != null)
        {
            SpawnerController.Instance.StopSpawner();
            Log("✓ Stopped spawner via SpawnerController");
        }

        var proceduralSpawner = FindFirstObjectByType<ProceduralSpawner>();
        if (proceduralSpawner != null)
        {
            proceduralSpawner.enabled = false;
            proceduralSpawner.StopAllCoroutines();
            Log("✓ Disabled ProceduralSpawner");
        }

        var allSpawners = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var spawner in allSpawners)
        {
            if (spawner != null && spawner.GetType().Name.Contains("Spawner"))
            {
                spawner.StopAllCoroutines();
                spawner.enabled = false;
            }
        }

        Log("✓ All spawners stopped");
    }

    void GenerateImprovedRewards()
    {
        generatedRewards.Clear();

        string savedRewardsJson = PlayerPrefs.GetString($"LevelRewards_{currentLevelId}", "");
        
        if (!string.IsNullOrEmpty(savedRewardsJson))
        {
            try
            {
                RewardDataList rewardList = JsonUtility.FromJson<RewardDataList>(savedRewardsJson);
                if (rewardList != null && rewardList.rewards != null)
                {
                    generatedRewards.AddRange(rewardList.rewards);
                    
                    PlayerPrefs.DeleteKey($"LevelRewards_{currentLevelId}");
                    PlayerPrefs.Save();
                    
                    Log($"✓ Using saved rewards from preview: {generatedRewards.Count} items");
                    return;
                }
            }
            catch (System.Exception e)
            {
                LogWarning($"Failed to parse saved rewards: {e.Message}");
            }
        }

        float roll = Random.Range(0f, 100f);

        if (roll < singleRewardChance)
        {
            int coinAmount = Random.Range(coinRewardRange.x, coinRewardRange.y + 1);
            generatedRewards.Add(new RewardData(
                RewardType.Coin, 
                coinAmount,
                coinIcon,
                "Coins"
            ));
            Log($"✓ Single reward: Coin x{coinAmount}");
        }
        else
        {
            int coinAmount = Random.Range(coinRewardRange.x, coinRewardRange.y + 1);
            generatedRewards.Add(new RewardData(
                RewardType.Coin,
                coinAmount,
                coinIcon,
                "Coins"
            ));

            int energyAmount = Random.Range(energyRewardRange.x, energyRewardRange.y + 1);
            generatedRewards.Add(new RewardData(
                RewardType.Energy,
                energyAmount,
                energyIcon,
                "Energy"
            ));

            Log($"✓ Double reward: Coin x{coinAmount} + Energy x{energyAmount}");
        }
    }

    void ApplyRewards()
    {
        if (PlayerEconomy.Instance == null)
        {
            LogWarning("PlayerEconomy not found! Cannot apply rewards.");
            return;
        }

        foreach (var reward in generatedRewards)
        {
            switch (reward.type)
            {
                case RewardType.Coin:
                    PlayerEconomy.Instance.AddCoins(reward.amount);
                    Log($"✓ Added {reward.amount} coins");
                    break;

                case RewardType.Energy:
                    PlayerEconomy.Instance.AddEnergy(reward.amount);
                    Log($"✓ Added {reward.amount} energy");
                    break;

                case RewardType.Booster:
                    if (BoosterInventory.Instance != null && !string.IsNullOrEmpty(reward.boosterId))
                    {
                        BoosterInventory.Instance.AddBooster(reward.boosterId, reward.amount);
                        Log($"✓ Added {reward.amount}x {reward.boosterId}");
                    }
                    break;
            }
        }

        if (isFirstCompletion)
        {
            MarkLevelRewarded(currentLevelId);
            Log($"✓ Marked {currentLevelId} as rewarded");
        }
    }

    IEnumerator ShowLevelCompleteUI()
    {
        yield return new WaitForSeconds(0.5f);

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            Log("✓ Level complete panel shown");
        }
        else
        {
            LogWarning("levelCompletePanel is NULL!");
            yield break;
        }

        ResetStars();

        // ✅ Show popup dengan text sesuai mode
        yield return StartCoroutine(AnimateLevelCompletePopup());

        SetLinoCharacter();

        // ✅ Animate stars (dengan atau tanpa particle tergantung mode)
        yield return StartCoroutine(AnimateStars());

        // ✅ Show rewards atau "already completed" message
        if (!isGameOverMode && isFirstCompletion && generatedRewards.Count > 0)
        {
            yield return StartCoroutine(ShowRewards());
        }
        else
        {
            ShowAlreadyCompletedMessage();
        }

        EnableButtons();

        Log("✓ Level complete UI fully shown");
    }

    IEnumerator AnimateLevelCompletePopup()
    {
        if (lvlPopup == null)
        {
            LogWarning("lvlPopup not assigned! Skipping popup animation.");
            yield break;
        }

        // ✅ Set text berdasarkan mode
        if (lvlPopupText != null)
        {
            lvlPopupText.text = isGameOverMode ? "GAME OVER" : "LEVEL COMPLETE";
        }

        Log($"Animating popup: {(isGameOverMode ? "GAME OVER" : "LEVEL COMPLETE")}");

        lvlPopup.SetActive(true);

        if (lvlPopupBackground != null)
        {
            Vector2 originalSize = lvlPopupBackground.sizeDelta;
            lvlPopupBackground.sizeDelta = new Vector2(0, originalSize.y);

            float elapsed = 0f;
            while (elapsed < lvlPopupExpandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lvlPopupExpandDuration;
                float easedT = EaseOutBack(t);

                lvlPopupBackground.sizeDelta = new Vector2(
                    Mathf.Lerp(0, originalSize.x, easedT),
                    originalSize.y
                );

                yield return null;
            }

            lvlPopupBackground.sizeDelta = originalSize;
        }

        yield return new WaitForSeconds(lvlPopupDisplayDuration);

        lvlPopup.SetActive(false);

        Log($"✓ Popup animation complete: {(isGameOverMode ? "GAME OVER" : "LEVEL COMPLETE")}");
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    void SetLinoCharacter()
    {
        if (linoHappy == null || linoSad == null)
        {
            LogWarning("Lino character GameObjects not assigned!");
            return;
        }

        // ✅ Game Over = always sad, Level Complete = check stars
        if (isGameOverMode || earnedStars == 0)
        {
            linoHappy.SetActive(false);
            linoSad.SetActive(true);
            Log($"✓ Showing Sad Lino ({(isGameOverMode ? "Game Over" : "0 stars")})");
        }
        else
        {
            linoHappy.SetActive(true);
            linoSad.SetActive(false);
            Log($"✓ Showing Happy Lino ({earnedStars} stars)");
        }
    }

    void ResetStars()
    {
        if (starImages == null || starEmpty == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = starEmpty;
                starImages[i].transform.localScale = Vector3.one;

                if (starObjects != null && i < starObjects.Length && starObjects[i] != null)
                {
                    starObjects[i].SetActive(true);
                }
            }
        }

        // ✅ Hide all particle effects initially
        if (starParticleEffects != null)
        {
            for (int i = 0; i < starParticleEffects.Length; i++)
            {
                if (starParticleEffects[i] != null)
                {
                    starParticleEffects[i].SetActive(false);
                }
            }
        }

        Log("✓ Stars reset to empty");
    }

    IEnumerator AnimateStars()
{
    if (starImages == null || starFilled == null)
    {
        LogWarning("Star images or starFilled sprite not assigned!");
        yield break;
    }

    int starsToShow = isGameOverMode ? 0 : earnedStars;

    // ✅ DEBUG: Print stars info
    Debug.Log($"[AnimateStars] Starting animation - Stars to show: {starsToShow}");
    Debug.Log($"[AnimateStars] starParticleEffects null? {starParticleEffects == null}");
    
    if (starParticleEffects != null)
    {
        Debug.Log($"[AnimateStars] starParticleEffects.Length = {starParticleEffects.Length}");
        for (int i = 0; i < starParticleEffects.Length; i++)
        {
            Debug.Log($"[AnimateStars] Particle[{i}] = {(starParticleEffects[i] != null ? starParticleEffects[i].name : "NULL")}");
        }
    }

    for (int i = 0; i < starsToShow && i < starImages.Length; i++)
    {
        if (starImages[i] == null) continue;

        yield return new WaitForSeconds(starAnimationDelay);

        starImages[i].sprite = starFilled;
        StartCoroutine(PunchScale(starImages[i].transform));

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStarPickup();
        }

        // ✅ DEBUG: Detailed particle spawn logging
        Debug.Log($"[AnimateStars] Attempting to spawn particle for star {i + 1}");
        Debug.Log($"[AnimateStars] - isGameOverMode: {isGameOverMode}");
        Debug.Log($"[AnimateStars] - starParticleEffects null? {starParticleEffects == null}");
        
        if (!isGameOverMode && starParticleEffects != null && i < starParticleEffects.Length)
        {
            Debug.Log($"[AnimateStars] - Passed null checks");
            
            if (starParticleEffects[i] != null && starImages[i] != null)
            {
                Debug.Log($"[AnimateStars] - Particle GameObject: {starParticleEffects[i].name}");
                Debug.Log($"[AnimateStars] - Particle active before: {starParticleEffects[i].activeSelf}");
                Debug.Log($"[AnimateStars] - targetCanvas: {(targetCanvas != null ? targetCanvas.name : "NULL")}");
                
                Vector3 worldPos = ConvertUIToWorldPosition(starImages[i].rectTransform, targetCanvas);
                
                Debug.Log($"[AnimateStars] - Calculated worldPos: {worldPos}");
                
                starParticleEffects[i].transform.position = worldPos;
                
                Debug.Log($"[AnimateStars] - Set position to: {starParticleEffects[i].transform.position}");
                
                starParticleEffects[i].SetActive(true);
                
                Debug.Log($"[AnimateStars] - Particle active after: {starParticleEffects[i].activeSelf}");
                Debug.Log($"[AnimateStars] ✓ Particle {i} activated successfully!");
                
                StartCoroutine(DisableParticleAfterDelay(starParticleEffects[i], 2f));
            }
            else
            {
                Debug.LogError($"[AnimateStars] - Particle[{i}] or starImage[{i}] is NULL!");
            }
        }
        else
        {
            if (isGameOverMode)
                Debug.Log($"[AnimateStars] - Skipped (Game Over Mode)");
            else if (starParticleEffects == null)
                Debug.LogError($"[AnimateStars] - starParticleEffects array is NULL!");
            else if (i >= starParticleEffects.Length)
                Debug.LogError($"[AnimateStars] - Index {i} out of range (Length: {starParticleEffects.Length})");
        }

        Log($"✓ Star {i + 1} animated");
    }

    Log($"✓ All {starsToShow} stars animated");
}

    Vector3 ConvertUIToWorldPosition(RectTransform uiElement, Canvas canvas)
{
    Debug.Log($"[ConvertUIToWorldPosition] ===== START =====");
    
    if (canvas == null)
    {
        Debug.LogError("[ConvertUIToWorldPosition] Canvas is NULL!");
        return Vector3.zero;
    }
    
    if (uiElement == null)
    {
        Debug.LogError("[ConvertUIToWorldPosition] UI Element is NULL!");
        return Vector3.zero;
    }
    
    Debug.Log($"[ConvertUIToWorldPosition] Canvas: {canvas.name}");
    Debug.Log($"[ConvertUIToWorldPosition] Canvas Render Mode: {canvas.renderMode}");
    Debug.Log($"[ConvertUIToWorldPosition] UI Element: {uiElement.name}");
    Debug.Log($"[ConvertUIToWorldPosition] UI Element position: {uiElement.position}");
    
    // Get screen position of UI element
    Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(
        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
        uiElement.position
    );
    
    Debug.Log($"[ConvertUIToWorldPosition] Screen Pos: {screenPos}");
    
    // Convert screen position to world position
    Camera mainCam = Camera.main;
    if (mainCam == null)
    {
        Debug.LogError("[ConvertUIToWorldPosition] Main camera not found!");
        return Vector3.zero;
    }
    
    Debug.Log($"[ConvertUIToWorldPosition] Main Camera: {mainCam.name}");
    Debug.Log($"[ConvertUIToWorldPosition] Camera Pos: {mainCam.transform.position}");
    
    // Convert to world space (at specific Z distance for particles)
    float zDistance = 10f; // Adjust ini sesuai kebutuhan
    Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDistance));
    
    Debug.Log($"[ConvertUIToWorldPosition] zDistance: {zDistance}");
    Debug.Log($"[ConvertUIToWorldPosition] Final World Pos: {worldPos}");
    Debug.Log($"[ConvertUIToWorldPosition] ===== END =====");
    
    return worldPos;
}



    // ✅ NEW: Disable particle effect after delay
    IEnumerator DisableParticleAfterDelay(GameObject particleFX, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (particleFX != null)
        {
            particleFX.SetActive(false);
        }
    }

    IEnumerator PunchScale(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float elapsed = 0f;
        float halfDuration = starAnimationDuration / 2f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale, originalScale * starScalePunch, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale * starScalePunch, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    IEnumerator ShowRewards()
    {
        if (rewardsContainer == null || rewardItemPrefab == null)
        {
            LogWarning("Rewards container or prefab not assigned!");
            ShowAlreadyCompletedMessage();
            yield break;
        }

        if (generatedRewards.Count == 0)
        {
            LogWarning("No rewards! Showing already completed message.");
            ShowAlreadyCompletedMessage();
            yield break;
        }

        Log($"Showing {generatedRewards.Count} reward(s)...");

        foreach (Transform child in rewardsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        rewardsContainer.SetActive(true);

        if (lvlDoneObject != null)
        {
            lvlDoneObject.SetActive(false);
        }

        if (alreadyCompletedText != null)
        {
            alreadyCompletedText.gameObject.SetActive(false);
        }

        ApplyRewards();

        yield return new WaitForSeconds(0.3f);

        foreach (var reward in generatedRewards)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardsContainer.transform);

            SetupRewardItem(rewardItem, reward);

            StartCoroutine(PunchScale(rewardItem.transform));

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCoinPickup();
            }

            yield return new WaitForSeconds(0.2f);
        }

        Log("✓ All rewards shown");
    }

    void ShowAlreadyCompletedMessage()
    {
        Log("Showing 'Already Completed' message");

        if (lvlDoneObject != null)
        {
            lvlDoneObject.SetActive(true);
            Log("✓ LVLDONE object shown");
        }

        if (alreadyCompletedText != null)
        {
            // ✅ Change message for game over
            alreadyCompletedText.text = isGameOverMode ? "Try Again!" : alreadyCompletedMessage;
            alreadyCompletedText.gameObject.SetActive(true);
            Log($"✓ Already completed text shown: {alreadyCompletedText.text}");
        }

        if (rewardsContainer != null)
        {
            rewardsContainer.SetActive(false);
        }
    }

    void SetupRewardItem(GameObject item, RewardData reward)
    {
        Transform iconTransform = item.transform.Find("IconItemsRW");

        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();

            if (iconImage != null && reward.icon != null)
            {
                iconImage.sprite = reward.icon;
                Log($"✓ Set reward icon: {reward.icon.name}");
            }
        }

        TMP_Text amountText = item.GetComponentInChildren<TMP_Text>();
        if (amountText != null)
        {
            amountText.text = $"+{reward.amount}";
        }

        Log($"✓ Setup reward: {reward.displayName} x{reward.amount}");
    }

    void EnableButtons()
    {
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        if (homeButton != null)
        {
            homeButton.interactable = true;
        }

        Log("✓ Buttons enabled");
    }

    public void OnContinueClicked()
    {
        if (isTransitioning) return;

        Log("Continue button clicked");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        // ✅ Game Over = retry same level, Level Complete = next level
        if (isGameOverMode)
        {
            StartCoroutine(TransitionToRetryLevel());
        }
        else
        {
            StartCoroutine(TransitionToNextLevel());
        }
    }

    public void OnHomeClicked()
    {
        if (isTransitioning) return;

        Log("Home button clicked");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        StartCoroutine(TransitionToMainMenu());
    }

    // ✅ NEW: Retry same level (game over)
    IEnumerator TransitionToRetryLevel()
    {
        isTransitioning = true;

        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

        Log($"Retrying level: {currentLevelNumber}");

        // Keep same level
        PlayerPrefs.SetString("SelectedLevelId", currentLevelId);
        PlayerPrefs.SetInt("SelectedLevelNumber", currentLevelNumber);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameplaySceneName);
    }

    IEnumerator TransitionToNextLevel()
    {
        isTransitioning = true;

        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

        int nextLevel = currentLevelNumber + 1;

        if (nextLevel <= 100)
        {
            Log($"Loading next level: {nextLevel}");

            PlayerPrefs.SetString("SelectedLevelId", $"level_{nextLevel}");
            PlayerPrefs.SetInt("SelectedLevelNumber", nextLevel);
            PlayerPrefs.Save();

            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Log("No more levels, returning to main menu");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    IEnumerator TransitionToMainMenu()
    {
        isTransitioning = true;

        Time.timeScale = 1f;

        yield return StartCoroutine(FadeOut());

        Log("Loading main menu");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    IEnumerator FadeOut()
    {
        if (fadeImage == null)
        {
            LogWarning("Fade image not assigned! Skipping fade out.");
            yield break;
        }

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        Color startColor = fadeImage.color;
        startColor.a = 0f;
        Color endColor = startColor;
        endColor.a = 1f;

        fadeImage.color = startColor;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
        Log("✓ Fade out complete");
    }

    IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            LogWarning("Fade image not assigned! Skipping fade in.");
            yield break;
        }

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        Color startColor = fadeImage.color;
        startColor.a = 1f;
        Color endColor = startColor;
        endColor.a = 0f;

        fadeImage.color = startColor;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
        fadeImage.gameObject.SetActive(false);
        Log("✓ Fade in complete");
    }

    bool HasReceivedReward(string levelId)
    {
        string rewarded = PlayerPrefs.GetString(PREF_REWARDED_LEVELS, "");
        if (string.IsNullOrEmpty(rewarded)) return false;

        string[] levels = rewarded.Split(',');
        foreach (string lvl in levels)
        {
            if (lvl.Trim() == levelId)
            {
                return true;
            }
        }

        return false;
    }

    void MarkLevelRewarded(string levelId)
    {
        string rewarded = PlayerPrefs.GetString(PREF_REWARDED_LEVELS, "");

        if (string.IsNullOrEmpty(rewarded))
        {
            rewarded = levelId;
        }
        else
        {
            if (!HasReceivedReward(levelId))
            {
                rewarded += "," + levelId;
            }
        }

        PlayerPrefs.SetString(PREF_REWARDED_LEVELS, rewarded);
        PlayerPrefs.Save();

        Log($"✓ Marked {levelId} as rewarded");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[LevelCompleteUI] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[LevelCompleteUI] {message}");
    }

    [ContextMenu("Debug: Clear Reward History")]
    public void ClearRewardHistory()
    {
        PlayerPrefs.DeleteKey(PREF_REWARDED_LEVELS);
        PlayerPrefs.Save();
        Debug.Log("[LevelCompleteUI] Reward history cleared");
    }

    [ContextMenu("Debug: Test Game Over")]
    void Debug_TestGameOver()
    {
        ShowGameOver();
    }

    [ContextMenu("Debug: Test Level Complete")]
    void Debug_TestLevelComplete()
    {
        OnLevelComplete();
    }
}