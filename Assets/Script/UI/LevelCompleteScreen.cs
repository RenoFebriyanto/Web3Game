// Assets/Script/UI/LevelCompleteScreen.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelCompleteScreen : MonoBehaviour
{
    [Header("Star Display")]
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Sprite starEmpty;
    [SerializeField] private Sprite starFilled;
    [SerializeField] private ParticleSystem[] starEffects = new ParticleSystem[3];
    [SerializeField] private AudioClip starEarnedSound;

    [Header("Results Display")]
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private TMP_Text missionDescriptionText;
    [SerializeField] private TMP_Text coinsCollectedText;
    [SerializeField] private TMP_Text crystalsCollectedText;
    [SerializeField] private TMP_Text distanceTraveledText;
    [SerializeField] private TMP_Text starsEarnedText;

    [Header("Rewards Display")]
    [SerializeField] private TMP_Text coinRewardText;
    [SerializeField] private TMP_Text crystalRewardText;
    [SerializeField] private TMP_Text experienceRewardText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Animation")]
    [SerializeField] private float starAnimationDelay = 0.5f;
    [SerializeField] private float countUpDuration = 1f;
    [SerializeField] private GameObject celebrationEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip buttonClickSound;

    private int earnedStars = 0;
    private bool hasShownAnimation = false;

    void Start()
    {
        SetupButtons();
    }

    void OnEnable()
    {
        // Reset animation state when screen becomes active
        hasShownAnimation = false;

        // Hide all stars initially
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = starEmpty;
                starImages[i].color = Color.gray;
                starImages[i].transform.localScale = Vector3.one;
            }
        }

        // Play level complete sound
        PlaySound(levelCompleteSound);
    }

    private void SetupButtons()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => OnButtonClick("Restart"));

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(() => OnButtonClick("NextLevel"));

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => OnButtonClick("MainMenu"));
    }

    public void ShowResults(int starsEarned, int coinsCollected, int crystalsCollected, int distanceTraveled)
    {
        if (hasShownAnimation) return;
        hasShownAnimation = true;

        earnedStars = Mathf.Clamp(starsEarned, 0, 3);

        // Display current level info
        DisplayLevelInfo();

        // Display session results
        DisplaySessionResults(coinsCollected, crystalsCollected, distanceTraveled);

        // Calculate and display rewards
        DisplayRewards(starsEarned, coinsCollected, crystalsCollected);

        // Start star animation
        StartCoroutine(AnimateStars());

        // Check if next level button should be enabled
        UpdateNextLevelButton();
    }

    private void DisplayLevelInfo()
    {
        string currentLevelId = LevelLoader.CurrentLevelId;
        int currentLevelNumber = LevelLoader.CurrentLevelNumber;

        if (levelNumberText != null)
        {
            levelNumberText.text = $"LEVEL {currentLevelNumber}";
        }

        // Get mission description
        var missionManager = MissionManager.Instance;
        if (missionManager != null && missionDescriptionText != null)
        {
            missionDescriptionText.text = missionManager.GetMissionDescription();
        }
    }

    private void DisplaySessionResults(int coins, int crystals, int distance)
    {
        StartCoroutine(CountUpText(coinsCollectedText, 0, coins, "Coins: "));
        StartCoroutine(CountUpText(crystalsCollectedText, 0, crystals, "Crystals: "));
        StartCoroutine(CountUpText(distanceTraveledText, 0, distance, "Distance: ", "m"));

        if (starsEarnedText != null)
        {
            starsEarnedText.text = $"Stars Earned: {earnedStars}/3";
        }
    }

    private void DisplayRewards(int starsEarned, int coins, int crystals)
    {
        // Calculate rewards based on performance
        int coinReward = CalculateCoinReward(starsEarned, coins);
        int crystalReward = CalculateCrystalReward(starsEarned, crystals);
        int experienceReward = CalculateExperienceReward(starsEarned);

        // Display rewards
        if (coinRewardText != null)
            StartCoroutine(CountUpText(coinRewardText, 0, coinReward, "+", " Coins"));

        if (crystalRewardText != null)
            StartCoroutine(CountUpText(crystalRewardText, 0, crystalReward, "+", " Crystals"));

        if (experienceRewardText != null)
            StartCoroutine(CountUpText(experienceRewardText, 0, experienceReward, "+", " XP"));

        // Apply rewards to player economy
        ApplyRewards(coinReward, crystalReward, experienceReward);
    }

    private int CalculateCoinReward(int stars, int coinsCollected)
    {
        int baseReward = 100;
        int starBonus = stars * 50;
        int collectionBonus = coinsCollected * 2;
        return baseReward + starBonus + collectionBonus;
    }

    private int CalculateCrystalReward(int stars, int crystalsCollected)
    {
        int baseReward = 5;
        int starBonus = stars * 3;
        int collectionBonus = crystalsCollected;
        return baseReward + starBonus + collectionBonus;
    }

    private int CalculateExperienceReward(int stars)
    {
        int baseReward = 50;
        int starBonus = stars * 25;
        return baseReward + starBonus;
    }

    private void ApplyRewards(int coins, int crystals, int experience)
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.AddCoins(coins);
            PlayerEconomy.Instance.AddShards(crystals); // Using shards as crystals
            // PlayerEconomy.Instance.AddExperience(experience); // If you have experience system
        }
    }

    private IEnumerator CountUpText(TMP_Text textComponent, int startValue, int endValue, string prefix, string suffix = "")
    {
        if (textComponent == null) yield break;

        float elapsed = 0f;
        while (elapsed < countUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / countUpDuration;
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
            textComponent.text = $"{prefix}{currentValue}{suffix}";
            yield return null;
        }

        textComponent.text = $"{prefix}{endValue}{suffix}";
    }

    private IEnumerator AnimateStars()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Initial delay

        for (int i = 0; i < earnedStars; i++)
        {
            if (i < starImages.Length && starImages[i] != null)
            {
                // Animate star appearance
                yield return StartCoroutine(AnimateSingleStar(i));
                yield return new WaitForSecondsRealtime(starAnimationDelay);
            }
        }

        // Show celebration effect if got 3 stars
        if (earnedStars == 3 && celebrationEffect != null)
        {
            celebrationEffect.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            celebrationEffect.SetActive(false);
        }
    }

    private IEnumerator AnimateSingleStar(int starIndex)
    {
        var starImage = starImages[starIndex];

        // Play star effect
        if (starIndex < starEffects.Length && starEffects[starIndex] != null)
        {
            starEffects[starIndex].Play();
        }

        // Play star sound
        PlaySound(starEarnedSound);

        // Animate star scale and color
        starImage.sprite = starFilled;
        Vector3 originalScale = starImage.transform.localScale;

        float duration = 0.5f;
        float elapsed = 0f;

        starImage.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Scale animation with overshoot
            float scale = Mathf.Lerp(0f, 1f, EaseOutBack(t));
            starImage.transform.localScale = originalScale * scale;

            // Color animation
            starImage.color = Color.Lerp(Color.gray, Color.yellow, t);

            yield return null;
        }

        starImage.transform.localScale = originalScale;
        starImage.color = Color.yellow;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null) return;

        // Check if next level exists and is unlocked
        int nextLevelNumber = LevelLoader.CurrentLevelNumber + 1;
        string nextLevelId = $"level_{nextLevelNumber}";

        var levelManager = FindObjectOfType<LevelManager>();
        bool nextLevelAvailable = false;

        if (levelManager != null)
        {
            // The next level should be unlocked by completing current level
            var nextLevelDef = levelManager.levelDefinitions.Find(x => x.id == nextLevelId);
            nextLevelAvailable = nextLevelDef != null;
        }

        nextLevelButton.interactable = nextLevelAvailable;

        // Update button text
        var buttonText = nextLevelButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = nextLevelAvailable ? "NEXT LEVEL" : "COMPLETED";
        }
    }

    private void OnButtonClick(string buttonType)
    {
        PlaySound(buttonClickSound);

        var gameManager = EnhancedInGameManager.Instance;
        if (gameManager == null) return;

        switch (buttonType)
        {
            case "Restart":
                gameManager.RestartLevel();
                break;
            case "NextLevel":
                gameManager.NextLevel();
                break;
            case "MainMenu":
                gameManager.GoToMainMenu();
                break;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        var audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(clip);
    }
}