using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kontrol display bintang di level selection item.
/// Tempel script ini pada prefab LevelSelectionItem.
/// Assign 3 star GameObjects (Image/Sprite) di inspector.
/// </summary>
public class LevelStarDisplay : MonoBehaviour
{
    [Header("Star Objects (assign 3 stars dari hierarchy)")]
    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

    [Header("Optional: Star Images untuk ganti sprite")]
    public Image star1Image;
    public Image star2Image;
    public Image star3Image;

    [Header("Optional: Sprites untuk filled/empty stars")]
    public Sprite starFilled;
    public Sprite starEmpty;

    private string levelId;
    private int levelNumber;

    /// <summary>
    /// Setup stars untuk level ini.
    /// Dipanggil dari LevelSelectionItem.Start()
    /// </summary>
    public void Initialize(string id, int num)
    {
        levelId = id;
        levelNumber = num;
        RefreshStars();
    }

    /// <summary>
    /// Refresh display bintang berdasarkan saved progress
    /// </summary>
    public void RefreshStars()
    {
        // Default: hide all stars
        if (star1 != null) star1.SetActive(false);
        if (star2 != null) star2.SetActive(false);
        if (star3 != null) star3.SetActive(false);

        // Get saved stars untuk level ini
        int earnedStars = 0;
        if (!string.IsNullOrEmpty(levelId) && LevelProgressManager.Instance != null)
        {
            earnedStars = LevelProgressManager.Instance.GetBestStars(levelId);
        }

        // Show stars sesuai earned amount
        if (earnedStars >= 1 && star1 != null)
        {
            star1.SetActive(true);
            if (star1Image != null && starFilled != null)
                star1Image.sprite = starFilled;
        }
        
        if (earnedStars >= 2 && star2 != null)
        {
            star2.SetActive(true);
            if (star2Image != null && starFilled != null)
                star2Image.sprite = starFilled;
        }
        
        if (earnedStars >= 3 && star3 != null)
        {
            star3.SetActive(true);
            if (star3Image != null && starFilled != null)
                star3Image.sprite = starFilled;
        }

        Debug.Log($"[LevelStarDisplay] Level {levelId}: {earnedStars} stars displayed");
    }

    /// <summary>
    /// Public method untuk manual refresh (bisa dipanggil dari luar)
    /// </summary>
    public void ForceRefresh()
    {
        RefreshStars();
    }
}